import React, { useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
 
function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [remember, setRemember] = useState(false);
 
  const handleSubmit = async (e) => {
    e.preventDefault();
 
    // --- FETCH API (comentado enquanto API não está pronta) ---
    /*
    try {
      const response = await fetch("http://localhost:5000/api/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          username,
          password,
          remember,
        }),
      });
 
      if (!response.ok) {
        throw new Error("Erro no login");
      }
 
      const data = await response.json();
      console.log("Login bem-sucedido:", data);
 
      // Exemplo: guardar token no localStorage
      if (data.token) {
        localStorage.setItem("token", data.token);
      }
 
      // Redirecionar para página principal
      window.location.href = "/HomePage";
    } catch (error) {
      console.error("Erro ao fazer login:", error);
      alert("Falha no login. Verifique suas credenciais.");
    }
    */
 
    // --- TESTE LOCAL ---
    console.log("Username:", username);
    console.log("Password:", password);
    console.log("Remember me:", remember);
  };
 
  return (
    <div className="d-flex align-items-center justify-content-center vh-100 bg-gradient">
      <div className="card shadow-lg border-0 rounded-4" style={{ width: "900px" }}>
        <div className="row g-0">
          {/* Formulário */}
          <div className="col-md-6 bg-white rounded-start-4 d-flex flex-column justify-content-center align-items-center p-5">
            {/* Mensagem de boas-vindas */}
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
            <div className="text-center mt-3">
              <a href="#" className="text-decoration-none text-secondary">
                Esqueceu a senha?
              </a>
            </div>
          </div>
 
          {/* Imagem figurativa */}
          <div className="col-md-6">
            <img
              src="https://images.unsplash.com/photo-1522202176988-66273c2fd55f?auto=format&fit=crop&w=900&q=80%22"
              alt="Login illustration"
              className="img-fluid h-100 w-100 rounded-end-4 object-fit-cover"
            />
          </div>
        </div>
      </div>
 
      {/* Estilo extra */}
      <style>{`
        .bg-gradient {
          background: linear-gradient(135deg, #6a11cb 0%, #2575fc 100%);
        }
        .btn-primary:hover {
          background-color: #0056b3;
          transform: scale(1.02);
          transition: all 0.2s ease-in-out;
        }
        .fade-in {
          opacity: 0;
          transform: translateY(20px);
          animation: fadeInUp 1s forwards;
        }
        .delay-1 {
          animation-delay: 0.5s;
        }
        @keyframes fadeInUp {
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
      `}</style>
    </div>
  );
}
 
export default Login;