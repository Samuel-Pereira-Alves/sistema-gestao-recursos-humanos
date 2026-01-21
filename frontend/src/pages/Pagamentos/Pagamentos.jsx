
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
  getAllPayments,
} from "../../Service/pagamentosService";
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
  const [totalPages, setTotalPages] = useState(1);

  // Dados
  const [pagamentos, setPagamentos] = useState([]);
  const [employeesDrop, setEmployeesDrop] = useState([]);
  const itemsPerPage = 5;

  const lastTermRef = useRef("");

  // Debounce: 300ms
  useEffect(() => {
    const t = setTimeout(() => setDebouncedTerm(searchTerm.trim()), 300);
    return () => clearTimeout(t);
  }, [searchTerm]);

  const load = useCallback(
    async (page, term) => {
      setFetchError(null);

      setLoading(true);

      try {
        const data = await getAllPayments({
          pageNumber: page,
          pageSize: itemsPerPage,
          search: term || "",
        });

        const newTotalPages = Math.max(1, Number(data?.totalPages || 1));

        if (page > newTotalPages) {
          setTotalPages(newTotalPages);
          setCurrentPage(newTotalPages);
          return;
        }

        setPagamentos(Array.isArray(data?.items) ? data.items : []);
        setTotalPages(newTotalPages);

        try {
          const token = localStorage.getItem("authToken");
          const employeedrop = await getAllEmployees(token);
          setEmployeesDrop(employeedrop);
        } catch {
        }
      } catch (err) {
        if (err?.name === "AbortError") return;

        console.error(err);
        //setFetchError(err?.message || "Erro a carregar dados.");
        setPagamentos([]);
        setTotalPages(1);
      } finally {
        setLoading(false);
      }
    },
    [itemsPerPage]
  );

  useEffect(() => {
    const termChanged = debouncedTerm !== lastTermRef.current;

    if (termChanged) {
      lastTermRef.current = debouncedTerm;
      if (currentPage !== 1) {
        setCurrentPage(1);
        return;
      }
    }

    load(currentPage, debouncedTerm);
  }, [currentPage, debouncedTerm, load]);

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
      businessEntityID: String(p.businessEntityID ?? ""),
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
    const businessEntityID = p.businessEntityID;
    const rateChangeDate = p.rateChangeDate ?? "";
    if (!businessEntityID || !rateChangeDate)
      return alert("Chaves do registo em falta.");

    if (
      !window.confirm(
        `Eliminar registo do colaborador ${p.person?.firstName} ${p.person?.lastName}?`
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
      <div className="mb-4 d-flex justify-content-between align-items-center">
        <h1 className="h3 mb-1">Gestão de Pagamentos</h1>
        {!loading && (
          <button className="btn btn-sm btn-primary" onClick={() => setCreateOpen(true)}>
            Criar registo
          </button>
        )}
      </div>

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
                      <th className="text-center" style={{width: 160}}>Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pagamentos.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="text-center text-muted">
                          Registos não encontrados.
                        </td>
                      </tr>
                    ) : (
                      pagamentos.map((p) => {
                        const key = `${p.businessEntityID}|${p.rateChangeDate}`;
                        const deleting = deleteLoadingId === key;
                        return (
                          <tr key={p.payHistoryId ?? key}>
                            <td>
                              {p.person?.firstName} {p.person?.lastName}
                              <div className="small text-muted">
                                ID: {p.businessEntityID ?? "—"}
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
                  <div className="text-center p-3 text-muted">Registos não encontrados.</div>
                ) : (
                  pagamentos.map((p) => {
                    const key = `${p.businessEntityID}|${p.rateChangeDate}`;
                    const deleting = deleteLoadingId === key;
                    return (
                      <div key={p.payHistoryId ?? key} className="border-bottom p-3">
                        <h6>
                          <strong>
                            {p.person?.firstName} {p.person?.lastName}
                          </strong>
                        </h6>
                        <ReadOnlyField label="ID" value={p.businessEntityID ?? "—"} />
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

              {/* Pagination */}
              {pagamentos.length > 0 ? (
                <Pagination
                  currentPage={currentPage}
                  totalPages={totalPages}
                  setPage={setCurrentPage}
                />
              ) : (<> </>)
              }
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
