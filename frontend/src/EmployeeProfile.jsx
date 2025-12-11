import React, { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap-icons/font/bootstrap-icons.css";

function EmployeeProfile() {
  const [employee, setEmployee] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const navigate = useNavigate();
  const { id } = useParams(); // rota /profile/:id

  useEffect(() => {
    const fetchEmployee = async () => {
      try {
        const response = await fetch(`http://localhost:5136/api/v1/employee/${id}`);
        if (!response.ok) throw new Error("Erro ao carregar funcionário");
        const data = await response.json();
        console.log("API data:", data);
        setEmployee(data);
      } catch (error) {
        console.error(error);
      } finally {
        setLoading(false);
      }
    };

    fetchEmployee();
  }, [id]);

  if (loading) return <p className="text-center mt-5">Carregando perfil...</p>;
  if (!employee) return <p className="text-center mt-5">Funcionário não encontrado</p>;

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setEmployee((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleSave = async () => {
    try {
      const response = await fetch(`http://localhost:5136/api/v1/employee/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(employee),
      });
      if (!response.ok) throw new Error("Erro ao atualizar funcionário");
      const updated = await response.json();
      setEmployee(updated);
      setEditing(false);
      console.log("Dados atualizados:", updated);
    } catch (error) {
      console.error(error);
      alert("Falha ao salvar alterações");
    }
  };

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
        <h2 className="ms-2 mb-0 text-primary fw-bold">Perfil do Funcionário</h2>
      </div>

      {/* Card */}
      <div className="card shadow-lg border-0 rounded-4">
        <div className="card-header bg-primary text-white text-center rounded-top-4">
          <h4 className="mb-0">{employee.JobTitle}</h4>
        </div>
        <div className="card-body p-4">
          {editing ? (
            <>
              <div className="row g-3">
                <div className="col-md-6">
                  <label className="form-label">Login</label>
                  <input
                    type="text"
                    className="form-control"
                    name="LoginID"
                    value={employee.LoginID}
                    onChange={handleChange}
                  />
                </div>
                <div className="col-md-6">
                  <label className="form-label">Estado Civil</label>
                  <input
                    type="text"
                    className="form-control"
                    name="MaritalStatus"
                    value={employee.MaritalStatus}
                    onChange={handleChange}
                  />
                </div>
                <div className="col-md-6">
                  <label className="form-label">Género</label>
                  <input
                    type="text"
                    className="form-control"
                    name="Gender"
                    value={employee.Gender}
                    onChange={handleChange}
                  />
                </div>
                <div className="col-md-6 d-flex align-items-center">
                  <div className="form-check mt-4">
                    <input
                      type="checkbox"
                      className="form-check-input"
                      name="SalariedFlag"
                      checked={employee.SalariedFlag}
                      onChange={handleChange}
                    />
                    <label className="form-check-label">Com Salário</label>
                  </div>
                </div>
              </div>
              <div className="mt-4 text-center">
                <button className="btn btn-success me-2 px-4" onClick={handleSave}>
                  💾 Salvar
                </button>
                <button className="btn btn-secondary px-4" onClick={() => setEditing(false)}>
                  ❌ Cancelar
                </button>
              </div>
            </>
          ) : (
            <div className="row g-3">
              <div className="col-md-6">
                <p><strong>ID:</strong> <span className="badge bg-secondary">{employee.BusinessEntityID}</span></p>
                <p><strong>Login:</strong> {employee.LoginID}</p>
                <p><strong>Nome Nacional:</strong> {employee.NationalIDNumber}</p>
              </div>
              <div className="col-md-6">
                <p><strong>Data de Nascimento:</strong> {new Date(employee.BirthDate).toLocaleDateString()}</p>
                <p><strong>Estado Civil:</strong> {employee.MaritalStatus || "N/A"}</p>
                <p><strong>Género:</strong> {employee.Gender || "N/A"}</p>
              </div>
              <div className="col-md-6">
                <p><strong>Data de Contratação:</strong> {new Date(employee.HireDate).toLocaleDateString()}</p>
                <p><strong>Com Salário:</strong> {employee.SalariedFlag ? "✅ Sim" : "❌ Não"}</p>
              </div>
              <div className="col-md-6">
                <p><strong>Horas de Férias:</strong> {employee.VacationHours}</p>
                <p><strong>Horas de Baixa:</strong> {employee.SickLeaveHours}</p>
                <p><strong>Última Modificação:</strong> {new Date(employee.ModifiedDate).toLocaleDateString()}</p>
              </div>
              <div className="text-center mt-4">
                <button className="btn btn-primary px-4" onClick={() => setEditing(true)}>
                  ✏️ Editar Perfil
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default EmployeeProfile;