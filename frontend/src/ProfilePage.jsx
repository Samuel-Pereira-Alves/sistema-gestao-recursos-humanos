// src/pages/ProfilePage.jsx
import React, { useState } from "react";

function ProfilePage() {
  const [user, setUser] = useState({
    nome: "João Silva",
    email: "joao.silva@email.com",
    telefone: "912345678",
    departamento: "Tecnologia",
    cargo: "Desenvolvedor .NET",
    salario: "2000€",
  });

  return (
    <div className="container mt-5">
      <h2>Perfil do Funcionário</h2>

      {/* Card com dados principais */}
      <div className="card mb-4 shadow-sm">
        <div className="card-body">
          <h4 className="card-title">{user.nome}</h4>
          <p className="card-text"><strong>Email:</strong> {user.email}</p>
          <p className="card-text"><strong>Telefone:</strong> {user.telefone}</p>
          <p className="card-text"><strong>Departamento:</strong> {user.departamento}</p>
          <p className="card-text"><strong>Cargo:</strong> {user.cargo}</p>
          <p className="card-text"><strong>Salário:</strong> {user.salario}</p>
        </div>
      </div>

     
    </div>
  );
}

export default ProfilePage;