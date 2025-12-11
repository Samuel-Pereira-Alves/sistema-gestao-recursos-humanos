import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom"; // importa o hook
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap-icons/font/bootstrap-icons.css"; // para usar ícones

function PaymentsList() {
  const [payments, setPayments] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    // Simulação de dados locais (mock)
    const mockData = [
      {
        payHistoryId: 1,
        employeeId: 101,
        rateChangeDate: "2025-12-01T00:00:00",
        rate: 1200.0,
        payFrequency: 1,
      },
      {
        payHistoryId: 2,
        employeeId: 102,
        rateChangeDate: "2025-12-01T00:00:00",
        rate: 1350.0,
        payFrequency: 2,
      },
      {
        payHistoryId: 3,
        employeeId: 103,
        rateChangeDate: "2025-12-01T00:00:00",
        rate: 1100.0,
        payFrequency: 1,
      },
    ];

    setPayments(mockData);
    setLoading(false);
  }, []);

  if (loading) return <p>Carregando pagamentos...</p>;

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
        <h2 className="ms-2 mb-0 text-primary fw-bold">📑 Histórico de Pagamentos</h2>
      </div>

      <table className="table table-striped table-hover shadow-sm">
        <thead className="table-dark">
          <tr>
            <th>ID</th>
            <th>Colaborador</th>
            <th>Valor (€)</th>
            <th>Data</th>
            <th>Frequência</th>
          </tr>
        </thead>
        <tbody>
          {payments.map((p) => (
            <tr key={p.payHistoryId}>
              <td>{p.payHistoryId}</td>
              <td>{p.employeeId}</td>
              <td>{p.rate.toFixed(2)}</td>
              <td>{new Date(p.rateChangeDate).toLocaleDateString()}</td>
              <td>{p.payFrequency}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default PaymentsList;