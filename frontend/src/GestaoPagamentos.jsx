// src/pages/rh/GestaoMovimentacoes.jsx
import React, { useEffect, useState } from "react";

function GestaoPagamentos() {
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 7;

  const [pagamentos, setPagamentos] = useState([]);

  const handleChange = (e) => {
    setSelectedFuncionario({
      ...selectedFuncionario,
      [e.target.name]: e.target.value,
    });
  };

  useEffect(() => {
    const fetchPagamentos = async () => {
      try {
        const response = await fetch("http://localhost:5136/api/v1/payhistory");
        if (!response.ok) throw new Error("Erro ao carregar movimentações");
        const data = await response.json();
        setPagamentos(data);
      } catch (error) {
        console.error(error);
      } finally {
        setLoading(false);
      }
    };

    fetchPagamentos();
  }, []);

  // Paginação
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const currentPagamentos = pagamentos.slice(indexOfFirst, indexOfLast);
  const totalPages = Math.ceil(pagamentos.length / itemsPerPage);

  return (
    <>
      <div className="container mt-4">

        {/* Header */}
        <div className="mb-4">
          <h1 className="h3 mb-1">Gestão de Pagamentos</h1>
        </div>

        {/* Content */}
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
                        <th className="px-4 py-3">Id</th>
                        <th className="px-4 py-3">Data de Mudanca</th>
                        <th className="px-4 py-3">Rate</th>
                        <th className="px-4 py-3 text-center">Frequencia</th>
                      </tr>
                    </thead>
                    <tbody>
                      {currentPagamentos.map((p) => (
                        <tr key={p.id}>
                          <td className="px-4 py-3">{p.businessEntityID}</td>
                          <td className="px-4 py-3 text-muted">{p.rateChangeDate}</td>
                          <td className="px-4 py-3 text-muted">{p.rate}</td>
                          <td className="px-4 py-3 text-muted text-center">{p.payFrequency}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Mobile Cards */}
                <div className="d-md-none">
                  {currentPagamentos.map((p) => (
                    <div key={p.id} className="border-bottom p-3">
                      <h6 className="mb-1">
                        <strong>ID:</strong> {p.businessEntityID}
                      </h6>
                      <p className="text-muted small mb-1">
                        <strong>Data de Mudança:</strong> {p.rateChangeDate}
                      </p>
                      <p className="text-muted small mb-1">
                        <strong>Rate:</strong> {p.rate}
                      </p>
                      <p className="text-muted small mb-0">
                        <strong>Frequência:</strong> {p.payFrequency}
                      </p>
                    </div>
                  ))}
                </div>

                {/* Pagination */}
                <div className="border-top p-3 ">
                  <div className="d-flex justify-content-between align-items-center">
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === 1}
                      onClick={() => setCurrentPage(currentPage - 1)}
                    >
                      ← Anterior
                    </button>
                    <span className="text-muted small">
                      Página {currentPage} de {totalPages}
                    </span>
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === totalPages}
                      onClick={() => setCurrentPage(currentPage + 1)}
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


export default GestaoPagamentos;