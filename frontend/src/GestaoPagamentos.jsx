// src/pages/rh/GestaoPagamentos.jsx
import React, { useState } from "react";

function GestaoPagamentos() {
  const [pagamentos] = useState([
    { id: 1, funcionario: "João Silva", data: "2025-11-01", valor: "2000€" },
    { id: 2, funcionario: "Maria Costa", data: "2025-11-01", valor: "1800€" },
  ]);

  return (
    <div className="container mt-4">
      <h2>Gestão de Pagamentos</h2>
      <table className="table table-striped">
        <thead>
          <tr>
            <th>Funcionário</th>
            <th>Data</th>
            <th>Valor</th>
            <th>Ações</th>
          </tr>
        </thead>
        <tbody>
          {pagamentos.map(p => (
            <tr key={p.id}>
              <td>{p.funcionario}</td>
              <td>{p.data}</td>
              <td>{p.valor}</td>
              <td>
                <button className="btn btn-warning btn-sm">Editar</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default GestaoPagamentos;