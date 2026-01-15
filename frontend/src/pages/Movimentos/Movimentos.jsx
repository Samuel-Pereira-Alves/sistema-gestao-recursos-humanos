import React, { useEffect, useMemo, useState, useCallback } from "react";
import { addNotificationForUser } from "../../utils/notificationBus";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import {buildDerivedDepartments, formatDate, normalize, idToString, dateInputToIsoMidnight, getBusinessEntityID, getDepartmentID, getShiftID, getStartDate, getEndDate, getDepartmentName, getGroupName, formatDateForRoute } from "../../utils/Utils";
import { getEmployees } from "../../Service/employeeService";
import AssignmentModal from "../../components/AssignmentForm/AssignmentModal";
import { createDepartmentHistory, deleteDepHistory, patchDepartmentHistory } from "../../Service/departmentHistoryService";

const SHIFT_LABELS = { 1: "Manhã", 2: "Tarde", 3: "Noite" };
const resolveShiftLabel = (id) => SHIFT_LABELS[Number(id)] ?? "—";

export default function Movimentos() {
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState(null);
  const [items, setItems] = useState([]);
  const [employees, setEmployees] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);

  const itemsPerPage = 5;

  const fetchEmployees = async () => {
    try {
      const token = localStorage.getItem("authToken")
      const data = await getEmployees(token);
      const idStr = localStorage.getItem("businessEntityId");
      const id = Number(idStr)

      const employeesExceptActual = data
        .filter(e => e.businessEntityID !== id)
        .sort((a, b) => {
          const nameA = (a.person?.firstName || "").toLowerCase();
          const nameB = (b.person?.firstName || "").toLowerCase();
          return nameA.localeCompare(nameB);
        });

      setEmployees(employeesExceptActual)
    } catch (error) {

    }
  }

  const fetchData = useCallback(async () => {
    try {
      const token = localStorage.getItem("authToken");
      setLoading(true);
      setFetchError(null);

      const data = await getEmployees(token)

      const employees = Array.isArray(data) ? data : data?.items ?? [];

      const flattened = employees.flatMap((emp) =>
        (emp.departmentHistories ?? emp.DepartmentHistories ?? []).map(
          (dh) => ({
            ...dh,
            employee: emp,
          })
        )
      );

      flattened.sort(
        (a, b) => new Date(getStartDate(b)) - new Date(getStartDate(a))
      );
      setItems(flattened);

      const derivedDeps = buildDerivedDepartments(flattened);
      setDepartments(derivedDeps);
    } catch (err) {
      setFetchError(err.message || "Erro desconhecido ao obter dados.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
    fetchEmployees();
  }, [fetchData]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm]);

  const rawSearch = searchTerm.trim();
  const isNumericSearch = /^\d+$/.test(rawSearch);
  const termo = normalize(searchTerm);

  const filtered = useMemo(() => {
    if (!rawSearch) return items;

    return items.filter((h) => {
      const employee = h.employee ?? {};
      const person = employee.person ?? {};

      if (isNumericSearch) {
        const beid = idToString(employee.businessEntityID);
        return beid === rawSearch;
      }

      const fullName = `${normalize(person.firstName)} ${normalize(
        person.lastName
      )}`.trim();
      const deptName = normalize(getDepartmentName(h));
      const groupName = normalize(getGroupName(h));
      const start = normalize(formatDate(getStartDate(h)));
      const end = normalize(formatDate(getEndDate(h)));

      return (
        (fullName && fullName.includes(termo)) ||
        (deptName && deptName.includes(termo)) ||
        (groupName && groupName.includes(termo)) ||
        (start && start.includes(termo)) ||
        (end && end.includes(termo))
      );
    });
  }, [items, rawSearch, termo, isNumericSearch]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / itemsPerPage));
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const pageItems = filtered.slice(indexOfFirst, indexOfLast);

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

  const openCreate = () => {
    setAction({
      open: true,
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
  };

const openDelete = async (h) => {
  try {
    const ok = window.confirm("Tem a certeza que deseja apagar este registo?");
    if (!ok) return;

    const beid = getBusinessEntityID(h);
    const depId = getDepartmentID(h);
    const shId  = getShiftID(h);
    const start = getStartDate(h);              
    const formattedDate = formatDateForRoute(start);

    const token = localStorage.getItem("authToken");
    await deleteDepHistory(token, beid, depId, shId, formattedDate);

    await fetchData();

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
      },
      form: {
        businessEntityID: String(beid),
        departmentID: String(depId),
        shiftID: String(shId),
        startDate: start,
        endDate: end ?? "",
      },
    });
  };

  const closeAction = () =>
    setAction((s) => ({ ...s, open: false, error: null }));

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
        addNotificationForUser("Foi criado um registo de movimentos para o seu perfil.", businessEntityID, { type: "DEPARTMENT" });
      } else {
        const { businessEntityID, departmentID, shiftID, startDate } = keys;
        if (!businessEntityID || !departmentID || !shiftID || !startDate)
          throw new Error("Chaves do registo em falta.");

        const patchBody = {
          endDate: form.endDate ? form.endDate : null,
        };

        await patchDepartmentHistory(businessEntityID, departmentID, shiftID, startDate, patchBody)
        addNotificationForUser("O seu registo de Movimentos de Departamentos foi atualizado pelo RH.", keys.businessEntityID, { type: "DEPARTMENT" });
      }

      await fetchData();
      closeAction();
    } catch (e) {
      setAction((s) => ({ ...s, error: e.message || "Erro na operação." }));
    } finally {
      setAction((s) => ({ ...s, loading: false }));
    }
  };

  const resolveDepartmentName = (id) => {
    const dep = departments.find((d) => String(d.departmentID) === String(id));
    return dep?.name ?? "—";
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
        <div className="card-body">
          {loading ? (
            <div className="text-center py-3">
              <div className="spinner-border text-secondary" role="status">
                <span className="visually-hidden">Carregando...</span>
              </div>
            </div>
          ) : (
            <input
              type="text"
              className="form-control"
              placeholder="Procurar por ID, colaborador, departamento, grupo ou data..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              aria-label="Pesquisar histórico de departamentos"
            />
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
                    {pageItems.map((h) => {
                      const employee = h.employee ?? {};
                      const person = employee.person ?? {};
                      const deptName = getDepartmentName(h);
                      const groupName = getGroupName(h);
                      const start = getStartDate(h);
                      const end = getEndDate(h);

                      const key = `${getBusinessEntityID(h)}|${getDepartmentID(
                        h
                      )}|${getShiftID(h)}|${start}`;

                      return (
                        <tr key={key}>
                          <td className="px-4 py-3">
                            {person.firstName} {person.lastName}
                            <div className="small text-muted">
                              ID: {employee.businessEntityID ?? "—"}
                            </div>
                          </td>
                          <td className="px-4 py-3">{deptName}</td>
                          <td className="px-4 py-3">{groupName}</td>
                          <td className="px-4 py-3 text-muted">
                            {formatDate(start)}
                          </td>
                          <td className="px-4 py-3 text-muted">
                            {formatDate(end)}
                          </td>
                          <td className="px-4 py-3 text-end">
                            <div className="d-flex justify-content-end gap-2">
                              <button
                                className="btn btn-outline-primary"
                                onClick={() => openEdit(h)}
                                disabled={localStorage.getItem("businessEntityId") == employee.businessEntityID}
                              >
                                Editar
                              </button>
                              <button
                                disabled={localStorage.getItem("businessEntityId") == employee.businessEntityID}
                                className="btn btn-outline-danger"
                                onClick={() => openDelete(h)}
                              >
                                Eliminar
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              {/* Mobile Cards */}
              <div className="d-md-none">
                {pageItems.map((h) => {
                  const employee = h.employee ?? {};
                  const person = employee.person ?? {};
                  const start = getStartDate(h);
                  const end = getEndDate(h);
                  const key = `${getBusinessEntityID(h)}|${getDepartmentID(
                    h
                  )}|${getShiftID(h)}|${start}`;

                  return (
                    <div key={key} className="border-bottom p-3">
                      <h6 className="mb-1">
                        <strong>
                          {person.firstName} {person.lastName}
                        </strong>
                      </h6>
                      <p className="text-muted small mb-1">
                        <strong>ID:</strong> {employee.businessEntityID ?? "—"}
                      </p>
                      <p className="text-muted small mb-1">
                        <strong>Departamento:</strong> {getDepartmentName(h)}
                      </p>
                      <p className="text-muted small mb-1">
                        <strong>Grupo:</strong> {getGroupName(h)}
                      </p>
                      <p className="text-muted small mb-1">
                        <strong>Início:</strong> {formatDate(start)}
                      </p>
                      <p className="text-muted small mb-2">
                        <strong>Fim:</strong> {formatDate(end)}
                      </p>
                      <div className="d-flex gap-2">
                        <button
                          className="btn btn-sm btn-outline-primary"
                          onClick={() => openEdit(h)}
                        >
                          Editar
                        </button>
                        <button
                          className="btn btn-sm btn-outline-danger ms-2"
                          onClick={() => openDelete(h)}
                        >
                          Apagar
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Empty state */}
              {!pageItems.length && (
                <div className="p-4">
                  <p className="text-muted mb-0">
                    Sem resultados para <strong>{rawSearch || "…"}</strong>.
                    Tenta outro termo.
                  </p>
                </div>
              )}

              {/* Pagination */}
              <Pagination
                currentPage={currentPage}
                totalPages={totalPages}
                setPage={setCurrentPage}
              />

            </>
          )}
        </div>
      </div>

      {fetchError && (
        <div className="alert alert-danger mt-3">{fetchError}</div>
      )}

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
