// src/pages/FormPage.jsx
import React, { useState } from "react";
import Navbar from "./Navbar";

function FormPage() {
  const [formData, setFormData] = useState({
    nome: "",
    email: "",
    telefone: "",
    vaga: "",
    ficheiro: null,
    confirmo: false,
  });

  const [errors, setErrors] = useState({});

  const handleChange = (e) => {
    const { name, value, files, type, checked } = e.target;
    if (name === "ficheiro") {
      setFormData({ ...formData, ficheiro: files[0] });
    } else if (type === "checkbox") {
      setFormData({ ...formData, [name]: checked });
    } else {
      setFormData({ ...formData, [name]: value });
    }
    setErrors({ ...errors, [name]: "" });
  };

  const validateForm = () => {
    let newErrors = {};
    if (!formData.nome) newErrors.nome = "O nome é obrigatório.";
    if (!formData.email) newErrors.email = "O email é obrigatório.";
    else if (!/\S+@\S+\.\S+/.test(formData.email))
      newErrors.email = "Formato de email inválido.";
    if (!formData.telefone) newErrors.telefone = "O telefone é obrigatório.";
    if (!formData.vaga) newErrors.vaga = "A vaga é obrigatória.";
    if (!formData.ficheiro) newErrors.ficheiro = "O CV em PDF é obrigatório.";
    if (!formData.confirmo)
      newErrors.confirmo = "É necessário confirmar que os dados estão corretos.";
    return newErrors;
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    const newErrors = validateForm();
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }
    console.log("Dados submetidos:", formData);
    alert("Formulário enviado com sucesso!");
  };

  return (
    <div>
      <Navbar />
      <main
        className="d-flex justify-content-center "
        style={{ minHeight: "100vh", paddingTop: "80px", paddingLeft: "1rem", paddingRight: "1rem" }}
      >
        <div className="w-100" style={{ maxWidth: "600px" }}>
          <h2 className="mb-4 text-center">Submissão de Candidatura</h2>
          <form onSubmit={handleSubmit} noValidate>
            <div className="form-group mb-3">
              <label htmlFor="nome">
                Nome <span className="text-danger">*</span>
              </label>
              <input
                type="text"
                className={`form-control ${errors.nome ? "is-invalid" : ""}`}
                id="nome"
                name="nome"
                value={formData.nome}
                onChange={handleChange}
                placeholder="Digite o seu nome"
              />
              {errors.nome && <div className="invalid-feedback">{errors.nome}</div>}
            </div>

            <div className="form-group mb-3">
              <label htmlFor="email">
                Email <span className="text-danger">*</span>
              </label>
              <input
                type="email"
                className={`form-control ${errors.email ? "is-invalid" : ""}`}
                id="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                placeholder="Digite o seu email"
              />
              {errors.email && <div className="invalid-feedback">{errors.email}</div>}
            </div>

            <div className="form-group mb-3">
              <label htmlFor="telefone">
                Telefone <span className="text-danger">*</span>
              </label>
              <input
                type="tel"
                className={`form-control ${errors.telefone ? "is-invalid" : ""}`}
                id="telefone"
                name="telefone"
                value={formData.telefone}
                onChange={handleChange}
                placeholder="Digite o seu telefone"
              />
              {errors.telefone && (
                <div className="invalid-feedback">{errors.telefone}</div>
              )}
            </div>

            <div className="form-group mb-3">
              <label htmlFor="vaga">
                Vaga Pretendida <span className="text-danger">*</span>
              </label>
              <input
                type="text"
                className={`form-control ${errors.vaga ? "is-invalid" : ""}`}
                id="vaga"
                name="vaga"
                value={formData.vaga}
                onChange={handleChange}
                placeholder="Digite a vaga"
              />
              {errors.vaga && <div className="invalid-feedback">{errors.vaga}</div>}
            </div>

            <div className="form-group mb-3">
              <label htmlFor="ficheiro">
                Upload CV (PDF) <span className="text-danger">*</span>
              </label>
              <input
                type="file"
                className={`form-control ${errors.ficheiro ? "is-invalid" : ""}`}
                id="ficheiro"
                name="ficheiro"
                accept="application/pdf"
                onChange={handleChange}
              />
              {errors.ficheiro && (
                <div className="invalid-feedback">{errors.ficheiro}</div>
              )}
            </div>

            <div className="form-check mb-3">
              <input
                type="checkbox"
                className={`form-check-input ${errors.confirmo ? "is-invalid" : ""}`}
                id="confirmo"
                name="confirmo"
                checked={formData.confirmo}
                onChange={handleChange}
              />
              <label className="form-check-label" htmlFor="confirmo">
                Confirmo que os dados estão corretos <span className="text-danger">*</span>
              </label>
              {errors.confirmo && (
                <div className="invalid-feedback d-block">{errors.confirmo}</div>
              )}
            </div>

            <button type="submit" className="btn btn-primary w-100">
              Enviar Candidatura
            </button>
          </form>
        </div>
      </main>
    </div>
  );
}

export default FormPage;