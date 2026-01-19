import React, { useState, useEffect } from "react";
import Select from "react-select";
import { useNavigate, useLocation, useParams } from "react-router-dom";
import { addNotification, addNotificationForUser, } from "../../utils/notificationBus";
import BackButton from "../../components/Button/BackButton";
import Avatar from "../../components/Avatar/Avatar";
import Loading from "../../components/Loading/Loading";
import ReadOnlyField from "../../components/ReadOnlyField/ReadOnlyField";
import { getDepartamentoAtualNome, formatDate } from "../../utils/Utils";
import {
  getDepartments,
  getEmployee,
  updateEmployee,
} from "../../Service/employeeService";
import { getEmployeeId } from "../../utils/Utils";

export default function Profile() {
  const location = useLocation();
  const params = new URLSearchParams(location.search);
  const userId = params.get("id");
  const navigate = useNavigate();
  const actualId = localStorage.getItem("businessEntityId");
  const { id: routeId } = useParams();

  const role = localStorage.getItem("role");

  const canEdit = role == "admin" && routeId != actualId ? true : false;

  const [departments, setDepartments] = useState([]);
  const [employee, setEmployee] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [fetchError, setFetchError] = useState(null);
  const [saveError, setSaveError] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);

  const targetId = getEmployeeId(routeId, location.search);

  // Carrega departamentos 
  useEffect(() => {
    (async () => {
      try {
        const token = localStorage.getItem("authToken");
        const data = await getDepartments(token);
        setDepartments(data);
      } catch (err) {
        console.error(err);
      }
    })();
  }, []);

  // Carregar funcionário
  useEffect(() => {
    const load = async () => {
      if (targetId == null) return;
      try {
        const token = localStorage.getItem("authToken");
        setLoading(true);
        setFetchError(null);
        setEmployee(null);
        const data = await getEmployee(targetId, token);
        setEmployee(data);
      } catch (error) {
        console.error(error);
        setFetchError(
          error.message || "Erro desconhecido ao obter funcionário."
        );
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [targetId, location.search, navigate]);

    const departmentOptions = (departments ?? []).map((d) => ({
    value: d.departmentID ?? d.id,
    label: d.name ?? d.departmentName,
  }));

  const currentDeptName = getDepartamentoAtualNome(employee);
  const selectedDepartmentInit =
    departmentOptions.find((o) => o.label === currentDeptName) ||
    null;

  const [selectedDept, setSelectedDept] = useState(selectedDepartmentInit);
  useEffect(() => {
    const freshSelected =
      departmentOptions.find((o) => o.label === currentDeptName) ||
      null;
    setSelectedDept(freshSelected);
  }, [employee, departments]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    if (name.startsWith("person.")) {
      const key = name.split(".")[1];
      setEmployee((prev) => ({
        ...prev,
        person: {
          ...prev.person,
          [key]: value,
        },
      }));
      return;
    }
    setEmployee((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleSave = async () => {
    const idToUpdate = getEmployeeId(routeId);
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

        departmentID: selectedDept?.value ?? employee.departmentID ?? null,
      };

      const token = localStorage.getItem("authToken");
      await updateEmployee(idToUpdate, payload, token);

      addNotificationForUser(
        `O seu perfil foi atualizado pelo RH.`,
        idToUpdate,
        { type: "PROFILE" }
      );

      const updated = await getEmployee(idToUpdate, token);
      setEmployee(updated);
      setEditing(false);
      setSuccessMessage("Perfil atualizado com sucesso!");
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (error) {
      console.error(error);
      setSaveError(error.message || "Falha ao salvar alterações");
    }
  };

  if (loading) return <Loading text="Carregando perfil..." />;

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
          {/* Header com Avatar + Nome + Departamento */}
          <div className="d-flex align-items-center mb-4">
            <div className="me-3">
              <Avatar name={employee.person?.firstName} />
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
                      <label className="form-label text-muted">Departamento</label>
                      <Select
                        options={departmentOptions}
                        value={selectedDept}
                        onChange={(value) => {
                          setSelectedDept(value);
                          setEmployee((prev) => ({ ...prev, departmentID: value?.value ?? null }));
                        }}
                        placeholder="Selecionar departamento..."
                        isClearable
                        classNamePrefix="react-select"
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
                    <ReadOnlyField
                      label="ID"
                      value={
                        <span className="badge bg-secondary bg-opacity-50 text-dark">
                          {employee.businessEntityID}
                        </span>
                      }
                    />
                    <ReadOnlyField
                      label="Nome"
                      value={`${employee.person?.firstName ?? ""} ${employee.person?.lastName ?? ""
                        }`}
                    />
                    <ReadOnlyField
                      label="Cartão de Cidadão"
                      value={employee.nationalIDNumber}
                    />
                  </div>
                </div>

                <div className="col-md-6">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">
                      Informações Pessoais
                    </p>
                    <ReadOnlyField
                      label="Data de Nascimento"
                      value={formatDate(employee.birthDate)}
                    />
                    <ReadOnlyField
                      label="Estado Civil"
                      value={
                        employee.maritalStatus === "S"
                          ? "Solteiro(a)"
                          : employee.maritalStatus === "M"
                            ? "Casado(a)"
                            : employee.maritalStatus || "N/A"
                      }
                    />
                    <ReadOnlyField
                      label="Género"
                      value={
                        employee.gender === "M"
                          ? "Masculino"
                          : employee.gender === "F"
                            ? "Feminino"
                            : employee.gender || "N/A"
                      }
                    />
                  </div>
                </div>

                <div className="col-md-6">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">Cargo e Salário</p>
                    <ReadOnlyField label="Cargo" value={employee.jobTitle} />
                    <ReadOnlyField
                      label="Com Salário"
                      value={
                        employee.salariedFlag ? (
                          <span className="badge bg-success">Sim</span>
                        ) : (
                          <span className="badge bg-secondary">Não</span>
                        )
                      }
                    />
                  </div>
                </div>

                <div className="col-md-6">
                  <div className="p-3 border rounded-3 bg-light">
                    <p className="mb-2 text-muted small">Registos</p>
                    <ReadOnlyField
                      label="Data de Contratação"
                      value={formatDate(employee.hireDate)}
                    />
                    <ReadOnlyField
                      label="Horas de Férias"
                      value={employee.vacationHours}
                    />
                    <ReadOnlyField
                      label="Horas de Baixa"
                      value={employee.sickLeaveHours}
                    />
                    <ReadOnlyField
                      label="Última Modificação"
                      value={formatDate(employee.modifiedDate)}
                    />
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
