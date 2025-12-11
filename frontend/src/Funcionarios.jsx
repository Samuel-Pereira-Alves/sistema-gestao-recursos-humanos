// src/pages/rh/Funcionarios.jsx
import React, { useState } from "react";

function Funcionarios() {
  const [funcionarios] = useState([
    { id: 1, nome: "João Silva", cargo: "Dev .NET", departamento: "TI" },
    { id: 2, nome: "Maria Costa", cargo: "Frontend React", departamento: "TI" },
  ]);

  return (
    <div className="container mt-4">
      <h2>Gestão de Funcionários</h2>
      <table className="table table-striped">
        <thead>
          <tr>
            <th>Nome</th>
            <th>Cargo</th>
            <th>Departamento</th>
            <th>Ações</th>
          </tr>
        </thead>
        <tbody>
          {funcionarios.map(f => (
            <tr key={f.id}>
              <td>{f.nome}</td>
              <td>{f.cargo}</td>
              <td>{f.departamento}</td>
              <td>
                <button className="btn btn-info btn-sm">Ver Perfil</button>
                <button className="btn btn-warning btn-sm ms-2">Editar</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default Funcionarios;