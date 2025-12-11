import React, { useState, useEffect } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

function DepartmentHistoryList() {
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);

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
    <div className="container mt-5">
      <h2 className="mb-4 text-primary fw-bold">🏢 Histórico de Departamentos</h2>
      <table className="table table-striped table-hover shadow-sm">
        <thead className="table-dark">
          <tr>
            <th>ID</th>
            <th>Colaborador </th>
            <th>Departamento </th>
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