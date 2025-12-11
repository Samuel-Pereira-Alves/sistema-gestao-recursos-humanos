import React, { useState, useEffect } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

function PaymentsList() {
  const [payments, setPayments] = useState([]);
  const [loading, setLoading] = useState(true);

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
    <div className="container mt-5">
      <h2 className="mb-4 text-primary fw-bold">📑 Histórico de Pagamentos</h2>
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