// src/pages/Login.jsx
import React, { useState, useContext } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import { AuthContext } from "./AuthContext";
import { useNavigate } from "react-router-dom";

function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [remember, setRemember] = useState(false);

  const { login } = useContext(AuthContext);
  const navigate = useNavigate();

  const handleSubmit = (e) => {
    e.preventDefault();

    // --- TESTE LOCAL ---
    console.log("Username:", username);
    console.log("Password:", password);
    console.log("Remember me:", remember);

    // Simulação: se username começar por "adm", assume admin, senão employee
    const role = username.startsWith("adm") ? "admin" : "employee";

    // Atualiza o contexto
    login(username, role);

    // Redireciona para Home
    navigate("/");
  };

  return (
    <div className="d-flex align-items-center justify-content-center vh-100 bg-gradient">
      <div className="card shadow-lg border-0 rounded-4" style={{ width: "900px" }}>
        <div className="row g-0">
          {/* Formulário */}
          <div className="col-md-6 bg-white rounded-start-4 d-flex flex-column justify-content-center align-items-center p-5">
            <div className="welcome-message text-center mb-4 w-100">
              <h3 className="fw-bold text-primary fade-in">
                Sistema de Gestão de Recursos Humanos
              </h3>
              <p className="text-muted fade-in delay-1">
                Aceda à sua conta para gerir colaboradores, processos e desempenho 📊
              </p>
            </div>

            <form onSubmit={handleSubmit} className="w-100">
              <div className="mb-3">
                <label className="form-label fw-semibold">Username</label>
                <div className="input-group input-group-lg">
                  <span className="input-group-text">
                    <i className="bi bi-person-fill"></i>
                  </span>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="Digite seu utilizador"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    required
                  />
                </div>
              </div>
              <div className="mb-3">
                <label className="form-label fw-semibold">Password</label>
                <div className="input-group input-group-lg">
                  <span className="input-group-text">
                    <i className="bi bi-lock-fill"></i>
                  </span>
                  <input
                    type="password"
                    className="form-control"
                    placeholder="Digite sua senha"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                  />
                </div>
              </div>
              <div className="form-check mb-3">
                <input
                  type="checkbox"
                  className="form-check-input"
                  checked={remember}
                  onChange={(e) => setRemember(e.target.checked)}
                />
                <label className="form-check-label">Lembrar-me</label>
              </div>
              <button
                type="submit"
                className="btn btn-primary btn-lg w-100 rounded-pill shadow-sm"
              >
                <i className="bi bi-box-arrow-in-right me-2"></i> Entrar
              </button>
            </form>
          </div>

          {/* Imagem figurativa */}
          <div className="col-md-6">
            <img
              src="https://images.unsplash.com/photo-1522202176988-66273c2fd55f?auto=format&fit=crop&w=900&q=80"
              alt="Login illustration"
              className="img-fluid h-100 w-100 rounded-end-4 object-fit-cover"
            />
          </div>
        </div>
      </div>
    </div>
  );
}

export default Login;