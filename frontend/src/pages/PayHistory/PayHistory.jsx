import React, { useEffect, useState } from "react";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import EmployeeDetails from "../../components/EmployeeDetails/EmployeeDetails";
import Loading from "../../components/Loading/Loading";
import {formatDate,formatCurrencyEUR,freqLabel,paginate} from "../../utils/Utils";
import { getEmployee } from "../../Service/employeeService";
import { mapPayHistories } from "../../utils/Utils";
import { getPayHistoryById } from "../../Service/pagamentosService";

export default function PayHistoryList() {
  const [loading, setLoading] = useState(false);
  const [fetchError, setFetchError] = useState(null);
  const [employee, setEmployee] = useState(null);
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
        const data = await getPayHistoryById(token, id);
        console.log(data)
        setPayments(data.items.items)
        setTotalPages(data.items.meta.totalPages)
      } catch (err) {
        console.error(err);
        setFetchError(err.message || "Erro desconhecido ao obter dados.");
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
          ) : loading ? (
            <Loading text="Carregando histórico de pagamentos..." />
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
                    {payments.length === 0 ? (
                      <tr>
                        <td
                          colSpan={4}
                          className="px-4 py-4 text-center text-muted"
                        >
                          Sem registos
                        </td>
                      </tr>
                    ) : (
                      payments.map((p, idx) => {
                        const seq = (currentPage - 1) * itemsPerPage + idx + 1;
                        return (
                          <tr key={p.rateChangeDate}>
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
                {payments.length === 0 ? (
                  <div className="text-center p-3 text-muted">Sem registos</div>
                ) : (
                  payments.map((p, idx) => {
                    const seq = (currentPage - 1) * itemsPerPage + idx + 1;
                    return (
                      <div key={p.rateChangeDate} className="card mb-2 border-0 shadow-sm">
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
              <Pagination
                currentPage={currentPage}
                totalPages={totalPages}
                setPage={setCurrentPage}
              />
            </>
          )}
        </div>
      </div>
    </div>
  );
}
