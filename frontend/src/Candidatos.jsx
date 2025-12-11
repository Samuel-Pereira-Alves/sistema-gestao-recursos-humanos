// src/pages/rh/Candidatos.jsx
import React, { useState } from "react";

function Candidatos() {
  const [candidatos] = useState([
    { 
      id: 1, 
      nome: "João Silva", 
      vaga: "Dev .NET", 
      estado: "Pendente", 
      email: "joao.silva@email.com", 
      telefone: "912345678", 
      cv: "/cvs/joao-silva.pdf" 
    },
    { 
      id: 2, 
      nome: "Maria Costa", 
      vaga: "Frontend React", 
      estado: "Pendente", 
      email: "maria.costa@email.com", 
      telefone: "987654321", 
      cv: "/cvs/maria-costa.pdf" 
    },
  ]);

    const aprovarCandidato = (id) => {
        //cahamr api que deve criar um funcionario automaticamente
        alert("Candidato aprovado e convertido em funcionário!");
    }

  return (
    <div className="container mt-4">
  <h2>Gestão de Candidatos</h2>
  {/* Scroll horizontal em ecrãs pequenos */}
  <div className="table-responsive">
    <table className="table table-striped table-hover align-middle">
      <thead className="table-dark">
        <tr>
          <th>Nome</th>
          <th>Email</th>
          <th>Telefone</th>
          <th>Vaga</th>
          <th>CV</th>
          <th>Estado</th>
          <th>Ações</th>
        </tr>
      </thead>
      <tbody>
        {candidatos.map((c) => (
          <tr key={c.id}>
            <td>{c.nome}</td>
            <td className="text-truncate" style={{ maxWidth: "200px" }}>{c.email}</td>
            <td>{c.telefone}</td>
            <td>{c.vaga}</td>
            <td>
              <a href={c.cv} target="_blank" rel="noopener noreferrer" className="btn btn-link btn-sm">
                Ver CV
              </a>
            </td>
            <td>
              <span className={`badge ${c.estado === "Pendente" ? "bg-warning" : "bg-success"}`}>
                {c.estado}
              </span>
            </td>
            <td>
              <button className="btn btn-success btn-sm me-2" onClick={() => aprovarCandidato(c.id)}>Aprovar</button>
              <button className="btn btn-danger btn-sm">Rejeitar</button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  </div>
</div>
  );
}

export default Candidatos;