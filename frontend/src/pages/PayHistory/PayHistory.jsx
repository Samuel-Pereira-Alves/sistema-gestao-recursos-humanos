import React, { useEffect, useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";

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
    case 1:
      return "Mensal";
    case 2:
      return "Semanal";
    case 3:
      return "Quinzenal";
    case 4:
      return "Anual";
    default:
      return code != null ? `Código ${code}` : "—";
  }
}

export default function PayHistoryList() {
  const [loading, setLoading] = useState(false);
  const [fetchError, setFetchError] = useState(null);

  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  const [payments, setPayments] = useState([]);

  const [employeeId, setEmployeeId] = useState(null);
  const [employee, setEmployee] = useState(null);

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
      const token = localStorage.getItem("authToken");
      setLoading(true);
      setFetchError(null);
      try {
        const res = await fetch(`http://localhost:5136/api/v1/employee/${id}`, {
          signal: controller.signal,
          headers: { Accept: "application/json",
                    "Authorization": `Bearer ${token}`,
           },
        });
        if (!res.ok)
          throw new Error(`Erro ao carregar pagamentos (HTTP ${res.status})`);
        const data = await res.json();

        if (!Array.isArray(data)) {
          setEmployee(data); // data tem person e payHistories
        }

        // Aceita array direto ou wrapper { payHistories: [...] }
        const list = (Array.isArray(data) ? data : data?.payHistories ?? [])
          .slice()
          .sort(
            (a, b) => new Date(b.rateChangeDate) - new Date(a.rateChangeDate)
          )
          .map((p, idx) => ({
            key: `${p.payHistoryId ?? idx}-${p.employeeId}-${
              p.rateChangeDate ?? idx
            }`,
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


  // Paginação
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const currentPayments = payments.slice(indexOfFirst, indexOfLast);
  const totalPages = Math.max(1, Math.ceil(payments.length / itemsPerPage));

  return (
    <>
      <div className="container mt-4">
        <BackButton />
        {/* Header (mantido) */}
        <div className="mb-4 d-flex justify-content-between align-items-center">
          <h1 className="h3 mb-0">Histórico de Pagamentos</h1>
          <div className="text-muted small">
            {employee?.person ? (
              <>
                Funcionário:{" "}
                <span>
                  {employee.person.firstName} {employee.person.lastName}
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
                <div className="alert alert-light border text-muted d-inline-block">
                  {fetchError}
                </div>
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
                          <td
                            colSpan={4}
                            className="px-4 py-4 text-center text-muted"
                          >
                            Sem registos
                          </td>
                        </tr>
                      ) : (
                        currentPayments.map((p, idx) => {
                          const seq = indexOfFirst + idx + 1; // numeração contínua global
                          return (
                            <tr key={p.key}>
                              <td className="px-4 py-3">{seq}</td>
                              <td className="px-4 py-3 text-muted">
                                {formatCurrencyEUR(p.rate)}
                              </td>
                              <td className="px-4 py-3 text-muted">
                                {formatDate(p.rateChangeDate)}
                              </td>
                              <td className="px-4 py-3 text-muted">
                                {freqLabel(p.payFrequency)}
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
                  {currentPayments.length === 0 ? (
                    <div className="text-center p-3 text-muted">
                      Sem registos
                    </div>
                  ) : (
                    currentPayments.map((p, idx) => {
                      const seq = indexOfFirst + idx + 1;
                      return (
                        <div
                          key={p.key}
                          className="card mb-2 border-0 shadow-sm"
                        >
                          <div className="card-body">
                            <div className="d-flex justify-content-between align-items-start">
                              <div className="fw-semibold">Pagamento {seq}</div>
                              <span className="badge bg-secondary">
                                {freqLabel(p.payFrequency)}
                              </span>
                            </div>
                            <div className="mt-2 small text-muted">
                              <span className="me-3">
                                Data: {formatDate(p.rateChangeDate)}
                              </span>
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
                <div className="border-top p-3">
                  <div className="d-flex justify-content-between align-items-center">
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === 1}
                      onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                      type="button"
                    >
                      ← Anterior
                    </button>
                    <span className="text-muted small">
                      Página {currentPage} de {totalPages}
                    </span>
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === totalPages}
                      onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                      type="button"
                    >
                      Próxima →
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </>
  );
}
