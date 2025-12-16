import React, { useEffect, useMemo, useState, useCallback } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import { addNotificationForUser } from "./store/notificationBus";

/* Utils de apresentação */
function formatDate(dateStr) {
  if (!dateStr) return "—";
  const d = new Date(dateStr);
  if (isNaN(d)) return "—";
  return d.toLocaleDateString("pt-PT");
}
function formatCurrencyEUR(value) {
  if (value == null) return "—";
  const n = Number(value);
  if (Number.isNaN(n)) return "—";
  return n.toLocaleString("pt-PT", { style: "currency", currency: "EUR" });
}
function freqLabel(code) {
  switch (Number(code)) {
    case 1:
      return "Mensal";
    case 2:
      return "Semanal";
    default:
      return code != null ? `Código ${code}` : "—";
  }
}

/* Utils de comparação / formatação */
function normalizarTexto(t) {
  if (!t) return "";
  return String(t)
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}
function toIdString(id) {
  if (id == null) return "";
  return String(id).trim(); // não converte para número (protege "004")
}
/** Converte "yyyy-MM-dd" para ISO "yyyy-MM-ddT00:00:00" (para POST) */
function dateInputToIsoMidnight(dateStr) {
  if (!dateStr) return "";
  return `${dateStr}T00:00:00`;
}

export default function GestaoPagamentos() {
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState(null);

  
  const [searchTerm, setSearchTerm] = useState("");

  
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 7;

  const [pagamentos, setPagamentos] = useState([]);

  // 🔁 Função reutilizável para ir buscar dados
  const fetchPagamentos = useCallback(async () => {
    try {
      setLoading(true);
      setFetchError(null);

      const response = await fetch("http://localhost:5136/api/v1/employee", {
        headers: { Accept: "application/json" },
      });
      if (!response.ok) throw new Error("Erro ao carregar movimentações");

      const data = await response.json();
      const employees = Array.isArray(data) ? data : data?.items ?? [];

      const flattened = employees.flatMap((emp) =>
        (emp.payHistories ?? []).map((ph) => ({
          ...ph,
          employee: emp, 
        }))
      );

      flattened.sort(
        (a, b) => new Date(b.rateChangeDate) - new Date(a.rateChangeDate)
      );
      setPagamentos(flattened);
    } catch (error) {
      setFetchError(
        error.message || "Erro desconhecido ao obter movimentações."
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchPagamentos();
  }, [fetchPagamentos]);

  /** Reset para a primeira página quando muda o termo de pesquisa */
  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm]);

  /** Filtro:
   *  - ID numérico: employee.businessEntityID (igualdade exata)
   *  - Texto: colaborador, frequência, valor e data (insensível a acentos)
   */
  const rawSearch = searchTerm.trim();
  const isNumericSearch = /^\d+$/.test(rawSearch);
  const termo = normalizarTexto(searchTerm);

  const pagamentosFiltrados = useMemo(() => {
    if (!rawSearch) return pagamentos;

    return pagamentos.filter((p) => {
      if (isNumericSearch) {
        const businessEntityIdStr = toIdString(p.employee?.businessEntityID);
        return businessEntityIdStr === rawSearch;
      }

      const firstName = normalizarTexto(p.employee?.person?.firstName);
      const lastName = normalizarTexto(p.employee?.person?.lastName);
      const fullName = `${firstName} ${lastName}`.trim();

      const freq = normalizarTexto(freqLabel(p.payFrequency));
      const valor = normalizarTexto(formatCurrencyEUR(p.rate));
      const data = normalizarTexto(formatDate(p.rateChangeDate));

      return (
        (fullName && fullName.includes(termo)) ||
        (freq && freq.includes(termo)) ||
        (valor && valor.includes(termo)) ||
        (data && data.includes(termo))
      );
    });
  }, [pagamentos, rawSearch, termo, isNumericSearch]);

  
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const currentPagamentos = pagamentosFiltrados.slice(
    indexOfFirst,
    indexOfLast
  );
  const totalPages = Math.max(
    1,
    Math.ceil(pagamentosFiltrados.length / itemsPerPage)
  );

  /* =========================
   *  Ações: Editar / Eliminar / Criar
   * ========================= */

  // Editar
  const [editOpen, setEditOpen] = useState(false);
  const [editLoading, setEditLoading] = useState(false);
  const [editError, setEditError] = useState(null);
  const [editKeys, setEditKeys] = useState({
    businessEntityID: "",
    rateChangeDate: "",
  }); // não editáveis
  const [editForm, setEditForm] = useState({ rate: "", payFrequency: "1" });

  const openEdit = (p) => {
    const businessEntityID =
      p.businessEntityID ?? p.employee?.businessEntityID ?? ""; 
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
      const url = `http://localhost:5136/api/v1/payhistory/${encodeURIComponent(
        businessEntityID
      )}/${encodeURIComponent(rateChangeDate)}`;

      const body = {
    
        rate: Number(editForm.rate),
        payFrequency: Number(editForm.payFrequency),
      };

      const resp = await fetch(url, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify(body),
      });

      if (!resp.ok) {
        const text = await resp.text();
        throw new Error(text || "Falha ao editar registo.");
      }
    
           
       addNotificationForUser("O seu registo foi atualizado com sucesso.", businessEntityID);

      await fetchPagamentos(); 
      setEditOpen(false);
    } catch (e) {
      setEditError(e.message || "Erro ao editar registo.");
    } finally {
      setEditLoading(false);
    }
  };

  // Eliminar
  const [deleteLoadingId, setDeleteLoadingId] = useState(null); 

  const handleDelete = async (p) => {
    const businessEntityID =
      p.businessEntityID ?? p.employee?.businessEntityID ?? "";
    const rateChangeDate = p.rateChangeDate ?? "";

    if (!businessEntityID || !rateChangeDate) {
      alert("Chaves do registo em falta.");
      return;
    }

    const confirm = window.confirm(
      `Eliminar registo de pay history do colaborador ${
        p.employee?.person?.firstName
      } ${p.employee?.person?.lastName} com data ${formatDate(rateChangeDate)}?`
    );
    if (!confirm) return;

    const url = `http://localhost:5136/api/v1/payhistory/${encodeURIComponent(
      businessEntityID
    )}/${encodeURIComponent(rateChangeDate)}`;

    try {
      setDeleteLoadingId(`${businessEntityID}|${rateChangeDate}`);
      const resp = await fetch(url, { method: "DELETE" }); 
      if (!resp.ok) {
        const text = await resp.text();
        throw new Error(text || "Falha ao eliminar registo.");
      }
      await fetchPagamentos();
    } catch (e) {
      alert(e.message || "Erro ao eliminar registo.");
    } finally {
      setDeleteLoadingId(null);
    }
  };

  // Criar
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

      if (
        !createForm.businessEntityID ||
        !createForm.rateChangeDate ||
        !createForm.rate
      ) {
        throw new Error("Preenche BusinessEntityID, Data e Valor.");
      }

      const body = {
        businessEntityID: Number(createForm.businessEntityID),
        rateChangeDate: dateInputToIsoMidnight(createForm.rateChangeDate), // "yyyy-MM-ddT00:00:00"
        rate: Number(createForm.rate),
        payFrequency: Number(createForm.payFrequency),
      };

      const resp = await fetch("http://localhost:5136/api/v1/payhistory", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify(body),
      });

      if (!resp.ok) {
        const text = await resp.text();
        throw new Error(text || "Falha ao criar registo.");
      }

      await fetchPagamentos();
      setCreateOpen(false);
      resetCreateForm();
    } catch (e) {
      setCreateError(e.message || "Erro ao criar registo.");
    } finally {
      setCreateLoading(false);
    }
  };

  /* ============== UI ============== */

  return (
    <div className="container mt-4">
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
              placeholder="Procurar por ID (businessEntityID), colaborador, frequência, valor ou data..."
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
                      <th className="px-4 py-3">Data</th>
                      <th className="px-4 py-3 text-center">Frequência</th>
                      <th className="px-4 py-3 text-end">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {currentPagamentos.map((p) => {
                      const key = `${
                        p.businessEntityID ?? p.employee?.businessEntityID
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
                            <div className="btn-group btn-group-sm">
                              <button
                                className="btn btn-outline-primary"
                                onClick={() => openEdit(p)}
                              >
                                Editar
                              </button>
                              <button
                                className="btn btn-outline-danger"
                                disabled={deleting}
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
                  const key = `${
                    p.businessEntityID ?? p.employee?.businessEntityID
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

              {/* Pagination + botão criar em baixo */}
              {!!currentPagamentos.length && (
                <div className="border-top p-3 d-flex flex-wrap gap-2 justify-content-between align-items-center">
                  <div className="d-flex align-items-center gap-2">
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === 1}
                      onClick={() => setCurrentPage((p) => p - 1)}
                    >
                      ← Anterior
                    </button>
                    <span className="text-muted small">
                      Página {currentPage} de {totalPages}
                    </span>
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === totalPages}
                      onClick={() => setCurrentPage((p) => p + 1)}
                    >
                      Próxima →
                    </button>
                  </div>

                </div>
              )}
            </>
          )}
        </div>
      </div>

      {fetchError && (
        <div className="alert alert-danger mt-3">{fetchError}</div>
      )}

      {/* ======= MODAL: Editar ======= */}
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
                      <option value="2">Semanal</option>
                      <option value="3">Quinzenal</option>
                      <option value="4">Anual</option>
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

      {/* ======= MODAL: Criar ======= */}
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
                  <div className="col-6">
                    <label className="form-label">ID - Funcionário</label>
                    <input
                      type="number"
                      className="form-control"
                      value={createForm.businessEntityID}
                      onChange={(e) =>
                        setCreateForm((f) => ({
                          ...f,
                          businessEntityID: e.target.value,
                        }))
                      }
                    />
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
                      <option value="2">Semanal</option>
                      <option value="3">Quinzenal</option>
                      <option value="4">Anual</option>
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
