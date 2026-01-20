
import React, { useEffect, useRef, useState, useCallback } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import Loading from "../../components/Loading/Loading";
import ReadOnlyField from "../../components/ReadOnlyField/ReadOnlyField";
import EditPaymentModal from "../../components/Modals/EditPaymentModal";
import CreatePaymentModal from "../../components/Modals/CreatePaymentModal";
import {
  freqLabel,
  formatCurrencyEUR,
  formatDate,
  dateInputToIsoMidnight,
  normalizeApiError,
} from "../../utils/Utils";
import {
  patchPayHistory,
  deletePayHistory,
  createPayHistory,
} from "../../Service/pagamentosService";
import { getEmployeesPaged } from "../../Service/employeeService"; // 👈 usa o service correto
import { getAllEmployees } from "../../Service/employeeService";
import { addNotificationForUser } from "../../utils/notificationBus";

export default function Pagamentos() {
  // ---- Estado base ----
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState(null);

  // Pesquisa & paginação
  const [searchTerm, setSearchTerm] = useState("");
  const [debouncedTerm, setDebouncedTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [serverTotalPages, setServerTotalPages] = useState(1);

  // Dados
  const [employees, setEmployees] = useState([]);      // employees para eventuais UIs auxiliares
  const [employeesDrop, setEmployeesDrop] = useState([]); // dropdown
  const [pagamentos, setPagamentos] = useState([]);    // lista achatada de PayHistories
  const itemsPerPage = 5;

  // ---- Concorrência ----
  const abortRef = useRef(null);
  const reqIdRef = useRef(0);
  const lastTermRef = useRef("");

  // Debounce: 300ms
  useEffect(() => {
    const t = setTimeout(() => setDebouncedTerm(searchTerm.trim()), 300);
    return () => clearTimeout(t);
  }, [searchTerm]);

  // Loader principal (página + termo), com AbortController e clamp de página
  const load = useCallback(
    async (page, term) => {
      setFetchError(null);

      // cancela o pedido anterior
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      const myReq = ++reqIdRef.current;

      setLoading(true);
      try {
        // 1) Chamar o endpoint paginado de employees
        const { items, totalPages } = await getEmployeesPaged({
          pageNumber: page,
          pageSize: itemsPerPage,
          search: term || "",
          signal: controller.signal,
        });

        if (myReq !== reqIdRef.current) return; // ignora respostas antigas

        // 2) Guardar employees úteis (ex.: para outras UIs)
        const myId = Number(localStorage.getItem("businessEntityId"));
        const employeesExceptActual = (items ?? [])
          .filter((e) => e.businessEntityID !== myId)
          .sort((a, b) =>
            (a.person?.firstName || "").localeCompare(b.person?.firstName || "")
          );
        setEmployees(employeesExceptActual);

        // 3) Achatar payHistories anexando o employee
        const pays = (items ?? []).flatMap((emp) =>
          (emp.payHistories ?? []).map((ph) => ({ ...ph, employee: emp }))
        );

        // 4) Ordenar por data (mais recentes primeiro)
        pays.sort(
          (a, b) => new Date(b.rateChangeDate) - new Date(a.rateChangeDate)
        );

        setPagamentos(pays);

        // 5) Atualizar totalPages e clamp de página
        const newTotalPages = Math.max(1, Number(totalPages || 1));
        if (page > newTotalPages) {
          setServerTotalPages(newTotalPages);
          setCurrentPage(1);
          return;
        }
        setServerTotalPages(newTotalPages);

        // 6) Dropdown (best‑effort, não bloqueante)
        try {
          const token = localStorage.getItem("authToken");
          const employeedrop = await getAllEmployees(token);
          if (myReq === reqIdRef.current) setEmployeesDrop(employeedrop);
        } catch {
          // ignora erros do dropdown
        }
      } catch (err) {
        if (err?.name === "AbortError") return;
        if (myReq !== reqIdRef.current) return;

        console.error(err);
        setFetchError(err?.message || "Erro a carregar dados.");
        setEmployees([]);
        setPagamentos([]);
        setServerTotalPages(1);
      } finally {
        if (myReq === reqIdRef.current) setLoading(false);
      }
    },
    [itemsPerPage]
  );

  // Efeito principal: reage a termo (debounced) e página
  useEffect(() => {
    const termChanged = debouncedTerm !== lastTermRef.current;

    if (termChanged) {
      lastTermRef.current = debouncedTerm;
      if (currentPage !== 1) {
        setCurrentPage(1);
        return; // evita chamada com página antiga; chamará quando page=1
      }
    }

    load(currentPage, debouncedTerm);
  }, [currentPage, debouncedTerm, load]);

  useEffect(() => {
    return () => abortRef.current?.abort();
  }, []);

  // ---- Estados e ações de edição ----
  const [editOpen, setEditOpen] = useState(false);
  const [editLoading, setEditLoading] = useState(false);
  const [editError, setEditError] = useState(null);
  const [editKeys, setEditKeys] = useState({
    businessEntityID: "",
    rateChangeDate: "",
  });
  const [editForm, setEditForm] = useState({ rate: "", payFrequency: "1" });

  const openEdit = (p) => {
    setEditKeys({
      businessEntityID: String(
        p.businessEntityID ?? p.employee?.businessEntityID ?? ""
      ),
      rateChangeDate: p.rateChangeDate ?? "",
    });
    setEditForm({
      rate: p.rate ?? "",
      payFrequency: String(p.payFrequency ?? "1"),
    });
    setEditError(null);
    setEditOpen(true);
  };

  const submitEdit = async () => {
    try {
      setEditLoading(true);
      setEditError(null);
      if (!editForm.rate || !editForm.payFrequency)
        throw new Error("Preenche todos os campos.");

      await patchPayHistory(editKeys.businessEntityID, editKeys.rateChangeDate, {
        rate: Number(editForm.rate),
        payFrequency: Number(editForm.payFrequency),
      });

      await load(currentPage, debouncedTerm);
      addNotificationForUser(
        "O seu registo de Pagamento foi atualizado.",
        editKeys.businessEntityID,
        { type: "PAYMENT" }
      );
      setEditOpen(false);
    } catch (e) {
      setEditError(normalizeApiError(e));
    } finally {
      setEditLoading(false);
    }
  };

  // ---- Eliminar registo ----
  const [deleteLoadingId, setDeleteLoadingId] = useState(null);
  const handleDelete = async (p) => {
    const businessEntityID =
      p.businessEntityID ?? p.employee?.businessEntityID ?? "";
    const rateChangeDate = p.rateChangeDate ?? "";
    if (!businessEntityID || !rateChangeDate)
      return alert("Chaves do registo em falta.");

    if (
      !window.confirm(
        `Eliminar registo do colaborador ${p.employee?.person?.firstName} ${p.employee?.person?.lastName}?`
      )
    )
      return;

    try {
      const key = `${businessEntityID}|${rateChangeDate}`;
      setDeleteLoadingId(key);
      await deletePayHistory(businessEntityID, rateChangeDate);

      await load(currentPage, debouncedTerm);
      addNotificationForUser(
        "O seu registo de Pagamento foi eliminado.",
        businessEntityID,
        { type: "PAYMENT" }
      );
    } catch (e) {
      alert(e.message || "Erro ao eliminar registo.");
    } finally {
      setDeleteLoadingId(null);
    }
  };

  // ---- Criar registo ----
  const [createOpen, setCreateOpen] = useState(false);
  const [createLoading, setCreateLoading] = useState(false);
  const [createError, setCreateError] = useState(null);
  const [createForm, setCreateForm] = useState({
    businessEntityID: "",
    rateChangeDate: "",
    rate: "",
    payFrequency: "1",
  });

  const submitCreate = async () => {
    try {
      setCreateLoading(true);
      setCreateError(null);
      if (
        !createForm.businessEntityID ||
        !createForm.rateChangeDate ||
        !createForm.rate
      )
        throw new Error("Preenche todos os campos.");

      const body = {
        businessEntityID: Number(createForm.businessEntityID),
        rateChangeDate: dateInputToIsoMidnight(createForm.rateChangeDate),
        rate: Number(createForm.rate),
        payFrequency: Number(createForm.payFrequency),
      };

      await createPayHistory(body);

      await load(currentPage, debouncedTerm);
      addNotificationForUser("Foi criado um novo registo de Pagamento.", body.businessEntityID, {
        type: "PAYMENT",
      });

      setCreateOpen(false);
      setCreateForm({
        businessEntityID: "",
        rateChangeDate: "",
        rate: "",
        payFrequency: "1",
      });
    } catch (err) {
      setCreateError(normalizeApiError(err));
    } finally {
      setCreateLoading(false);
    }
  };

  return (
    <div className="container mt-4">
      <BackButton />
      <div className="mb-4 d-flex justify-content-between align-items-center">
        <h1 className="h3 mb-1">Gestão de Pagamentos</h1>
        {!loading && (
          <button className="btn btn-sm btn-primary" onClick={() => setCreateOpen(true)}>
            Criar registo
          </button>
        )}
      </div>

      {/* Search (sempre visível) */}
      <div className="card mb-3 border-0 shadow-sm">
        <div className="card-body position-relative">
          <input
            type="text"
            className="form-control"
            placeholder="Procurar por ID, colaborador, frequência, valor ou data..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
          {loading && (
            <div
              className="position-absolute top-50 end-0 translate-middle-y me-3 text-muted small"
              aria-hidden="true"
            >
              <span className="spinner-border spinner-border-sm" /> A carregar...
            </div>
          )}
        </div>
      </div>

      {/* Content */}
      <div className="card shadow-sm">
        <div className="card-body p-0">
          {loading && pagamentos.length === 0 ? (
            <Loading text="Carregando lista..." />
          ) : (
            <>
              {/* Desktop Table */}
              <div className="table-responsive d-none d-md-block">
                <table className="table table-hover mb-0">
                  <thead className="table-light">
                    <tr>
                      <th>Colaborador</th>
                      <th>Valor</th>
                      <th>Data</th>
                      <th className="text-center">Frequência</th>
                      <th className="text-end">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pagamentos.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="text-center text-muted">
                          Sem registos
                        </td>
                      </tr>
                    ) : (
                      pagamentos.map((p) => {
                        const key = `${p.businessEntityID ?? p.employee?.businessEntityID}|${p.rateChangeDate}`;
                        const deleting = deleteLoadingId === key;
                        return (
                          <tr key={p.payHistoryId ?? key}>
                            <td>
                              {p.employee?.person?.firstName} {p.employee?.person?.lastName}
                              <div className="small text-muted">
                                ID: {p.employee?.businessEntityID ?? "—"}
                              </div>
                            </td>
                            <td>{formatCurrencyEUR(p.rate)}</td>
                            <td>{formatDate(p.rateChangeDate)}</td>
                            <td className="text-center">{freqLabel(p.payFrequency)}</td>
                            <td className="text-end">
                              <button
                                className="btn btn-outline-primary btn-sm me-2"
                                onClick={() => openEdit(p)}
                              >
                                Editar
                              </button>
                              <button
                                className="btn btn-outline-danger btn-sm"
                                disabled={deleting}
                                onClick={() => handleDelete(p)}
                              >
                                {deleting ? "A eliminar..." : "Eliminar"}
                              </button>
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
                {pagamentos.length === 0 ? (
                  <div className="text-center p-3 text-muted">Sem registos</div>
                ) : (
                  pagamentos.map((p) => {
                    const key = `${p.businessEntityID ?? p.employee?.businessEntityID}|${p.rateChangeDate}`;
                    const deleting = deleteLoadingId === key;
                    return (
                      <div key={p.payHistoryId ?? key} className="border-bottom p-3">
                        <h6>
                          <strong>
                            {p.employee?.person?.firstName} {p.employee?.person?.lastName}
                          </strong>
                        </h6>
                        <ReadOnlyField label="ID" value={p.employee?.businessEntityID ?? "—"} />
                        <ReadOnlyField label="Valor" value={formatCurrencyEUR(p.rate)} />
                        <ReadOnlyField label="Data" value={formatDate(p.rateChangeDate)} />
                        <ReadOnlyField label="Frequência" value={freqLabel(p.payFrequency)} />
                        <div className="d-flex gap-2 mt-2">
                          <button className="btn btn-sm btn-outline-primary" onClick={() => openEdit(p)}>
                            Editar
                          </button>
                          <button
                            className="btn btn-sm btn-outline-danger"
                            disabled={deleting}
                            onClick={() => handleDelete(p)}
                          >
                            {deleting ? "A eliminar..." : "Eliminar"}
                          </button>
                        </div>
                      </div>
                    );
                  })
                )}
              </div>

              {/* Pagination (usa meta do servidor) */}
              <Pagination
                currentPage={currentPage}
                totalPages={serverTotalPages}
                setPage={(p) => {
                  if (!loading && p >= 1 && p <= serverTotalPages && p !== currentPage) {
                    setCurrentPage(p);
                  }
                }}
              />
            </>
          )}
        </div>
      </div>

      {fetchError && <div className="alert alert-danger mt-3">{fetchError}</div>}

      <EditPaymentModal
        open={editOpen}
        onClose={() => setEditOpen(false)}
        onSubmit={submitEdit}
        loading={editLoading}
        error={editError}
    keys={editKeys}
        form={editForm}
        setForm={setEditForm}
      />

      <CreatePaymentModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onSubmit={submitCreate}
        loading={createLoading}
        error={createError}
        form={createForm}
        setForm={setCreateForm}
        employees={employeesDrop}
      />
    </div>
  );
}
