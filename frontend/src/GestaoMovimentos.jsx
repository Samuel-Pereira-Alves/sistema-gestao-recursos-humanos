// src/pages/rh/GestaoMovimentacoes.jsx
import React, { useState } from "react";

function GestaoMovimentacoes() {
    const [loading, setLoading] = useState(false);
    const [searchTerm, setSearchTerm] = useState("");
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;

  const [movimentacoes, setMovimentacoes] = useState([
    { id: 1, funcionario: "João Silva", de: "TI", para: "Financeiro", data: "2025-10-01" },
    { id: 2, funcionario: "Maria Costa", de: "TI", para: "Marketing", data: "2025-11-01" },
  ]);

    const handleChange = (e) => {
    setSelectedFuncionario({
      ...selectedFuncionario,
      [e.target.name]: e.target.value,
    });
  };


    const filteredMovimentacoess = movimentacoes.filter(m =>
    m.funcionario.toLowerCase().includes(searchTerm.toLowerCase()) 
  );

  // Paginação
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const currentMovimentos = filteredMovimentacoess.slice(indexOfFirst, indexOfLast);
  const totalPages = Math.ceil(filteredMovimentacoess.length / itemsPerPage);

 return (
    <>
      <div className="container mt-4">
       
        {/* Header */}
        <div className="mb-4">
          <h1 className="h3 mb-1">Gestão de Movimentos</h1>
        </div>

        {/* Search */}
        <div className="card mb-3 border-0 shadow-sm">
          <div className="card-body">
            {loading ? (
              <div className="text-center py-5">
                <div className="spinner-border text-primary" role="status">
                  <span className="visually-hidden">Carregando...</span>
                </div>
              </div>
            ) : (
            <input
              type="text"
              className="form-control"
              placeholder="Procurar funcionários..."
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
              }}
            />
            )}
          </div>
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
                        <th className="px-4 py-3">Nome</th>
                        <th className="px-4 py-3">De</th>
                        <th className="px-4 py-3">Para</th>
                        <th className="px-4 py-3 text-center">Data</th>
                      </tr>
                    </thead>
                    <tbody>
                      {currentMovimentos.map((f) => (
                        <tr key={f.id}>
                          <td className="px-4 py-3">{f.funcionario}</td>
                          <td className="px-4 py-3 text-muted">{f.de}</td>
                          <td className="px-4 py-3 text-muted">{f.para}</td>
                          <td className="px-4 py-3 text-muted">{f.data}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Mobile Cards */}
                <div className="d-md-none">
                  {currentMovimentos.map((f) => (
                    <div key={f.id} className="border-bottom p-3">
                      <h6 className="mb-1">{f.funcionario}</h6>
                      <p className="text-muted small mb-1">{f.de}</p>
                      <p className="text-muted small mb-3">{f.para}</p>
                      <p className="text-muted small mb-0">{f.data}</p>
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


export default GestaoMovimentacoes;