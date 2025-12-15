
import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap-icons/font/bootstrap-icons.css";


function getDepartamentoAtualNome(funcionario) {
  const historicos = funcionario?.departmentHistories ?? [];
  if (historicos.length === 0) return "Sem departamento";
  const atual = historicos.find((h) => h.endDate == null);
  const escolhido =
    atual ??
    historicos
      .slice()
      .sort((a, b) => new Date(b.startDate) - new Date(a.startDate))[0];
  return escolhido?.department?.name ?? "Departamento desconhecido";
}

export default function EmployeeProfile() {
  
  const [employee, setEmployee] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [fetchError, setFetchError] = useState(null);

  const navigate = useNavigate();
  const location = useLocation();

  const getEmployeeId = () => {
    const params = new URLSearchParams(location.search);
    const idFromQuery = params.get("id");
    const idFromStorage = localStorage.getItem("businessEntityId");
    return idFromQuery || idFromStorage || null;
  };

  useEffect(() => {
    const id = getEmployeeId();

    if (!id) {
      setFetchError("ID do funcionário não encontrado.");
      navigate("/funcionarios", { replace: true });
      return;
    }

    const controller = new AbortController();

    const fetchEmployee = async () => {
      try {
        setLoading(true);
        setFetchError(null);
        const response = await fetch(`http://localhost:5136/api/v1/employee/${id}`, {
          signal: controller.signal,
        });
        if (!response.ok)
          throw new Error(`Erro ao carregar funcionário (HTTP ${response.status})`);
        const data = await response.json();
        setEmployee(data);
      } catch (error) {
        if (error.name === "AbortError") return;
        console.error(error);
        setFetchError(error.message || "Erro desconhecido ao obter funcionário.");
      } finally {
        setLoading(false);
      }
    };

    fetchEmployee();
    return () => controller.abort();
  }, [location.search, navigate]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setEmployee((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleSave = async () => {
    const id = getEmployeeId();
    if (!id) {
      alert("ID do funcionário não encontrado.");
      return;
    }

    try {
      const response = await fetch(
        `http://localhost:5136/api/v1/employee/${localStorage.getItem(
          "businessEntityId"
        )}`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(employee),
        }
      );
      if (!response.ok) throw new Error("Erro ao atualizar funcionário");
      const updated = await response.json();
      setEmployee(updated);
      setEditing(false);
    } catch (error) {
      console.error(error);
      alert("Falha ao salvar alterações");
    }
  };

  if (loading)
    return (
      <div className="container mt-5 text-center text-muted">
        <div className="spinner-border text-secondary mb-3" role="status" />
        <p className="mb-0">Carregando perfil...</p>
      </div>
    );

  if (fetchError)
    return (
      <div className="container mt-5 text-center">
        <div className="alert alert-light border text-muted d-inline-block">
          {fetchError}
        </div>
      </div>
    );

  if (!employee)
    return (
      <div className="container mt-5 text-center">
        <div className="alert alert-light border text-muted d-inline-block">
          Funcionário não encontrado
        </div>
      </div>
    );

  return (
    <div className="container mt-4">
      {/* Barra superior com seta */}
      <div className="d-flex align-items-center mb-3">
  
        <h2 className="ms-2 h3">Perfil do Funcionário</h2>
      </div>

      {/* Card principal em tons de cinza */}
      <div className="card border-0 shadow-sm rounded-4">
        <div className="card-header bg-light text-dark text-center rounded-top-4">
          <h5 className="mb-0">{employee.jobTitle}</h5>
        </div>

        <div className="card-body p-4">
          {/* Cabeçalho com avatar simples */}
          <div className="d-flex align-items-center mb-4">
            <div
              className="rounded-circle bg-secondary bg-opacity-25 d-flex align-items-center justify-content-center me-3"
              style={{ width: 56, height: 56 }}
              aria-label="Avatar"
            >
              <span className="text-muted fw-bold">
                {getNomeCompleto(employee)
                  .split(" ")
                  .map((p) => p[0])
                  .slice(0, 2)
                  .join("")
                  .toUpperCase()}
              </span>
            </div>
            <div>
              <div className="h6 mb-1 text-dark">{employee.person?.firstName} {employee.person?.lastName}</div>
              <div className="text-muted small">{getDepartamentoAtualNome(employee)}</div>
            </div>
          </div>

          {/* Conteúdo */}
          {editing ? (
            <>
              <div className="row g-3">
                <div className="col-md-6">
                  <label className="form-label text-muted">Login</label>
                  <input
                    type="text"
                    className="form-control"
                    name="loginID"
                    value={employee.loginID ?? ""}
                    onChange={handleChange}
                  />
                </div>

                <div className="col-md-6">
                  <label className="form-label text-muted">Estado Civil</label>
                  <input
                    type="text"
                    className="form-control"
                    name="maritalStatus"
                    value={employee.maritalStatus ?? ""}
                    onChange={handleChange}
                  />
                </div>

                <div className="col-md-6">
                  <label className="form-label text-muted">Género</label>
                  <input
                    type="text"
                    className="form-control"
                    name="gender"
                    value={employee.gender ?? ""}
                    onChange={handleChange}
                  />
                </div>

                <div className="col-md-6 d-flex align-items-center">
                  <div className="form-check mt-4">
                    <input
                      type="checkbox"
                      className="form-check-input"
                      name="salariedFlag"
                      checked={!!employee.salariedFlag}
                      onChange={handleChange}
                      id="flag-salario"
                    />
                    <label className="form-check-label text-muted" htmlFor="flag-salario">
                      Com Salário
                    </label>
                  </div>
                </div>

                {/* Departamento (read-only) */}
                <div className="col-md-12">
                  <label className="form-label text-muted">Departamento (atual)</label>
                  <input
                    type="text"
                    className="form-control bg-light"
                    value={getDepartamentoAtualNome(employee)}
                    readOnly
                  />
                </div>
              </div>

              {/* Ações */}
              <div className="mt-4 text-center">
                <button
                  className="btn btn-outline-dark me-2 px-4"
                  onClick={handleSave}
                  type="button"
                >
                  Guardar Alterações
                </button>
                <button
                  className="btn btn-outline-secondary px-4"
                  onClick={() => setEditing(false)}
                  type="button"
                >
                  Cancelar
                </button>
              </div>
            </>
          ) : (
            <>
              <div className="row g-3">
                <div className="col-md-6">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">Identificação</p>
                    <p className="mb-1">
                      <strong>ID:</strong>{" "}
                      <span className="badge bg-secondary bg-opacity-50 text-dark">
                        {employee.businessEntityID}
                      </span>
                    </p>
                    <p className="mb-1">
                      <strong>Nome:</strong> {getNomeCompleto(employee)}
                    </p>
                    <p className="mb-0">
                      <strong>Cartão de Cidadão:</strong> {employee.nationalIDNumber}
                    </p>
                  </div>
                </div>

                <div className="col-md-6">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">Informações Pessoais</p>
                    <p className="mb-1">
                      <strong>Data de Nascimento:</strong>{" "}
                      {new Date(employee.birthDate).toLocaleDateString("pt-PT")}
                    </p>
                    <p className="mb-1">
                      <strong>Estado Civil:</strong> {employee.maritalStatus || "N/A"}
                    </p>
                    <p className="mb-0">
                      <strong>Género:</strong> {employee.gender || "N/A"}
                    </p>
                  </div>
                </div>

                <div className="col-md-6">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">Registos</p>
                    <strong>Data de Contratação:</strong>{" "}
                      {new Date(employee.hireDate).toLocaleDateString("pt-PT")}
                    <p className="mb-1">
                      <strong>Horas de Férias:</strong> {employee.vacationHours}
                    </p>
                    <p className="mb-1">
                      <strong>Horas de Baixa:</strong> {employee.sickLeaveHours}
                    </p>
                    <p className="mb-0">
                      <strong>Última Modificação:</strong>{" "}
                      {new Date(employee.modifiedDate).toLocaleDateString("pt-PT")}
                    </p>
                  </div>
                </div>

                <div className="col-12">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">Departamento</p>
                    <p className="mb-0">{getDepartamentoAtualNome(employee)}</p>
                  </div>
                </div>
              </div>

              <div className="text-center mt-4">
                <button
                  className="btn btn-outline-dark px-4"
                  onClick={() => setEditing(true)}
                  type="button"
                >
                  Editar Perfil
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
