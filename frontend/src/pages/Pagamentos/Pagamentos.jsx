import React, { useEffect, useMemo, useState, useCallback } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import { addNotificationForUser } from "../../utils/notificationBus";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import { freqLabel, formatCurrencyEUR, formatDate, normalizarTexto, toIdString, dateInputToIsoMidnight } from "../../utils/formatters";
import { listPagamentosFlattened, patchPayHistory, deletePayHistory, createPayHistory } from "../../Service/pagamentosService";
import { filterPagamentos, paginate } from "../../utils/Utils";

export default function Pagamentos() {
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [employees, setEmployees] = useState([]);
  const [pagamentos, setPagamentos] = useState([]);

  const itemsPerPage = 5;

  const reload = useCallback(async () => {
    setLoading(true);
    setFetchError(null);
    try {
      const { employees: emps, pagamentos: pays } = await listPagamentosFlattened();

      const idStr = localStorage.getItem("businessEntityId");
      const myId = Number(idStr);
      const employeesExceptActual = (emps ?? []).filter((e) => e.businessEntityID !== myId)
        .sort((a, b) => (a.person?.firstName || "").toLowerCase()
          .localeCompare((b.person?.firstName || "").toLowerCase()));

      setEmployees(employeesExceptActual);
      setPagamentos(pays ?? []);
    } catch (error) {
      console.error(error);
      setFetchError(error.message || "Erro a carregar dados.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm]);

  const pagamentosFiltrados = useMemo(
    () => filterPagamentos(pagamentos, searchTerm),
    [pagamentos, searchTerm]
  );

  const { slice: currentPagamentos, totalPages } = useMemo(
    () => paginate(pagamentosFiltrados, currentPage, itemsPerPage),
    [pagamentosFiltrados, currentPage, itemsPerPage]
  );


  const [editOpen, setEditOpen] = useState(false);
  const [editLoading, setEditLoading] = useState(false);
  const [editError, setEditError] = useState(null);
  const [editKeys, setEditKeys] = useState({
    businessEntityID: "",
    rateChangeDate: "",
  });
  const [editForm, setEditForm] = useState({ rate: "", payFrequency: "1" });

  const openEdit = (p) => {
    const businessEntityID = p.businessEntityID ?? p.employee?.businessEntityID ?? "";
    const rateChangeDate = p.rateChangeDate ?? "";

    setEditKeys({ businessEntityID: String(businessEntityID), rateChangeDate });
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

      const { businessEntityID, rateChangeDate } = editKeys;
      const body = {
        rate: Number(editForm.rate),
        payFrequency: Number(editForm.payFrequency),
      };

      await patchPayHistory(businessEntityID, rateChangeDate, body);
      await reload();

      addNotificationForUser(
        "O seu registo de Pagamento foi atualizado.",
        editKeys.businessEntityID,
        { type: "PAYMENT" }
      );
      setEditOpen(false);
    } catch (e) {
      console.error(e);
      setEditError(e.message || "Erro ao editar registo.");
    } finally {
      setEditLoading(false);
    }
  };

  const [deleteLoadingId, setDeleteLoadingId] = useState(null);

  const handleDelete = async (p) => {
    const businessEntityID = p.businessEntityID ?? p.employee?.businessEntityID ?? "";
    const rateChangeDate = p.rateChangeDate ?? "";

    if (!businessEntityID || !rateChangeDate) {
      alert("Chaves do registo em falta.");
      return;
    }

    const confirm = window.confirm(`Eliminar registo de pay history do colaborador ${p.employee?.person?.firstName
      } ${p.employee?.person?.lastName} com data ${formatDate(rateChangeDate)}?`
    );

    if (!confirm) return;

    try {
      setDeleteLoadingId(`${businessEntityID}|${rateChangeDate}`);
      await deletePayHistory(businessEntityID, rateChangeDate);
      await reload();

      addNotificationForUser(
        "O seu registo de Pagamento foi eliminado.",
        businessEntityID,
        { type: "PAYMENT" }
      );
    } catch (e) {
      console.error(e);
      alert(e.message || "Erro ao eliminar registo.");
    } finally {
      setDeleteLoadingId(null);
    }
  };

  const [createOpen, setCreateOpen] = useState(false);
  const [createLoading, setCreateLoading] = useState(false);
  const [createError, setCreateError] = useState(null);
  const [createForm, setCreateForm] = useState({
    businessEntityID: "",
    rateChangeDate: "",
    rate: "",
    payFrequency: "1",
  });

  const resetCreateForm = () =>
    setCreateForm({
      businessEntityID: "",
      rateChangeDate: "",
      rate: "",
      payFrequency: "1",
    });

  const submitCreate = async () => {
    try {
      setCreateLoading(true);
      setCreateError(null);

      if (!createForm.businessEntityID || !createForm.rateChangeDate || !createForm.rate) {
        throw new Error("Preenche BusinessEntityID, Data e Valor.");
      }

      const body = {
        businessEntityID: Number(createForm.businessEntityID),
        rateChangeDate: dateInputToIsoMidnight(createForm.rateChangeDate),
        rate: Number(createForm.rate),
        payFrequency: Number(createForm.payFrequency),
      };

      await createPayHistory(body);
      await reload();

      addNotificationForUser(
        "Foi criado um novo registo de Pagamento associado ao seu perfil.",
        body.businessEntityID,
        { type: "PAYMENT" }
      );

      setCreateOpen(false);
      resetCreateForm();
    } catch (e) {
      console.error(e);
      setCreateError(e.message || "Erro ao criar registo.");
    } finally {
      setCreateLoading(false);
    }
  };

  return (
    <div className="container mt-4">
      <BackButton />
      <div className="mb-4 d-flex align-items-center justify-content-between">
        <h1 className="h3 mb-1">Gestão de Pagamentos</h1>

        {/* Botão Criar */}
        {!loading && (
          <button
            className="btn btn-sm btn-primary"
            onClick={() => {
              setCreateError(null);
              setCreateOpen(true);
            }}
          >
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
              placeholder="Procurar por ID, colaborador, frequência, valor ou data..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              aria-label="Pesquisar pagamentos"
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
                      <th className="px-4 py-3">Valor</th>
                      <th className="px-4 py-3 ">Data</th>
                      <th className="px-4 py-3 text-center">Frequência</th>
                      <th className="px-4 py-3 text-end">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {currentPagamentos.map((p) => {
                      const key = `${p.businessEntityID ?? p.employee?.businessEntityID
                        }|${p.rateChangeDate}`;
                      const deleting = deleteLoadingId === key;

                      return (
                        <tr key={p.payHistoryId ?? key}>
                          <td className="px-4 py-3">
                            {p.employee?.person?.firstName}{" "}
                            {p.employee?.person?.lastName}
                            <div className="small text-muted">
                              ID: {p.employee?.businessEntityID ?? "—"}
                            </div>
                          </td>
                          <td className="px-4 py-3">
                            {formatCurrencyEUR(p.rate)}
                          </td>
                          <td className="px-4 py-3 text-muted">
                            {formatDate(p.rateChangeDate)}
                          </td>
                          <td className="px-4 py-3 text-muted text-center">
                            {freqLabel(p.payFrequency)}
                          </td>
                          <td className="px-4 py-3 text-end">
                            <div className="d-flex justify-content-end gap-2">
                              <button
                                disabled={localStorage.getItem("businessEntityId") == (p.employee?.businessEntityID ?? p.businessEntityID)}
                                className="btn btn-outline-primary"
                                onClick={() => openEdit(p)}
                              >
                                Editar
                              </button>
                              <button
                                className="btn btn-outline-danger"
                                disabled={localStorage.getItem("businessEntityId") == (p.employee?.businessEntityID ?? p.businessEntityID)}
                                onClick={() => handleDelete(p)}
                              >
                                {deleting ? "A eliminar..." : "Eliminar"}
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
                {currentPagamentos.map((p) => {
                  const key = `${p.businessEntityID ?? p.employee?.businessEntityID
                    }|${p.rateChangeDate}`;
                  const deleting = deleteLoadingId === key;

                  return (
                    <div
                      key={p.payHistoryId ?? key}
                      className="border-bottom p-3"
                    >
                      <h6 className="mb-1">
                        <strong>
                          {p.employee?.person?.firstName}{" "}
                          {p.employee?.person?.lastName}
                        </strong>
                      </h6>
                      <p className="text-muted small mb-1">
                        <strong>ID:</strong>{" "}
                        {p.employee?.businessEntityID ?? "—"}
                      </p>
                      <p className="text-muted small mb-1">
                        <strong>Valor:</strong> {formatCurrencyEUR(p.rate)}
                      </p>
                      <p className="text-muted small mb-1">
                        <strong>Data:</strong> {formatDate(p.rateChangeDate)}
                      </p>
                      <p className="text-muted small mb-2">
                        <strong>Frequência:</strong> {freqLabel(p.payFrequency)}
                      </p>
                      <div className="d-flex gap-2">
                        <button
                          className="btn btn-sm btn-outline-primary"
                          onClick={() => openEdit(p)}
                        >
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
                })}
              </div>

              {/* Empty state */}
              {!currentPagamentos.length && (
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

      {editOpen && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          role="dialog"
          style={{ background: "rgba(0,0,0,0.5)" }}
          aria-modal="true"
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">Editar registo</h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setEditOpen(false)}
                  aria-label="Fechar"
                />
              </div>
              <div className="modal-body">
                {editError && (
                  <div className="alert alert-danger">{editError}</div>
                )}

                <div className="row g-3">
                  <div className="col-6">
                    <label className="form-label">ID - Funcionário</label>
                    <input
                      className="form-control"
                      value={editKeys.businessEntityID}
                      disabled
                      readOnly
                    />
                  </div>
                  <div className="col-6">
                    <label className="form-label">Data Pagamento</label>
                    <input
                      className="form-control"
                      value={formatDate(editKeys.rateChangeDate)}
                      disabled
                      readOnly
                    />
                  </div>

                  <div className="col-6">
                    <label className="form-label">Valor</label>
                    <input
                      type="number"
                      step="0.01"
                      className="form-control"
                      value={editForm.rate}
                      onChange={(e) =>
                        setEditForm((f) => ({ ...f, rate: e.target.value }))
                      }
                    />
                  </div>
                  <div className="col-6">
                    <label className="form-label">Frequência</label>
                    <select
                      className="form-select"
                      value={editForm.payFrequency}
                      onChange={(e) =>
                        setEditForm((f) => ({
                          ...f,
                          payFrequency: e.target.value,
                        }))
                      }
                    >
                      <option value="1">Mensal</option>
                      <option value="2">Quinzenal</option>
                    </select>
                  </div>
                </div>
              </div>
              <div className="modal-footer">
                <button
                  className="btn btn-outline-secondary"
                  onClick={() => setEditOpen(false)}
                >
                  Cancelar
                </button>
                <button
                  className="btn btn-primary"
                  onClick={submitEdit}
                  disabled={editLoading}
                >
                  {editLoading ? "A guardar..." : "Guardar alterações"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {createOpen && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          role="dialog"
          style={{ background: "rgba(0,0,0,0.5)" }}
          aria-modal="true"
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">Criar novo registo</h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setCreateOpen(false)}
                  aria-label="Fechar"
                />
              </div>
              <div className="modal-body">
                {createError && (
                  <div className="alert alert-danger">{createError}</div>
                )}

                <div className="row g-3">

                  <div className="row g-3">
                    <div className="col-6">
                      <label className="form-label">Funcionário</label>

                      <select
                        className="form-select"
                        value={createForm.businessEntityID ?? ""}
                        onChange={(e) =>
                          setCreateForm((f) => ({
                            ...f,
                            businessEntityID: e.target.value === "" ? null : Number(e.target.value),
                          }))
                        }
                      >


                        {employees.map((emp) => {
                          const id = emp.businessEntityID ?? emp.id;
                          const first = emp.person?.firstName ?? "";
                          const middle = emp.person?.middleName ?? "";
                          const last = emp.person?.lastName ?? "";
                          const fullName = [first, middle, last].filter(Boolean).join(" ") || "Sem nome";

                          return (
                            <option key={id} value={id}>
                              {fullName}
                            </option>
                          );
                        })}
                      </select>
                    </div>
                  </div>

                  <div className="col-6">
                    <label className="form-label">Data </label>
                    <input
                      type="date"
                      className="form-control"
                      value={createForm.rateChangeDate}
                      onChange={(e) =>
                        setCreateForm((f) => ({
                          ...f,
                          rateChangeDate: e.target.value,
                        }))
                      }
                    />
                  </div>

                  <div className="col-6">
                    <label className="form-label">Valor</label>
                    <input
                      type="number"
                      step="0.01"
                      className="form-control"
                      value={createForm.rate}
                      onChange={(e) =>
                        setCreateForm((f) => ({ ...f, rate: e.target.value }))
                      }
                    />
                  </div>
                  <div className="col-6">
                    <label className="form-label">Frequência</label>
                    <select
                      className="form-select"
                      value={createForm.payFrequency}
                      onChange={(e) =>
                        setCreateForm((f) => ({
                          ...f,
                          payFrequency: e.target.value,
                        }))
                      }
                    >
                      <option value="1">Mensal</option>
                      <option value="2">Quinzenal</option>
                    </select>
                  </div>
                </div>
              </div>
              <div className="modal-footer">
                <button
                  className="btn btn-outline-secondary"
                  onClick={() => setCreateOpen(false)}
                >
                  Cancelar
                </button>
                <button
                  className="btn btn-primary"
                  onClick={submitCreate}
                  disabled={createLoading}
                >
                  {createLoading ? "A criar..." : "Criar registo"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
