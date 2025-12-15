
// src/pages/FormPage.jsx
import React, { useState } from "react";

function FormPage({ onCancel }) {
  const [file, setFile] = useState(null);
  const [errors, setErrors] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const MAX_SIZE_MB = 5;

  const handleChange = (e) => {
    const f = e.target.files?.[0] || null;
    setFile(f);
    setErrors("");

    if (!f) return;

    // Validações simples
    const isPdf = f.type === "application/pdf" || f.name.toLowerCase().endsWith(".pdf");
    const isUnderMax = f.size <= MAX_SIZE_MB * 1024 * 1024;

    if (!isPdf) {
      setErrors("Por favor selecione um ficheiro PDF.");
      setFile(null);
    } else if (!isUnderMax) {
      setErrors(`O ficheiro é demasiado grande (máx. ${MAX_SIZE_MB}MB).`);
      setFile(null);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!file) {
      setErrors("O CV em PDF é obrigatório.");
      return;
    }

    setIsSubmitting(true);
    setErrors("");

    try {
      const formData = new FormData();
      formData.append("cv", file); // campo "cv" no corpo

      // Ajuste o endpoint para o seu backend
      const res = await fetch("api/v1/jobcandidates/upload", {
        method: "POST",
        body: formData,
      });

      if (!res.ok) {
        // Pode ler mensagem do servidor:
        const msg = await res.text();
        throw new Error(msg || "Falha no upload do CV.");
      }

      alert("CV enviado com sucesso!");
      setFile(null);
    } catch (err) {
      console.error(err);
      setErrors(err.message || "Ocorreu um erro ao enviar o CV.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main
      className="d-flex justify-content-center align-items-start"
      style={{ minHeight: "100vh", paddingTop: "80px", paddingLeft: "1rem", paddingRight: "1rem" }}
    >
      <div className="w-100" style={{ maxWidth: "600px" }}>
        <h2 className="mb-4 text-center">Submeter CV</h2>

        <form onSubmit={handleSubmit} noValidate>
          <div className="form-group mb-3">
            <label htmlFor="cv">
              Upload CV (PDF) <span className="text-danger">*</span>
            </label>
            <input
              type="file"
              className={`form-control ${errors ? "is-invalid" : ""}`}
              id="cv"
              name="cv"
              accept="application/pdf"
              onChange={handleChange}
            />
            {errors && <div className="invalid-feedback d-block">{errors}</div>}
            <small className="text-muted d-block mt-2">
              Apenas ficheiros PDF. Tamanho máximo: {MAX_SIZE_MB}MB.
            </small>
          </div>

          <div className="d-flex gap-2">
            {typeof onCancel === "function" && (
              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={onCancel}
                disabled={isSubmitting}
              >
                Cancelar
              </button>
            )}
            <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
              {isSubmitting ? "A enviar..." : "Enviar CV"}
            </button>
          </div>
        </form>
      </div>
       </main>
  );
}

