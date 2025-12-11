// src/pages/rh/DashboardRH.jsx
import React from "react";
import { useNavigate } from "react-router-dom";

function DashboardRH() {
    const navigate = useNavigate();

  return (
    <div className="container mt-4">
      <h2>Dashboard RH</h2>
      <div className="row g-3">
        <div className="col-md-4">
          <div className="card p-3 shadow-sm"
            style={{cursor: "pointer"}}
            onClick={() => navigate("/candidatos")}
          >
            <h5>Candidatos</h5>
            <p>120 ativos</p>
          </div>
        </div>
        <div className="col-md-4">
          <div className="card p-3 shadow-sm">
            <h5>Funcionários</h5>
            <p>85 ativos</p>
          </div>
        </div>
        <div className="col-md-4">
          <div className="card p-3 shadow-sm">
            <h5>Vagas abertas</h5>
            <p>6 em curso</p>
          </div>
        </div>
      </div>
    </div>
  );
}

export default DashboardRH;