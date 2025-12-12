
import React, { useEffect, useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

/* Utils */
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
    case 1: return "Mensal";
    case 2: return "Semanal";
    case 3: return "Quinzenal";
    case 4: return "Anual";
    default: return code != null ? `Código ${code}` : "—";
  }
}

export default function PayHistoryList() {
  const [loading, setLoading] = useState(false);
  const [fetchError, setFetchError] = useState(null);

  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  const [payments, setPayments] = useState([]);

  const [employeeId, setEmployeeId] = useState(null);
  const [employeeName, setEmployeeName] = useState(null); // só para o header

  // Lê o ID e carrega o histórico
  useEffect(() => {
    const id = localStorage.getItem("businessEntityId");
    setEmployeeId(id);

    if (!id) {
      setFetchError("ID do funcionário não encontrado no localStorage.");
      return;
    }

    const controller = new AbortController();

    async function load() {
      setLoading(true);
      setFetchError(null);
      try {
        const res = await fetch(`http://localhost:5136/api/v1/payhistory/${id}`, {
          signal: controller.signal,
          headers: { Accept: "application/json" },
        });
        if (!res.ok) throw new Error(`Erro ao carregar pagamentos (HTTP ${res.status})`);
        const data = await res.json();

        // Aceita array direto ou wrapper { payHistories: [...] }
        const list = (Array.isArray(data) ? data : (data?.payHistories ?? []))
          .slice()
          .sort((a, b) => new Date(b.rateChangeDate) - new Date(a.rateChangeDate))
          .map((p, idx) => ({
            key: `${p.payHistoryId ?? idx}-${p.employeeId}-${p.rateChangeDate ?? idx}`,
            payHistoryId: p.payHistoryId ?? null,
            employeeId: p.employeeId ?? null, // só útil internamente
            rateChangeDate: p.rateChangeDate ?? null,
            rate: p.rate ?? null,
            payFrequency: p.payFrequency ?? null,
          }));

        setPayments(list);
      } catch (err) {
        if (err.name === "AbortError") return;
        console.error(err);
        setFetchError(err.message || "Erro desconhecido ao obter dados.");
      } finally {
        setLoading(false);
      }
    }

    load();
    return () => controller.abort();
  }, []);

  // Busca nome do colaborador (APENAS para o header), com cache por ID
  useEffect(() => {
    if (!employeeId) return;

    const cacheKey = `userName:${employeeId}`;
    const cached = localStorage.getItem(cacheKey);
    if (cached) {
      setEmployeeName(cached);
      return;
    }

    let cancelled = false;
    const controller = new AbortController();

    async function loadEmployeeName() {
      try {
        const res = await fetch(`http://localhost:5136/api/v1/employee/${employeeId}`, {
          signal: controller.signal,
          headers: { Accept: "application/json" },
        });
        if (!res.ok) throw new Error(`Falha ao obter utilizador (HTTP ${res.status})`);
        const data = await res.json();

        const first = data?.person?.firstName || "";
        const middle = data?.person?.middleName || "";
        const last = data?.person?.lastName || "";
        const full = [first, middle, last].filter(Boolean).join(" ").trim() || "Utilizador";

        if (!cancelled) {
          setEmployeeName(full);
          localStorage.setItem(cacheKey, full); // cache por ID
        }
      } catch (err) {
        if (err.name === "AbortError") return;
        console.error(err);
      }
    }

    loadEmployeeName();
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [employeeId]);

  // Paginação
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const currentPayments = payments.slice(indexOfFirst, indexOfLast);
  const totalPages = Math.max(1, Math.ceil(payments.length / itemsPerPage));

  return (
    <>
      <div className="container mt-4">
        {/* Header (mantido) */}
        <div className="mb-4 d-flex justify-content-between align-items-center">
          <h1 className="h3 mb-0">Histórico de Pagamentos</h1>
          <div className="text-muted small">
            {employeeId ? (
              <>
                Funcionário:{" "}
                <span className="badge bg-secondary bg-opacity-50 text-dark">
                  {employeeName ?? "—"}
                </span>
                {employeeId != null && employeeId !== "" && (
                  <span className="ms-1 text-muted">#{employeeId}</span>
                )}
              </>
            ) : (
              <>Sem ID no localStorage</>
            )}
          </div>
        </div>

        {/* Content */}
        <div className="card border-0 shadow-sm">
          <div className="card-body p-0">
            {fetchError ? (
              <div className="text-center py-5">
                <div className="alert alert-light border text-muted d-inline-block">{fetchError}</div>
              </div>
            ) : loading ? (
              <div className="text-center py-5" aria-live="polite">
                <div className="spinner-border text-secondary" role="status">
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
                        <th className="px-4 py-3">Pagamento</th>
                        <th className="px-4 py-3">Valor</th>
                        <th className="px-4 py-3">Data</th>
                        <th className="px-4 py-3">Frequência</th>
                      </tr>
                    </thead>
                    <tbody>
                      {currentPayments.length === 0 ? (
                        <tr>
                          <td colSpan={4} className="px-4 py-4 text-center text-muted">
                            Sem registos
                          </td>
                        </tr>
                      ) : (
                        currentPayments.map((p, idx) => {
                          const seq = indexOfFirst + idx + 1; // numeração contínua global
                          return (
                            <tr key={p.key}>
                              <td className="px-4 py-3">{seq}</td>
                              <td className="px-4 py-3 text-muted">{formatCurrencyEUR(p.rate)}</td>
                              <td className="px-4 py-3 text-muted">{formatDate(p.rateChangeDate)}</td>
                              <td className="px-4 py-3 text-muted">{freqLabel(p.payFrequency)}</td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>

                {/* Mobile Cards */}
                <div className="d-md-none">
                  {currentPayments.length === 0 ? (
                    <div className="text-center p-3 text-muted">Sem registos</div>
                  ) : (
                    currentPayments.map((p, idx) => {
                      const seq = indexOfFirst + idx + 1;
                      return (
                        <div key={p.key} className="card mb-2 border-0 shadow-sm">
                          <div className="card-body">
                            <div className="d-flex justify-content-between align-items-start">
                              <div className="fw-semibold">Pagamento {seq}</div>
                              <span className="badge bg-secondary">{freqLabel(p.payFrequency)}</span>
                            </div>
                            <div className="mt-2 small text-muted">
                              <span className="me-3">Data: {formatDate(p.rateChangeDate)}</span>
                              <span className="fw-semibold text-dark">
                                Valor: {formatCurrencyEUR(p.rate)}
                              </span>
                            </div>
                          </div>
                        </div>
                      );
                    })
                  )}
                </div>

                {/* Pagination */}
                <div className="d-flex justify-content-between align-items-center mt-3 px-3 pb-3">
                  <div className="small text-muted">
                    A mostrar {currentPayments.length} de {payments.length}
                  </div>
                  <nav aria-label="Paginação">
                    <ul className="pagination mb-0">
                      <li className={`page-item ${currentPage === 1 ? "disabled" : ""}`}>
                        <button
                          className="page-link"
                          onClick={() => setCurrentPage(1)}
                          aria-label="Primeira"
                        >
                          «
                        </button>
                      </li>
                      <li className={`page-item ${currentPage === 1 ? "disabled" : ""}`}>
                        <button
                          className="page-link"
                          onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                          aria-label="Anterior"
                        >
                          ‹
                        </button>
                      </li>
                      <li className="page-item disabled">
                        <span className="page-link">
                          Página {currentPage} de {totalPages}
                        </span>
                      </li>
                      <li className={`page-item ${currentPage >= totalPages ? "disabled" : ""}`}>
                        <button
                          className="page-link"
                          onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                          aria-label="Seguinte"
                        >
                          ›
                        </button>
                      </li>
                      <li className={`page-item ${currentPage >= totalPages ? "disabled" : ""}`}>
                        <button
                          className="page-link"
                          onClick={() => setCurrentPage(totalPages)}
                          aria-label="Última"
                        >
                          »
                        </button>
                      </li>
                    </ul>
                  </nav>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </>
  );
}
