import React, { useState, useEffect } from "react";
import { useNavigate, useLocation, useParams } from "react-router-dom";
import { addNotification, addNotificationForUser } from "../../utils/notificationBus";
import BackButton from "../../components/Button/BackButton";

function getDepartamentoAtualNome(funcionario) {
  const historicos = funcionario?.departmentHistories ?? [];
  if (historicos.length === 0) return "Sem departamento";
  //se data atual entre data de fim e inicio
  const atual = historicos.find((h) => h.endDate == null);
  const escolhido =
    atual ??
    historicos
      .slice()
      .sort((a, b) => new Date(b.startDate) - new Date(a.startDate))[0];
  return escolhido?.department?.name ?? "Departamento desconhecido";
}

export default function Profile() {
  const location = useLocation();
  const params = new URLSearchParams(location.search);
  const userId = params.get("id") ;
  const navigate = useNavigate();
  const { id: routeId } = useParams()
  const role = localStorage.getItem("role");
  const id = localStorage.getItem("businessEntityId");
  const canEdit = role == "admin" &&  userId!=null && id != userId  ? true : false;

  const [departments, setDepartments] = useState([]);
  const [employee, setEmployee] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [fetchError, setFetchError] = useState(null);
  const [saveError, setSaveError] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);

  useEffect(() => {
    const fetchDepartments = async () => {
      try {
        const token = localStorage.getItem("authToken");
        const res = await fetch(
          "http://localhost:5136/api/v1/departmenthistory",
          {
            method: "GET",
            headers: {
              Accept: "application/json",
              Authorization: `Bearer ${token}`,
            },
          }
        );
        if (!res.ok) throw new Error("Erro ao carregar departamentos");
        const data = await res.json();
        setDepartments(data);
      } catch (err) {
        console.error(err);
      }
    };

    fetchDepartments();
  }, []);

  const getEmployeeId = () => {
    if (routeId) return routeId;

    const params = new URLSearchParams(location.search);
    const fromQuery = params.get("id");
    if (fromQuery) return fromQuery;

    const fromStorage = localStorage.getItem("businessEntityId");
    return fromStorage ?? null;
  };

  const targetId = getEmployeeId();

  useEffect(() => {
    const fetchEmployee = async () => {
      try {
        const token = localStorage.getItem("authToken");
        setLoading(true);
        setFetchError(null);

        const response = await fetch(`http://localhost:5136/api/v1/employee/${targetId}`, {
          method: "GET",
          headers: {
            Accept: "application/json",
            Authorization: `Bearer ${token}`,
          },
        });

        if (!response.ok)
          throw new Error(
            `Erro ao carregar funcionário (HTTP ${response.status})`
          );
        const data = await response.json();
        setEmployee(data);
      } catch (error) {
        if (error.name === "AbortError") return;
        console.error(error);
        setFetchError(
          error.message || "Erro desconhecido ao obter funcionário."
        );
      } finally {
        setLoading(false);
      }
    };

    //   fetchEmployee();
    // }, [navigate, location.search]);

    if (targetId != null) {
      // reset before fetching a new id
      setEmployee(null);
      fetchEmployee();
    }
  }, [targetId]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;

    if (name.startsWith('person.')) {
      const key = name.split('.')[1];
      setEmployee((prev) => ({
        ...prev,
        person: {
          ...prev.person,
          [key]: value
        }
      }));
      return;
    }

    setEmployee((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
  };

  const handleSave = async () => {
    const id = getEmployeeId();
    setSaveError(null);
    setSuccessMessage(null);

    try {
      const payload = {
        businessEntityID: employee.businessEntityID,
        nationalIDNumber: employee.nationalIDNumber,
        loginID: employee.loginID,
        jobTitle: employee.jobTitle,
        birthDate: employee.birthDate,
        maritalStatus: employee.maritalStatus,

        person: {
          firstName: employee.person?.firstName ?? "",
          middleName: employee.person?.middleName ?? "",
          lastName: employee.person?.lastName ?? "",
        },

        gender: employee.gender,
        hireDate: employee.hireDate,
        salariedFlag: employee.salariedFlag,
        vacationHours: parseInt(employee.vacationHours) || 0,
        sickLeaveHours: parseInt(employee.sickLeaveHours) || 0,
      };

      const token = localStorage.getItem("authToken");

      const response = await fetch(
        `http://localhost:5136/api/v1/employee/${id}`,
        {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json",
            Accept: "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify(payload),
        }
      );

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Erro ao atualizar: ${errorText}`);
      }

      addNotification(
        `O perfil do funcionário ${employee.person?.firstName} ${employee.person?.lastName} foi atualizado.`,
        "admin"
      );
      
      addNotificationForUser(
        `O seu perfil foi atualizado pelo RH.`,
        id
      );

      const refreshResponse = await fetch(
        `http://localhost:5136/api/v1/employee/${id}`, {
        method: "GET",
        headers: {
          Accept: "application/json",
          Authorization: `Bearer ${token}`,
        },
      }
      );

      if (refreshResponse.ok) {
        const updated = await refreshResponse.json();
        setEmployee(updated);
      }

      setEditing(false);
      setSuccessMessage("Perfil atualizado com sucesso!");
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (error) {
      console.error(error);
      setSaveError(error.message || "Falha ao salvar alterações");
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
      <BackButton />
      <div className="d-flex align-items-center mb-3">
        <h2 className="ms-2 h3">Perfil do Funcionário</h2>
      </div>

      {successMessage && (
        <div
          className="alert alert-success alert-dismissible fade show"
          role="alert"
        >
          {successMessage}
          <button
            type="button"
            className="btn-close"
            onClick={() => setSuccessMessage(null)}
          ></button>
        </div>
      )}

      <div className="card border-0 shadow-sm rounded-4">
        <div className="card-header bg-light text-dark text-center rounded-top-4">
          <h5 className="mb-0">{employee.jobTitle}</h5>
        </div>

        <div className="card-body p-4">
          <div className="d-flex align-items-center mb-4">
            <div
              className="rounded-circle bg-secondary bg-opacity-25 d-flex align-items-center justify-content-center me-3"
              style={{ width: 56, height: 56 }}
              aria-label="Avatar"
            >
              <span className="text-muted fw-bold">
                {employee.person?.firstName?.charAt(0) ?? "?"}
              </span>
            </div>
            <div>
              <div className="h6 mb-1 text-dark">
                {employee.person?.firstName} {employee.person?.lastName}
              </div>
              <div className="text-muted small">
                {getDepartamentoAtualNome(employee)}
              </div>
            </div>
          </div>

          {saveError && (
            <div className="alert alert-danger" role="alert">
              {saveError}
            </div>
          )}

          {editing ? (
            <>
              <div className="row g-3">
                <div className="col-md-6">
                  <label className="form-label text-muted">Primeiro Nome</label>
                  <input
                    type="text"
                    className="form-control"
                    name="person.firstName"
                    value={employee.person.firstName ?? ""}
                    onChange={handleChange}
                  />
                </div>

                <div className="col-md-6">
                  <label className="form-label text-muted">Nome do Meio</label>
                  <input
                    type="text"
                    className="form-control"
                    name="person.middleName"
                    value={employee.person.middleName ?? ""}
                    onChange={handleChange}
                  />
                </div>

                <div className="col-md-6">
                  <label className="form-label text-muted">Último Nome</label>
                  <input
                    type="text"
                    className="form-control"
                    name="person.lastName"
                    value={employee.person.lastName ?? ""}
                    onChange={handleChange}
                  />
                </div>

                <div className="col-md-6">
                  <label className="form-label text-muted">
                    Data de Nascimento
                  </label>
                  <input
                    type="date"
                    className="form-control"
                    name="birthDate"
                    value={employee.birthDate}
                    onChange={handleChange}
                  />
                </div>

                <div className="col-md-6">
                  <label className="form-label text-muted">Estado Civil</label>
                  <select
                    className="form-select"
                    name="maritalStatus"
                    value={employee.maritalStatus ?? ""}
                    onChange={handleChange}
                  >
                    <option value="">Selecione...</option>
                    <option value="S">Solteiro(a)</option>
                    <option value="M">Casado(a)</option>
                  </select>
                </div>

                <div className="col-md-6">
                  <label className="form-label text-muted">Género</label>
                  <select
                    className="form-select"
                    name="gender"
                    value={employee.gender ?? ""}
                    onChange={handleChange}
                  >
                    <option value="">Selecione...</option>
                    <option value="M">Masculino</option>
                    <option value="F">Feminino</option>
                  </select>
                </div>
                {canEdit && (
                  <>
                    <div className="col-md-6">
                      <label className="form-label text-muted">Cargo</label>
                      <input
                        type="text"
                        className="form-control"
                        name="jobTitle"
                        value={employee.jobTitle ?? ""}
                        onChange={handleChange}
                      />
                    </div>

                    <div className="col-md-6">
                      <label className="form-label text-muted">
                        Cartão de Cidadão
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        name="nationalIDNumber"
                        value={employee.nationalIDNumber ?? 0}
                        onChange={handleChange}
                      />
                    </div>

                    <div className="col-md-6">
                      <label className="form-label text-muted">
                        Horas de Férias
                      </label>
                      <input
                        type="number"
                        className="form-control"
                        name="vacationHours"
                        value={employee.vacationHours ?? 0}
                        onChange={handleChange}
                      />
                    </div>

                    <div className="col-md-6">
                      <label className="form-label text-muted">
                        Horas de Baixa
                      </label>
                      <input
                        type="number"
                        className="form-control"
                        name="sickLeaveHours"
                        value={employee.sickLeaveHours ?? 0}
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
                        <label
                          className="form-check-label text-muted"
                          htmlFor="flag-salario"
                        >
                          Com Salário
                        </label>
                      </div>
                    </div>

                    <div className="col-md-6">
                      <label className="form-label text-muted">
                        Departamento atual
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        name="departamento"
                        value={getDepartamentoAtualNome(employee)}
                        disabled
                      />
                    </div>
                  </>
                )}
              </div>

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
                  onClick={() => {
                    setEditing(false);
                    setSaveError(null);
                  }}
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
                      <strong>Nome:</strong> {employee.person?.firstName}{" "}
                      {employee.person?.lastName}
                    </p>
                    <p className="mb-0">
                      <strong>Cartão de Cidadão:</strong>{" "}
                      {employee.nationalIDNumber}
                    </p>
                  </div>
                </div>

                <div className="col-md-6">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">
                      Informações Pessoais
                    </p>
                    <p className="mb-1">
                      <strong>Data de Nascimento:</strong>{" "}
                      {new Date(employee.birthDate).toLocaleDateString("pt-PT")}
                    </p>
                    <p className="mb-1">
                      <strong>Estado Civil:</strong>{" "}
                      {employee.maritalStatus === "S"
                        ? "Solteiro(a)"
                        : employee.maritalStatus === "M"
                          ? "Casado(a)"
                          : employee.maritalStatus || "N/A"}
                    </p>
                    <p className="mb-0">
                      <strong>Género:</strong>{" "}
                      {employee.gender === "M"
                        ? "Masculino"
                        : employee.gender === "F"
                          ? "Feminino"
                          : employee.gender || "N/A"}
                    </p>
                  </div>
                </div>

                <div className="col-md-6">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">Cargo e Salário</p>
                    <p className="mb-1">
                      <strong>Cargo:</strong> {employee.jobTitle}
                    </p>
                    <p className="mb-0">
                      <strong>Com Salário:</strong>{" "}
                      {employee.salariedFlag ? (
                        <span className="badge bg-success">Sim</span>
                      ) : (
                        <span className="badge bg-secondary">Não</span>
                      )}
                    </p>
                  </div>
                </div>

                <div className="col-md-6">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">Registos</p>
                    <p className="mb-1">
                      <strong>Data de Contratação:</strong>{" "}
                      {new Date(employee.hireDate).toLocaleDateString("pt-PT")}
                    </p>
                    <p className="mb-1">
                      <strong>Horas de Férias:</strong> {employee.vacationHours}
                    </p>
                    <p className="mb-1">
                      <strong>Horas de Baixa:</strong> {employee.sickLeaveHours}
                    </p>
                    <p className="mb-0">
                      <strong>Última Modificação:</strong>{" "}
                      {new Date(employee.modifiedDate).toLocaleDateString(
                        "pt-PT"
                      )}
                    </p>
                  </div>
                </div>

                <div className="col-12">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">Departamento Atual</p>
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
