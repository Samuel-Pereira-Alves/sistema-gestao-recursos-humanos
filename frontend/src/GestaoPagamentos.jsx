// src/pages/rh/GestaoMovimentacoes.jsx
import React, { useState, useEffect } from "react";

function GestaoMovimentacoes() {
  const [movimentacoes, setMovimentacoes] = useState([]);
  const [loading, setLoading] = useState(true);

  // estados para paginação
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10; // nº de registos por página

  useEffect(() => {
    const fetchMovimentacoes = async () => {
      try {
        const response = await fetch("http://localhost:5136/api/v1/payhistory");
        if (!response.ok) throw new Error("Erro ao carregar movimentações");
        const data = await response.json();
        setMovimentacoes(data);
      } catch (error) {
        console.error(error);
      } finally {
        setLoading(false);
      }
    };

    fetchMovimentacoes();
  }, []);

  if (loading) return <p className="text-center mt-5">Carregando movimentações...</p>;

  // cálculo da paginação
  const totalPages = Math.ceil(movimentacoes.length / itemsPerPage);
  const startIndex = (currentPage - 1) * itemsPerPage;
  const currentItems = movimentacoes.slice(startIndex, startIndex + itemsPerPage);

  return (
    <div className="container mt-4">
      <h2>Gestão de Movimentações</h2>
      <table className="table table-striped">
        <thead>
          <tr>
            <th>Funcionário</th>
            <th>Data Alteração</th>
            <th>Salário</th>
            <th>Frequência</th>
          </tr>
        </thead>
        <tbody>
          {currentItems.map((m, idx) => (
            <tr key={idx}>
              <td>{m.employee?.jobTitle || `ID ${m.businessEntityID}`}</td>
              <td>{new Date(m.rateChangeDate).toLocaleDateString("pt-PT")}</td>
              <td>{m.rate}</td>
              <td>{m.payFrequency}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* Pagination */}
      <div className="border-top p-3">
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
    </div>
  );
}

export default GestaoMovimentacoes;