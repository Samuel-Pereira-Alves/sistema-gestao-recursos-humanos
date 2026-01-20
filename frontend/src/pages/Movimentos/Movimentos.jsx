
import React, { useEffect, useState, useCallback, useRef } from "react";
import { addNotificationForUser } from "../../utils/notificationBus";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import {
  formatDate,
  dateInputToIsoMidnight,
  getBusinessEntityID,
  getDepartmentID,
  getShiftID,
  getStartDate,
  getEndDate,
  getDepartmentName,
  getGroupName,
  formatDateForRoute,
} from "../../utils/Utils";
import { getAllEmployees } from "../../Service/employeeService";
import AssignmentModal from "../../components/AssignmentForm/AssignmentModal";
import {
  createDepartmentHistory,
  deleteDepHistory,
  getAllDepartments,
  getAllDepartmentsFromEmployees,
  patchDepartmentHistory,
} from "../../Service/departmentHistoryService";

const SHIFT_LABELS = { 1: "Manhã", 2: "Tarde", 3: "Noite" };
const resolveShiftLabel = (id) => SHIFT_LABELS[Number(id)] ?? "—";

export default function Movimentos() {
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState(null);

  // Itens (já flattened vindos do backend)
  const [items, setItems] = useState([]);

  // Colaboradores (para modal)
  const [employees, setEmployees] = useState([]);

  // Departamentos (para resolvers de nome, etc.)
  const [departments, setDepartments] = useState([]);

  // Pesquisa + debounce
  const [searchTerm, setSearchTerm] = useState("");
  const [debouncedTerm, setDebouncedTerm] = useState("");

  // Paginação SERVER-SIDE
  const [serverPage, setServerPage] = useState(1);
  const [serverTotalPages, setServerTotalPages] = useState(1);
  const itemsPerPage = 5;

  // Concorrência / cancelamento
  const abortRef = useRef(null);
  const reqIdRef = useRef(0);

  // Debounce 300ms
  useEffect(() => {
    const t = setTimeout(() => setDebouncedTerm(searchTerm.trim()), 300);
    return () => clearTimeout(t);
  }, [searchTerm]);

  // Loader principal (página + termo), com AbortController e clamp
  const load = useCallback(
    async (page, term) => {
      setFetchError(null);

      const safePage = Math.max(1, Number(page) || 1);
      const safeTerm = (term ?? "").toString();

      // cancela o pedido anterior
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      const myReq = ++reqIdRef.current;
      setLoading(true);

      try {
        const data = await getAllDepartments({
          pageNumber: safePage,
          pageSize: itemsPerPage,
          query: safeTerm,          // pesquisa server-side
          signal: controller.signal,
        });

        if (myReq !== reqIdRef.current) return; // ignora respostas fora de ordem

        const newTotalPages = Math.max(1, Number(data?.totalPages || 1));

        // clamp para última página válida
        if (safePage > newTotalPages) {
          setServerTotalPages(newTotalPages);
          setServerPage(newTotalPages);
          return; // o useEffect volta a invocar load com a nova página
        }

        // Normalização: itens devem vir em data.items (array)
        const arr = Array.isArray(data?.items) ? data.items : [];
        setItems(arr);
        setServerTotalPages(newTotalPages);

      } catch (err) {
        if (err?.name === "AbortError") return;
        if (myReq !== reqIdRef.current) return;

        console.error("[load] erro:", err);
        setFetchError(err?.message || "Erro a carregar dados.");
        setItems([]);
        setServerTotalPages(1);
      } finally {
        if (myReq === reqIdRef.current) setLoading(false);
      }
    },
    [itemsPerPage]
  );

  // Reage a (página, termo debounced)
  useEffect(() => {
    load(serverPage, debouncedTerm);
  }, [serverPage, debouncedTerm, load]);

  // Cleanup abort ao desmontar
  useEffect(() => {
    return () => abortRef.current?.abort();
  }, []);

  // Carregar lista de departamentos uma vez (best-effort)
  useEffect(() => {
    (async () => {
      try {
        const token = localStorage.getItem("authToken");
        const deps = await getAllDepartmentsFromEmployees(token);
        setDepartments(Array.isArray(deps) ? deps : []);
      } catch {
        // ignora erros do dropdown
      }
    })();
  }, []);

  // === Modal state ===
  const [action, setAction] = useState({
    open: false,
    mode: "create",
    loading: false,
    error: null,
    keys: {
      businessEntityID: "",
      departmentID: "",
      shiftID: "",
      startDate: "",
    },
    form: {
      businessEntityID: "",
      departmentID: "",
      shiftID: "",
      startDate: "",
      endDate: "",
    },
  });

  const fetchEmployeesForModal = useCallback(async () => {
    try {
      const token = localStorage.getItem("authToken");
      const data = await getAllEmployees(token);
      const list = Array.isArray(data) ? data : [];

      const idStr = localStorage.getItem("businessEntityId");
      const myId = Number(idStr || "0");

      const employeesExceptActual = list
        .filter((e) => Number(e?.businessEntityID) !== myId)
        .sort((a, b) => {
          const nameA = (a?.person?.firstName || "").toLowerCase();
          const nameB = (b?.person?.firstName || "").toLowerCase();
          return nameA.localeCompare(nameB);
        });

      setEmployees(employeesExceptActual);
    } catch (error) {
      console.error("[fetchEmployeesForModal] erro:", error);
    }
  }, []);

  const openCreate = () => {
    setAction({
      open: true,
      mode: "create",
      loading: false,
      error: null,
      keys: { businessEntityID: "", departmentID: "", shiftID: "", startDate: "" },
      form: { businessEntityID: "", departmentID: "", shiftID: "", startDate: "", endDate: "" },
    });
    fetchEmployeesForModal();
  };

  const openDelete = async (h) => {
    try {
      const ok = window.confirm("Tem a certeza que deseja apagar este registo?");
      if (!ok) return;

      const beid = getBusinessEntityID(h);
      const depId = getDepartmentID(h);
      const shId = getShiftID(h);
      const start = getStartDate(h);
      const formattedDate = formatDateForRoute(start);

      const token = localStorage.getItem("authToken");
      await deleteDepHistory(token, beid, depId, shId, formattedDate);

      await load(serverPage, debouncedTerm);

      addNotificationForUser(
        "Foi eliminado um movimento de departamentos no seu perfil.",
        beid,
        { type: "DEPARTMENT" }
      );
    } catch (e) {
      console.error("[openDelete] error:", e);
      alert(e.message || "Erro ao apagar registo.");
    }
  };

  const openEdit = (h) => {
    const beid = getBusinessEntityID(h);
    const depId = getDepartmentID(h);
    const shId = getShiftID(h);
    const start = getStartDate(h);
    const end = getEndDate(h);
    const departmentName = h.dep.name;

    setAction({
      open: true,
      mode: "edit",
      loading: false,
      error: null,
      keys: {
        businessEntityID: String(beid),
        departmentID: String(depId),
        shiftID: String(shId),
        startDate: start,
        departmentName
      },
      form: {
        businessEntityID: String(beid),
        departmentID: String(depId),
        shiftID: String(shId),
        startDate: start,
        endDate: end ?? "",
        departmentName
      },
    });
  };

  const closeAction = () => setAction((s) => ({ ...s, open: false, error: null }));

  const submitAction = async () => {
    try {
      setAction((s) => ({ ...s, loading: true, error: null }));

      const { mode, form, keys } = action;

      if (mode === "create") {
        const businessEntityID = Number(form.businessEntityID);
        const departmentID = Number(form.departmentID);
        const shiftID = Number(form.shiftID);
        if (!businessEntityID) throw new Error("BusinessEntityID em falta.");
        if (!departmentID) throw new Error("DepartmentID em falta.");
        if (!shiftID) throw new Error("ShiftID em falta.");
        if (!form.startDate) throw new Error("Data Início em falta.");

        const body = {
          businessEntityID,
          departmentID,
          shiftID,
          startDate: dateInputToIsoMidnight(form.startDate),
          endDate: form.endDate ? dateInputToIsoMidnight(form.endDate) : null,
        };

        await createDepartmentHistory(body);
        addNotificationForUser(
          "Foi criado um registo de movimentos para o seu perfil.",
          businessEntityID,
          { type: "DEPARTMENT" }
        );
      } else {
        const { businessEntityID, departmentID, shiftID, startDate, departmentName } = keys;
        if (!businessEntityID || !departmentID || !shiftID || !startDate)
          throw new Error("Chaves do registo em falta.");

        const patchBody = { endDate: form.endDate ? form.endDate : null };

        await patchDepartmentHistory(
          businessEntityID,
          departmentID,
          shiftID,
          startDate,
          patchBody,
          departmentName
        );
        addNotificationForUser(
          "O seu registo de Movimentos de Departamentos foi atualizado pelo RH.",
          keys.businessEntityID,
          { type: "DEPARTMENT" }
        );
      }

      await load(serverPage, debouncedTerm);
      closeAction();
    } catch (e) {
      setAction((s) => ({ ...s, error: e.message || "Erro na operação." }));
    } finally {
      setAction((s) => ({ ...s, loading: false }));
    }
  };

  
// Usa o nome que vem no DTO (h.dep.name) e faz fallback por ID se necessário.
const resolveDepartmentName = (id) => {
  const depa = departments.find(d => d.departmentID == id)
  
  return depa?.name ?? "ja";
};



  return (
    <div className="container mt-4">
      <BackButton />
      <div className="mb-4 d-flex align-items-center justify-content-between">
        <h1 className="h3 mb-1">Histórico de Departamentos</h1>
        {!loading && (
          <button className="btn btn-sm btn-primary" onClick={openCreate}>
            Criar registo
          </button>
        )}
      </div>

      {/* Search */}
      <div className="card mb-3 border-0 shadow-sm">
        <div className="card-body position-relative">
          <input
            type="text"
            className="form-control pe-5"
            placeholder="Procurar por ID, colaborador, departamento, grupo ou data..."
            value={searchTerm}
            onChange={(e) => {
              const v = e.target.value;
              setSearchTerm(v);
              // reset da página só quando necessário
              if (serverPage !== 1) setServerPage(1);
            }}
            aria-label="Pesquisar histórico de departamentos"
          />
          {/* Spinner subtil dentro da searchbar enquanto pesquisa */}
          {loading && (
            <div
              className="position-absolute top-50 end-0 translate-middle-y me-3 text-muted small"
              aria-hidden="true"
            >
              <span className="spinner-border spinner-border-sm" />
            </div>
          )}
        </div>
      </div>

      <div className="card shadow-sm">
        <div className="card-body p-0">
          {loading ? (
            <div className="text-center py-5">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Carregando...</span>
              </div>
            </div>
          ) : (
            <>
              {/* Desktop Table */}
              <div className="table-responsive d-none d-md-block">
                <table className="table table-hover mb-0">
                  <thead className="table-light">
                    <tr>
                      <th className="px-4 py-3">Colaborador</th>
                      <th className="px-4 py-3">Departamento</th>
                      <th className="px-4 py-3">Grupo</th>
                      <th className="px-4 py-3">Data Início</th>
                      <th className="px-4 py-3">Data Fim</th>
                      <th className="px-4 py-3 text-end">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.length === 0 ? (
                      <tr>
                        <td colSpan={6} className="text-center text-muted py-4">
                          Sem registos
                        </td>
                      </tr>
                    ) : (
                      items.map((h) => {
                        // Assumimos que o backend envia um DTO flattened:
                        // {
                        //   businessEntityID, departmentID, shiftID, startDate, endDate,
                        //   person: { firstName, lastName, businessEntityID? },
                        //   dep: { name, groupName }
                        // }
                        const person = h?.person ?? {};
                        const deptName = h?.dep?.name ?? getDepartmentName(h);
                        const groupName = h?.dep?.groupName ?? getGroupName(h);
                        const start = getStartDate(h);
                        const end = getEndDate(h);

                        const key = `${getBusinessEntityID(h)}|${getDepartmentID(h)}|${getShiftID(h)}|${start}`;

                        return (
                          <tr key={key}>
                            <td className="px-4 py-3">
                              {person.firstName} {person.lastName}
                              <div className="small text-muted">
                                ID: {h?.businessEntityID ?? person?.businessEntityID ?? "—"}
                              </div>
                            </td>
                            <td className="px-4 py-3">{deptName}</td>
                            <td className="px-4 py-3">{groupName}</td>
                            <td className="px-4 py-3 text-muted">{formatDate(start)}</td>
                            <td className="px-4 py-3 text-muted">{formatDate(end)}</td>
                            <td className="px-4 py-3 text-end">
                              <div className="d-flex justify-content-end gap-2">
                                <button
                                  className="btn btn-outline-primary"
                                  onClick={() => openEdit(h)}
                                  disabled={
                                    String(localStorage.getItem("businessEntityId")) ===
                                    String(h?.businessEntityID ?? person?.businessEntityID)
                                  }
                                >
                                  Editar
                                </button>
                                <button
                                  className="btn btn-outline-danger"
                                  onClick={() => openDelete(h)}
                                  disabled={
                                    String(localStorage.getItem("businessEntityId")) ===
                                    String(h?.businessEntityID ?? person?.businessEntityID)
                                  }
                                >
                                  Eliminar
                                </button>
                              </div>
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>

              {/* Mobile Cards */}
              <div className="d-md-none">
                {items.length === 0 ? (
                  <div className="text-center p-3 text-muted">Sem registos</div>
                ) : (
                  items.map((h) => {
                    const person = h?.person ?? {};
                    const start = getStartDate(h);
                    const end = getEndDate(h);
                    const key = `${getBusinessEntityID(h)}|${getDepartmentID(h)}|${getShiftID(h)}|${start}`;

                    return (
                      <div key={key} className="border-bottom p-3">
                        <h6 className="mb-1">
                          <strong>
                            {person.firstName} {person.lastName}
                          </strong>
                        </h6>
                        <p className="text-muted small mb-1">
                          <strong>ID:</strong> {h?.businessEntityID ?? person?.businessEntityID ?? "—"}
                        </p>
                        <p className="text-muted small mb-1">
                          <strong>Departamento:</strong> {h?.dep?.name}
                        </p>
                        <p className="text-muted small mb-1">
                          <strong>Grupo:</strong> {h?.dep?.groupName ?? getGroupName(h)}
                        </p>
                        <p className="text-muted small mb-1">
                          <strong>Início:</strong> {formatDate(start)}
                        </p>
                        <p className="text-muted small mb-2">
                          <strong>Fim:</strong> {formatDate(end)}
                        </p>
                        <div className="d-flex gap-2">
                          <button className="btn btn-sm btn-outline-primary" onClick={() => openEdit(h)}>
                            Editar
                          </button>
                          <button className="btn btn-sm btn-outline-danger ms-2" onClick={() => openDelete(h)}>
                            Apagar
                          </button>
                        </div>
                      </div>
                    );
                  })
                )}
              </div>

              {/* Empty state (quando sem items) */}
              {!items.length && (
                <div className="p-4">
                  <p className="text-muted mb-0">
                    Sem resultados para <strong>{debouncedTerm || "…"}</strong>. Tenta outro termo.
                  </p>
                </div>
              )}

              {/* Pagination */}
              <Pagination
                currentPage={serverPage}
                totalPages={serverTotalPages}
                setPage={setServerPage} // garantir número
              />
            </>
          )}
        </div>
      </div>

      {fetchError && <div className="alert alert-danger mt-3">{fetchError}</div>}

      <AssignmentModal
        action={action}
        setAction={setAction}
        closeAction={closeAction}
        submitAction={submitAction}
        employees={employees}
        departments={departments}
        resolveDepartmentName={resolveDepartmentName}
        resolveShiftLabel={resolveShiftLabel}
        formatDate={formatDate}
        dateInputToIsoMidnight={dateInputToIsoMidnight}
      />
    </div>
  );
}
