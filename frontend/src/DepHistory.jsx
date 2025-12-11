import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom"; // hook de navegação
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap-icons/font/bootstrap-icons.css"; // ícones Bootstrap

function DepartmentHistoryList() {
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    // Simulação de dados locais (mock) com base no modelo DepartmentHistory
    const mockData = [
      {
        departmentHistoryId: 1,
        employeeId: 101,
        departmentId: 10,
        startDate: "2023-01-01T00:00:00",
        endDate: "2024-06-30T00:00:00",
      },
      {
        departmentHistoryId: 2,
        employeeId: 102,
        departmentId: 20,
        startDate: "2024-07-01T00:00:00",
        endDate: null, // ainda ativo
      },
      {
        departmentHistoryId: 3,
        employeeId: 103,
        departmentId: 30,
        startDate: "2022-05-15T00:00:00",
        endDate: "2025-01-01T00:00:00",
      },
    ];

    setHistory(mockData);
    setLoading(false);
  }, []);

  if (loading) return <p>Carregando histórico de departamentos...</p>;

  return (
    <div className="container mt-4">
      {/* Barra superior com seta */}
      <div className="d-flex align-items-center mb-3">
        <button
          className="btn btn-link text-decoration-none text-dark"
          onClick={() => navigate(-1)}
        >
          <i className="bi bi-arrow-left fs-4"></i>
        </button>
        <h2 className="ms-2 mb-0 text-primary fw-bold">🏢 Histórico de Departamentos</h2>
      </div>

      <table className="table table-striped table-hover shadow-sm">
        <thead className="table-dark">
          <tr>
            <th>ID</th>
            <th>Colaborador</th>
            <th>Departamento</th>
            <th>Data de Início</th>
            <th>Data de Fim</th>
          </tr>
        </thead>
        <tbody>
          {history.map((h) => (
            <tr key={h.departmentHistoryId}>
              <td>{h.departmentHistoryId}</td>
              <td>{h.employeeId}</td>
              <td>{h.departmentId}</td>
              <td>{new Date(h.startDate).toLocaleDateString()}</td>
              <td>{h.endDate ? new Date(h.endDate).toLocaleDateString() : "Ativo"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default DepartmentHistoryList;