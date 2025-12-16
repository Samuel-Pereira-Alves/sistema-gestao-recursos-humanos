
import React, { useState } from "react";
import Navbar from "./Navbar";
import { addNotification } from "./store/notificationBus";

function FormPage({ hideNavbar = false, variant = "default", onCancel }) {
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
    const isPdf = f.type === "application/pdf" || f.name?.toLowerCase().endsWith(".pdf");
    if (!isPdf) newErrors.ficheiro = "Apenas ficheiros PDF são aceites.";
    if (f.size > MAX_SIZE) newErrors.ficheiro = `O ficheiro excede ${MAX_SIZE_MB}MB.`;
    return newErrors;
  };

  const handleChange = (e) => {
    const f = e.target.files?.[0] || null;
    setSuccessMsg("");
    const newErrors = validateFile(f);
    setErrors(newErrors);
    setFicheiro(Object.keys(newErrors).length ? null : f);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSuccessMsg("");
    const newErrors = validateFile(ficheiro);
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      setSending(true);
      const formData = new FormData();
      formData.append("cv", ficheiro);
      const resp = await fetch("http://localhost:5136/api/v1/jobcandidate/upload", {
        method: "POST",
        body: formData,
      });
      if (!resp.ok) throw new Error("Falha ao enviar candidatura");
      await new Promise((res) => setTimeout(res, 900));
      addNotification("[admin] Nova candidatura! - Verifica o painel de administração.");

      setSuccessMsg("Candidatura enviada com sucesso! Obrigado.");
      setFicheiro(null);
      setErrors({});

      if (variant === "embedded" && typeof onCancel === "function") {
        setTimeout(() => onCancel(), 900);
      }
    } catch (err) {
      console.error(err);
      setErrors({ ficheiro: "Ocorreu um erro ao enviar. Tenta novamente." });
    } finally {
      setSending(false);
    }
  };

  const UploadUI = (
    <form onSubmit={handleSubmit} noValidate className="simple-form">
      <header className="mb-3 text-center">
        <h6 className="mb-1">Enviar CV (PDF)</h6>
        <p className="text-muted small mb-0">
          Seleciona o ficheiro em formato PDF. Limite: {MAX_SIZE_MB}MB.
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

      <div className="mb-3">
        <label htmlFor="cv-input" className="form-label fw-semibold">CV (PDF) <span className="text-danger">*</span></label>
        <input
          id="cv-input"
          type="file"
          accept=".pdf,application/pdf"
          className={`form-control form-control-lg input-gray ${errors.ficheiro ? "is-invalid" : ""}`}
          onChange={handleChange}
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

      <div className="d-flex gap-2 mt-4">
        <button
          type="submit"
          className="btn btn-dark flex-grow-1"
          disabled={!ficheiro || sending}
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
  );

  if (variant === "embedded") {
    return (
      <div className="candidatura-embedded w-100" style={{ maxWidth: "640px" }}>
        {UploadUI}
      </div>
    );
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
                Carrega o teu CV em formato PDF para análise.
              </p>
              {UploadUI}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

export default FormPage;