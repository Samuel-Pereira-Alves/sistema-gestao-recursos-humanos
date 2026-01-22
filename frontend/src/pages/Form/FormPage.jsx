import React, { useRef, useState } from "react";
import { addNotification } from "../../utils/notificationBus";
import axios from "axios";

function Form({ onCancel }) {
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [nationalIDNumber, setNationalIDNumber] = useState("");
  const [birthDate, setBirthDate] = useState("");
  const [maritalStatus, setMaritalStatus] = useState("");
  const [gender, setGender] = useState("");
  const [ficheiro, setFicheiro] = useState(null);
  const [email, setEmail] = useState("");

  const [errors, setErrors] = useState({});
  const [sending, setSending] = useState(false);
  const [successMsg, setSuccessMsg] = useState("");

  const [dragActive, setDragActive] = useState(false);
  const inputRef = useRef(null);

  const [uploadProgress, setUploadProgress] = useState(null);
  const [uploadMsg, setUploadMsg] = useState("");

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
    if (f.size > MAX_SIZE)
      newErrors.ficheiro = `O ficheiro excede ${MAX_SIZE_MB}MB.`;
    return newErrors;
  };

  const validateForm = () => {
    const newErrors = {};
    if (!firstName.trim()) newErrors.firstName = "Primeiro nome é obrigatório.";
    if (!lastName.trim()) newErrors.lastName = "Apelido é obrigatório.";

    const nid = nationalIDNumber.trim();
    if (!nid) {
      newErrors.nationalIDNumber =
        "Número de identificação nacional é obrigatório.";
    } else if (!/^\d{9}$/.test(nid)) {
      newErrors.nationalIDNumber = "Deve conter exatamente 9 dígitos.";
    }

    if (!birthDate) newErrors.birthDate = "Data de nascimento é obrigatória.";

    const bd = new Date(birthDate);
    if (birthDate && bd > new Date()) {
      newErrors.birthDate = "Data De Nascimento Inválida.";
    }
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const eighteen = new Date(
      bd.getFullYear() + 18,
      bd.getMonth(),
      bd.getDate(),
    );

    if (eighteen > today) {
      newErrors.birthDate = "O utilizador deve ter pelo menos 18 anos.";
    }

    if (!["S", "M"].includes(maritalStatus)) {
      newErrors.maritalStatus =
        "Seleciona estado civil: Solteiro (S) ou Casado (M).";
    }

    if (!["M", "F"].includes(gender)) {
      newErrors.gender = "Seleciona género: Masculino (M) ou Feminino (F).";
    }

    const fileErrors = validateFile(ficheiro);
    Object.assign(newErrors, fileErrors);

    return newErrors;
  };

  const applyFile = (f) => {
    setSuccessMsg("");
    setUploadMsg("");
    const newErrors = validateFile(f);
    setErrors((prev) => ({ ...prev, ...newErrors }));
    if (Object.keys(newErrors).length) {
      setFicheiro(null);
    } else {
      setFicheiro(f);
      setUploadMsg("Ficheiro pronto para envio.");
    }
  };

  const handleFileChange = (e) => {
    const f = e.target.files?.[0] || null;
    applyFile(f);
  };

  const preventDefaults = (e) => {
    e.preventDefault();
    e.stopPropagation();
  };

  const handleDragOver = (e) => {
    preventDefaults(e);
    setDragActive(true);
    setUploadMsg("Solta o PDF para anexar…");
  };

  const handleDragLeave = (e) => {
    preventDefaults(e);
    setDragActive(false);
    setUploadMsg("");
  };

  const handleDrop = (e) => {
    preventDefaults(e);
    setDragActive(false);
    const files = e.dataTransfer?.files;
    if (files && files.length > 0) {
      applyFile(files[0]);
      e.dataTransfer.clearData();
    }
  };

  const handleZoneClick = () => {
    inputRef.current?.click();
  };

  const handleZoneKeyDown = (e) => {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      handleZoneClick();
    }
  };

  const resetUploadUI = () => {
    setUploadProgress(null);
    setUploadMsg("");
  };

  const doUpload = async (formData) => {
    await axios.post(
      "http://localhost:5136/api/v1/jobcandidate/upload",
      formData,
      {
        onUploadProgress: (evt) => {
          if (evt.total) {
            const percent = Math.round((evt.loaded / evt.total) * 100);
            setUploadProgress(percent);
            setUploadMsg(percent < 100 ? "A enviar…" : "A finalizar…");
          } else {
            setUploadMsg("A enviar…");
          }
        },
      },
    );
  };

  const clearForm = () => {
    setFicheiro(null);
    setErrors({});
    setEmail("");
    setFirstName("");
    setLastName("");
    setNationalIDNumber("");
    setBirthDate("");
    setMaritalStatus("");
    setGender("");
    if (inputRef.current) inputRef.current.value = "";
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSuccessMsg("");
    resetUploadUI();

    const newErrors = validateForm();
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      setSending(true);
      setUploadProgress(0);
      setUploadMsg("A iniciar envio…");

      const formData = new FormData();
      formData.append("cv", ficheiro);
      formData.append("FirstName", firstName.trim());
      formData.append("Email", email);
      formData.append("LastName", lastName.trim());
      formData.append("NationalIDNumber", nationalIDNumber.trim());
      formData.append("BirthDate", birthDate);
      formData.append("MaritalStatus", maritalStatus);
      formData.append("Gender", gender);

      await doUpload(formData);

      //enviar email
      try {
        await axios.post("http://localhost:5136/api/email/send", {
          to: email,
          subject: "Candidatura",
          text: "A sua candidatura foi recebida. Iremos analisá-la e responder brevemente!",
        });
      } catch (e) {
        if (e.response) {
          console.error("Erro API:", e.response.data);
        } else {
          console.error("Erro rede/CORS:", e.message);
        }
      }

      addNotification(
        `Nova candidatura: ${firstName} ${lastName} – verifica o painel de candidaturas.`,
        "admin",
        { type: "CANDIDATE" },
      );

      setUploadProgress(100);
      setUploadMsg("Concluído.");
      setSuccessMsg("Candidatura enviada com sucesso! Obrigado.");

      clearForm();

      if (typeof onCancel === "function") {
        setTimeout(() => onCancel(), 900);
      }
    } catch (err) {
      console.error("Erro ao enviar candidatura:", err);
      setErrors({
        nationalIDNumber: err.response.data?.detail,
      });
      setUploadMsg("Falha no envio.");
      setUploadProgress(null);
    } finally {
      setSending(false);
      setTimeout(() => {
        setUploadMsg("");
        setUploadProgress(null);
      }, 2500);
    }
  };

  return (
    <div className="candidatura-embedded w-100" style={{ maxWidth: "640px" }}>
      <form onSubmit={handleSubmit} noValidate className="simple-form">
        <header className="mb-3 text-center">
          <h6 className="mb-1">Dados do candidato e CV (PDF)</h6>
          <p className="text-muted small mb-0">
            Preenche os teus dados e seleciona o ficheiro em formato PDF.
            Limite: {MAX_SIZE_MB}MB.
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
          {/* Primeiro Nome */}
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
            {errors.firstName && (
              <div className="invalid-feedback">{errors.firstName}</div>
            )}
          </div>

          {/* Apelido */}
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
            {errors.lastName && (
              <div className="invalid-feedback">{errors.lastName}</div>
            )}
          </div>

          {/* Email */}
          <div className="col-md-6">
            <label htmlFor="email" className="form-label fw-semibold">
              Email <span className="text-danger">*</span>
            </label>
            <input
              id="email"
              type="email"
              className={`form-control input-gray ${errors.email ? "is-invalid" : ""}`}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
            />
          </div>

          {/* Nº Cartao Cidadao */}
          <div className="col-md-6">
            <label
              htmlFor="nationalIDNumber"
              className="form-label fw-semibold"
            >
              Nº Cartão Cidadão <span className="text-danger">*</span>
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
                const onlyDigits = e.target.value
                  .replace(/\D/g, "")
                  .slice(0, 9);
                setNationalIDNumber(onlyDigits);
              }}
              placeholder="123456789"
              autoComplete="off"
            />
            {errors.nationalIDNumber && (
              <div className="invalid-feedback">{errors.nationalIDNumber}</div>
            )}
          </div>

          {/* Data de nascimento */}
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
            {errors.birthDate && (
              <div className="invalid-feedback">{errors.birthDate}</div>
            )}
          </div>

          {/* Estado civil */}
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
            {errors.maritalStatus && (
              <div className="invalid-feedback">{errors.maritalStatus}</div>
            )}
          </div>

          {/* Género */}
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
            {errors.gender && (
              <div className="invalid-feedback">{errors.gender}</div>
            )}
          </div>
        </div>

        {/* Upload do CV */}
        <div className="mt-3">
          <label htmlFor="cv-input" className="form-label fw-semibold">
            CV (PDF) <span className="text-danger">*</span>
          </label>

          <div
            role="button"
            tabIndex={0}
            onClick={handleZoneClick}
            onKeyDown={handleZoneKeyDown}
            onDragOver={handleDragOver}
            onDragEnter={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            className={`border rounded-3 p-4 text-center ${dragActive ? "bg-light border-dark" : "border-secondary-subtle"}`}
            style={{ cursor: "pointer" }}
            aria-label="Arraste o ficheiro PDF aqui ou clique para selecionar"
            aria-describedby="cv-help"
          >
            {!ficheiro ? (
              <>
                <div className="fw-semibold">Arraste o ficheiro PDF aqui</div>
                <div className="text-muted small">
                  ou clique para selecionar
                </div>
                <div id="cv-help" className="text-muted small mt-2">
                  Apenas PDF. Tamanho máximo: {MAX_SIZE_MB}MB.
                </div>
                {uploadMsg && <div className="mt-2 small">{uploadMsg}</div>}
              </>
            ) : (
              <div className="selected-file my-1">
                <div
                  className="file-pill d-inline-flex align-items-center gap-2 px-3 py-1 rounded-pill bg-body-tertiary"
                  title={ficheiro.name}
                >
                  <span
                    className="file-name text-truncate"
                    style={{ maxWidth: 260 }}
                  >
                    {ficheiro.name}
                  </span>
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-danger"
                    onClick={(e) => {
                      e.stopPropagation();
                      setFicheiro(null);
                      if (inputRef.current) inputRef.current.value = "";
                      resetUploadUI();
                    }}
                    aria-label="Remover ficheiro selecionado"
                  >
                    ✕
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* Barra de progresso */}
          {uploadProgress !== null && (
            <div className="mt-2">
              <div
                className="progress"
                role="progressbar"
                aria-valuenow={uploadProgress}
                aria-valuemin="0"
                aria-valuemax="100"
              >
                <div
                  className="progress-bar"
                  style={{ width: `${uploadProgress}%` }}
                >
                  {uploadProgress}%
                </div>
              </div>
              {uploadMsg && (
                <div className="small text-muted mt-1">{uploadMsg}</div>
              )}
            </div>
          )}

          {/* Clique para inserir PDF */}
          <input
            ref={inputRef}
            id="cv-input"
            type="file"
            accept=".pdf,application/pdf"
            className="d-none"
            onChange={handleFileChange}
          />

          {errors.ficheiro && (
            <div className="invalid-feedback d-block mt-2">
              {errors.ficheiro}
            </div>
          )}
        </div>

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

        {/* Mensagem de sucesso */}
        {successMsg && (
          <div className="alert alert-success mt-3">{successMsg}</div>
        )}
      </form>
    </div>
  );
}

export default Form;
