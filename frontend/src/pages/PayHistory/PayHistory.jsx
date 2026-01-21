import React, { useEffect, useState } from "react";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import Loading from "../../components/Loading/Loading";
import {
  formatDate,
  formatCurrencyEUR,
  freqLabel,
  paginate,
} from "../../utils/Utils";
import { getPayHistoryById } from "../../Service/pagamentosService";
import ReadOnlyField from "../../components/ReadOnlyField/ReadOnlyField";

export default function PayHistoryList() {
  const [loading, setLoading] = useState(false);
  const [fetchError, setFetchError] = useState(null);
  const [payments, setPayments] = useState([]);
  const [totalPages, setTotalPages] = useState(0);

  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;

  useEffect(() => {
    const id = localStorage.getItem("businessEntityId");
    if (!id) {
      setFetchError("ID do funcionário não encontrado no localStorage.");
      return;
    }

    const load = async () => {
      const token = localStorage.getItem("authToken");
      setLoading(true);
      setFetchError(null);
      try {
        const data = await getPayHistoryById(token, id, {
          pageNumber: currentPage,
          pageSize: itemsPerPage,
        });
        setPayments(data.items);
        setTotalPages(data.meta.totalPages);
      } catch (err) {
        console.error(err);
        //setFetchError(err.message || "Erro desconhecido ao obter dados.");
      } finally {
        setLoading(false);
      }
    };

    load();
  }, []);

return (
  <div className="container mt-4">
    <BackButton />
    
    {/* Header */}
    <div className="mb-4 d-flex justify-content-between align-items-center">
      <h1 className="h3 mb-0">Histórico de Pagamentos</h1>
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
        ) : loading && payments.length === 0 ? (
          <Loading text="Carregando histórico de pagamentos..." />
        ) : (
          <>
            {/* Desktop Table */}
            <div className="table-responsive d-none d-md-block">
              <table className="table table-hover mb-0">
                <thead className="table-light">
                  <tr>
                    <th>Pagamento</th>
                    <th>Valor</th>
                    <th>Data</th>
                    <th>Frequência</th>
                  </tr>
                </thead>
                <tbody>
                  {payments.length === 0 ? (
                    <tr>
                      <td colSpan={4} className="text-center text-muted">
                        Registos não encontrados.
                      </td>
                    </tr>
                  ) : (
                    payments.map((p, idx) => {
                      const seq = (currentPage - 1) * itemsPerPage + idx + 1;
                      return (
                        <tr key={p.rateChangeDate}>
                          <td>{seq}</td>
                          <td className="text-muted">
                            {formatCurrencyEUR(p.rate)}
                          </td>
                          <td className="text-muted">
                            {formatDate(p.rateChangeDate)}
                          </td>
                          <td className="text-muted">
                            {freqLabel(p.payFrequency)}
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>

            {/* Mobile Cards (no mesmo estilo do Histórico de Departamentos) */}
            <div className="d-md-none">
              {payments.length === 0 ? (
                <div className="text-center p-3 text-muted">
                  Registos não encontrados.
                </div>
              ) : (
                payments.map((p, idx) => {
                  const seq = (currentPage - 1) * itemsPerPage + idx + 1;
                  return (
                    <div key={p.rateChangeDate} className="border-bottom p-3">
                      <h6>Pagamento {seq}</h6>

                      {/* Grupo/Frequência em texto secundário */}
                      {p.payFrequency != null && (
                        <p className="text-muted small">
                          {freqLabel(p.payFrequency)}
                        </p>
                      )}

                      {/* Campos no padrão ReadOnlyField */}
                      <ReadOnlyField
                        label="Data"
                        value={formatDate(p.rateChangeDate)}
                      />
                      <ReadOnlyField
                        label="Valor"
                        value={formatCurrencyEUR(p.rate)}
                      />
                    </div>
                  );
                })
              )}
            </div>

            {/* Pagination */}
            {payments.length > 0 ? (
              <Pagination
                currentPage={currentPage}
                totalPages={totalPages}
                setPage={setCurrentPage}
              />
            ) : (
              <></>
            )}
          </>
        )}
      </div>
    </div>
  </div>
);

}
