
import React, { useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import { useNavigate } from "react-router-dom";
import { login } from "../../Service/loginService";

function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");

  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setErrorMsg("");
    setLoading(true);

    try {
      const data = await login(username, password);

      localStorage.setItem("authToken", data.token);
      localStorage.setItem("role", data.role);
      localStorage.setItem("employeeId", data.employeeId);
      localStorage.setItem("systemUserId", data.systemUserId);
      localStorage.setItem("businessEntityId", data.businessEntityId);
      localStorage.setItem("username", username);

      window.dispatchEvent(new Event("authChanged"));

      navigate("/");
    } catch (error) {
      console.error("Erro no login:", error);
      // Mostra mensagem amigável; podes usar error.message se preferires a do backend
      setErrorMsg("Credenciais inválidas. Tente novamente.");
    } finally {
      setLoading(false);
    }
  };


  return (
    <div className="login-bg d-flex align-items-center justify-content-center min-vh-100">
      <div className="login-card card border-0 shadow-sm">
        <div className="card-body p-4 p-md-5">
          <header className="text-center mb-4">
            <h1 className="app-title mb-2">Gestão de Recursos Humanos</h1>
            <p className="app-subtitle mb-0">
              Inicie sessão para continuar
            </p>
          </header>

          {errorMsg && (
            <div className="alert alert-danger py-2 px-3 mb-4" role="alert">
              {errorMsg}
            </div>
          )}

          <form onSubmit={handleSubmit} noValidate>
            <div className="mb-3">
              <label htmlFor="username" className="form-label fw-semibold">
                Utilizador
              </label>
              <input
                id="username"
                type="text"
                className="form-control form-control-lg input-gray"
                placeholder="Nome"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                required
                autoComplete="username"
              />
            </div>

            <div className="mb-3">
              <label htmlFor="password" className="form-label fw-semibold">
                Palavra-passe
              </label>
              <input
                id="password"
                type="password"
                className="form-control form-control-lg input-gray"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
            </div>

            <button
              type="submit"
              className="btn btn-gray btn-lg w-100"
              disabled={loading}
            >
              {loading ? "A entrar..." : "Entrar"}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

export default Login;