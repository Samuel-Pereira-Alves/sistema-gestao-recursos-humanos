// src/pages/rh/GestaoMovimentacoes.jsx
import React, { useState } from "react";

function GestaoMovimentacoes() {
  const [movimentacoes] = useState([
    { id: 1, funcionario: "João Silva", de: "TI", para: "Financeiro", data: "2025-10-01" },
    { id: 2, funcionario: "Maria Costa", de: "TI", para: "Marketing", data: "2025-11-01" },
  ]);

  return (
    <div className="container mt-4">
      <h2>Gestão de Movimentações</h2>
      <table className="table table-striped">
        <thead>
          <tr>
            <th>Funcionário</th>
            <th>De</th>
            <th>Para</th>
            <th>Data</th>
          </tr>
        </thead>
        <tbody>
          {movimentacoes.map(m => (
            <tr key={m.id}>
              <td>{m.funcionario}</td>
              <td>{m.de}</td>
              <td>{m.para}</td>
              <td>{m.data}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default GestaoMovimentacoes;