import React, { useState } from "react";
import Navbar from "../../components/Navbar/Navbar";
import { addNotification } from "../../utils/notificationBus";

function Form({ hideNavbar = false, variant = "default", onCancel }) {
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [nationalIDNumber, setNationalIDNumber] = useState("");
  const [birthDate, setBirthDate] = useState("");
  const [maritalStatus, setMaritalStatus] = useState("");
  const [gender, setGender] = useState("");

  const [ficheiro, setFicheiro] = useState(null);

  const [errors, setErrors] = useState({});
  const [sending, setSending] = useState(false);
  const [successMsg, setSuccessMsg] = useState("");

  const MAX_SIZE_MB = 5;
  const MAX_SIZE = MAX_SIZE_MB * 1024 * 1024;

  const validateFile = (f) => {
    const newErrors = {};
    if (!f) {
      newErrors.ficheiro = "O CV em PDF é obrigatório.";
      return newErrors;
    }
    const isPdf =
      f.type === "application/pdf" || f.name?.toLowerCase().endsWith(".pdf");
    if (!isPdf) newErrors.ficheiro = "Apenas ficheiros PDF são aceites.";
    if (f.size > MAX_SIZE) newErrors.ficheiro = `O ficheiro excede ${MAX_SIZE_MB}MB.`;
    return newErrors;
  };

  const validateForm = () => {
    const newErrors = {};
    if (!firstName.trim()) newErrors.firstName = "Primeiro nome é obrigatório.";
    if (!lastName.trim()) newErrors.lastName = "Apelido é obrigatório.";

    const nid = nationalIDNumber.trim();
    if (!nid) {
      newErrors.nationalIDNumber = "Número de identificação nacional é obrigatório.";
    } else if (!/^\d{9}$/.test(nid)) {
      newErrors.nationalIDNumber = "Deve conter exatamente 9 dígitos.";
    }

    if (!birthDate) newErrors.birthDate = "Data de nascimento é obrigatória.";

    const bd = new Date(birthDate);
    if (birthDate && bd > new Date()) {
      newErrors.birthDate = "Data futura não é válida.";
    }

    if (!["S", "M"].includes(maritalStatus)) {
      newErrors.maritalStatus = "Seleciona estado civil: Solteiro (S) ou Casado (M).";
    }

    if (!["M", "F"].includes(gender)) {
      newErrors.gender = "Seleciona género: Masculino (M) ou Feminino (F).";
    }

    const fileErrors = validateFile(ficheiro);
    Object.assign(newErrors, fileErrors);

    return newErrors;
  };

  const handleFileChange = (e) => {
    const f = e.target.files?.[0] || null;
    setSuccessMsg("");
    const newErrors = validateFile(f);
    setErrors((prev) => ({ ...prev, ...newErrors }));
    setFicheiro(Object.keys(newErrors).length ? null : f);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSuccessMsg("");
    const newErrors = validateForm();
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      setSending(true);
      const formData = new FormData();
      formData.append("cv", ficheiro);
      formData.append("FirstName", firstName.trim());
      formData.append("LastName", lastName.trim());
      formData.append("NationalIDNumber", nationalIDNumber.trim());
      formData.append("BirthDate", birthDate);
      formData.append("MaritalStatus", maritalStatus);
      formData.append("Gender", gender);

      const resp = await fetch("http://localhost:5136/api/v1/jobcandidate/upload", {
        method: "POST",
        body: formData,
      });

      if (!resp.ok) {
        const text = await resp.text().catch(() => "");
        throw new Error(text || "Falha ao enviar candidatura");
      }

      addNotification(
        `Nova candidatura: ${firstName} ${lastName} – verifica o painel.`,
        "admin"
      );

      setSuccessMsg("Candidatura enviada com sucesso! Obrigado.");
      setFicheiro(null);
      setErrors({});
      setFirstName("");
      setLastName("");
      setNationalIDNumber("");
      setBirthDate("");
      setMaritalStatus("");
      setGender("");

      if (variant === "embedded" && typeof onCancel === "function") {
        setTimeout(() => onCancel(), 900);
      }
    } catch (err) {
      console.error(err);
      setErrors({ ficheiro: err.message || "Ocorreu um erro ao enviar. Tenta novamente." });
    } finally {
      setSending(false);
    }
  };

  if (variant === "embedded") {
    return <div className="candidatura-embedded w-100" style={{ maxWidth: "640px" }}>
      <form onSubmit={handleSubmit} noValidate className="simple-form">
        <header className="mb-3 text-center">
          <h6 className="mb-1">Dados do candidato e CV (PDF)</h6>
          <p className="text-muted small mb-0">
            Preenche os teus dados e seleciona o ficheiro em formato PDF. Limite: {MAX_SIZE_MB}MB.
          </p>
        </header>

        {(errors.ficheiro || successMsg) && (
          <div
            className={`alert ${errors.ficheiro ? "alert-danger" : "alert-success"} py-2 px-3 mb-3`}
            role="alert"
          >
            {errors.ficheiro || successMsg}
          </div>
        )}

        {/* Campos de texto */}
        <div className="row g-3">
          <div className="col-md-6">
            <label htmlFor="firstName" className="form-label fw-semibold">
              Primeiro nome <span className="text-danger">*</span>
            </label>
            <input
              id="firstName"
              type="text"
              className={`form-control input-gray ${errors.firstName ? "is-invalid" : ""}`}
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              autoComplete="given-name"
            />
            {errors.firstName && <div className="invalid-feedback">{errors.firstName}</div>}
          </div>

          <div className="col-md-6">
            <label htmlFor="lastName" className="form-label fw-semibold">
              Apelido <span className="text-danger">*</span>
            </label>
            <input
              id="lastName"
              type="text"
              className={`form-control input-gray ${errors.lastName ? "is-invalid" : ""}`}
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              autoComplete="family-name"
            />
            {errors.lastName && <div className="invalid-feedback">{errors.lastName}</div>}
          </div>

          <div className="col-md-6">
            <label htmlFor="nationalIDNumber" className="form-label fw-semibold">
              Nº Identificação Nacional <span className="text-danger">*</span>
            </label>
            <input
              id="nationalIDNumber"
              type="text"
              inputMode="numeric"
              pattern="\d{9}"
              maxLength={9}
              className={`form-control input-gray ${errors.nationalIDNumber ? "is-invalid" : ""}`}
              value={nationalIDNumber}
              onChange={(e) => {
                const onlyDigits = e.target.value.replace(/\D/g, "").slice(0, 9);
                setNationalIDNumber(onlyDigits);
              }}
              placeholder="123456789"
              autoComplete="off"
            />
            {errors.nationalIDNumber && (
              <div className="invalid-feedback">{errors.nationalIDNumber}</div>
            )}
          </div>

          <div className="col-md-6">
            <label htmlFor="birthDate" className="form-label fw-semibold">
              Data de nascimento <span className="text-danger">*</span>
            </label>
            <input
              id="birthDate"
              type="date"
              className={`form-control input-gray ${errors.birthDate ? "is-invalid" : ""}`}
              value={birthDate}
              onChange={(e) => setBirthDate(e.target.value)}
              placeholder="YYYY-MM-DD"
            />
            {errors.birthDate && <div className="invalid-feedback">{errors.birthDate}</div>}
          </div>

          <div className="col-md-6">
            <label htmlFor="maritalStatus" className="form-label fw-semibold">
              Estado civil <span className="text-danger">*</span>
            </label>
            <select
              id="maritalStatus"
              className={`form-select input-gray ${errors.maritalStatus ? "is-invalid" : ""}`}
              value={maritalStatus}
              onChange={(e) => setMaritalStatus(e.target.value)}
            >
              <option value="">Seleciona…</option>
              <option value="S">Solteiro(a) (S)</option>
              <option value="M">Casado(a) (M)</option>
            </select>
            {errors.maritalStatus && <div className="invalid-feedback">{errors.maritalStatus}</div>}
          </div>

          <div className="col-md-6">
            <label htmlFor="gender" className="form-label fw-semibold">
              Género <span className="text-danger">*</span>
            </label>
            <select
              id="gender"
              className={`form-select input-gray ${errors.gender ? "is-invalid" : ""}`}
              value={gender}
              onChange={(e) => setGender(e.target.value)}
            >
              <option value="">Seleciona…</option>
              <option value="M">Masculino (M)</option>
              <option value="F">Feminino (F)</option>
            </select>
            {errors.gender && <div className="invalid-feedback">{errors.gender}</div>}
          </div>
        </div>

        {/* Upload do CV */}
        <div className="mt-3">
          <label htmlFor="cv-input" className="form-label fw-semibold">
            CV (PDF) <span className="text-danger">*</span>
          </label>
          <input
            id="cv-input"
            type="file"
            accept=".pdf,application/pdf"
            className={`form-control form-control-lg input-gray ${errors.ficheiro ? "is-invalid" : ""}`}
            onChange={handleFileChange}
          />
          {errors.ficheiro && <div className="invalid-feedback">{errors.ficheiro}</div>}
        </div>

        {ficheiro && (
          <div className="selected-file my-2">
            <div className="file-pill" title={ficheiro.name}>
              <span className="file-name">{ficheiro.name}</span>
              <button
                type="button"
                className="btn btn-sm btn-clear"
                onClick={() => setFicheiro(null)}
                aria-label="Remover ficheiro selecionado"
              >
                ✕
              </button>
            </div>
          </div>
        )}

        {/* Botões */}
        <div className="d-flex gap-2 mt-4">
          <button
            type="submit"
            className="btn btn-dark flex-grow-1"
            disabled={sending}
          >
            {sending ? "A enviar..." : "Enviar candidatura"}
          </button>
          {typeof onCancel === "function" && (
            <button
              type="button"
              className="btn btn-outline-secondary"
              onClick={onCancel}
              disabled={sending}
            >
              Cancelar
            </button>
          )}
        </div>
      </form>
    </div>;
  }

  return (
    <div>
      {!hideNavbar && <Navbar />}
      <main
        className="d-flex justify-content-center"
        style={{ minHeight: "100vh", paddingTop: "80px", paddingLeft: "1rem", paddingRight: "1rem" }}
      >
        <div className="candidatura-full w-100" style={{ maxWidth: "720px" }}>
          <div className="card candidatura-card border-0 shadow-sm">
            <div className="card-body p-4 p-md-5">
              <h2 className="mb-3 text-center">Submissão de Candidatura</h2>
              <p className="text-muted text-center mb-4">
                Preenche os teus dados e carrega o teu CV em formato PDF.
              </p>
              {UploadUI}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

export default Form;