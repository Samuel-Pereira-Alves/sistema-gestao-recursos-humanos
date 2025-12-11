// src/pages/rh/Candidatos.jsx
import React, { useState } from "react";

function Candidatos() {
  const [candidatos] = useState([
    { id: 1, nome: "João Silva", vaga: "Dev .NET", estado: "Pendente" },
    { id: 2, nome: "Maria Costa", vaga: "Frontend React", estado: "Pendente" },
  ]);

  return (
    <div className="container mt-4">
      <h2>Gestão de Candidatos</h2>
      <table className="table table-striped">
        <thead>
          <tr>
            <th>Nome</th>
            <th>Vaga</th>
            <th>Estado</th>
            <th>Ações</th>
          </tr>
        </thead>
        <tbody>
          {candidatos.map((c) => (
            <tr key={c.id}>
              <td>{c.nome}</td>
              <td>{c.vaga}</td>
              <td>{c.estado}</td>
              <td>
                <button className="btn btn-success btn-sm me-2">Aprovar</button>
                <button className="btn btn-danger btn-sm">Rejeitar</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default Candidatos;