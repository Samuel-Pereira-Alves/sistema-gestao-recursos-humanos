
// src/pages/HomePage.jsx
import React, { useEffect, useRef, useState } from "react";
import { useLocation, Link } from "react-router-dom";
import Navbar from "./Navbar";
import FormPage from "./FormPage";

function HomePage() {
  
  const [role, setRole] = useState(localStorage.getItem("role") || null);

  
  const [mostrarForm, setMostrarForm] = useState(false);

  const location = useLocation();
  const cardRef = useRef(null);

  
  useEffect(() => {
    if (location.state?.openCandidatura && !role) {
      setMostrarForm(true);
      
      window.history.replaceState({}, document.title);
      
      setTimeout(() => cardRef.current?.scrollIntoView({ behavior: "smooth" }), 0);
    }
  }, [location.state, role]);

  
  useEffect(() => {
    const onStorage = (e) => {
      if (e.key === "role") {
        setRole(e.newValue || null);
        
        if (e.newValue) setMostrarForm(false);
      }
    };
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, []);

  
  const abrirForm = () => {
    if (!role) setMostrarForm(true);
  };
  const fecharForm = () => setMostrarForm(false);

  return (
    <div>
      <Navbar />

      <main className="container" style={{ marginTop: "80px", marginBottom: "80px" }}>
        <h2 className="mt-4">Bem-vindo ao Sistema de Gestão de RH</h2>

        <div className="my-5" />

        
        {!role && (
          <div className="card mt-5" style={{ backgroundColor: "#f1f3f5" }} ref={cardRef}>
            <div className="card-body" style={{ maxWidth: 800, margin: "0 auto" }}>
              
              {!mostrarForm && (
                <>
                  <h5 className="card-title">Candidatura espontânea</h5>
                  <p className="card-text">
                    Envia-nos o teu CV e os teus dados para análise futura de oportunidades.
                  </p>
                  <button
                    className="btn btn-secondary"
                    onClick={abrirForm}
                    aria-expanded={mostrarForm}
                    aria-controls="form-candidatura"
                  >
                    Submeter candidatura
                  </button>
                </>
              )}

              
              {mostrarForm && (
                <div id="form-candidatura" className="mt-3">
                  <FormPage hideNavbar variant="embedded" onCancel={fecharForm} />
                </div>
              )}
            </div>
          </div>
        )}

        
        {role && (
          <section className="mt-5">
            <h5>Área do colaborador</h5>
            <div className="row g-3 mt-2">
              <div className="col-md-4">
                <div className="card h-100">
                  <div className="card-body">
                    <h6 className="card-title">Perfil</h6>
                    <p className="card-text">Consulta e atualiza os teus dados pessoais.</p>
                    <Link className="btn btn-outline-primary" to="/profile">Ir para Perfil</Link>
                  </div>
                </div>
              </div>

              <div className="col-md-4">
                <div className="card h-100">
                  <div className="card-body">
                    <h6 className="card-title">Pagamentos</h6>
                    <p className="card-text">Consulta histórico de pagamentos e recibos.</p>
                    <Link className="btn btn-outline-primary" to="/payhistory">Ver Pagamentos</Link>
                  </div>
                </div>
              </div>

              <div className="col-md-4">
                <div className="card h-100">
                  <div className="card-body">
                    <h6 className="card-title">Departamentos</h6>
                    <p className="card-text">Informação sobre os teus departamentos e equipa.</p>
                    <Link className="btn btn-outline-primary" to="/dephistory">Ver Departamentos</Link>
                  </div>
                </div>
              </div>
            </div>
          </section>
        )}

        
        {role === "admin" && (
          <section className="mt-4">
            <h5>Área de administração</h5>
            <div className="row g-3 mt-2">
              
              <div className="col-md-4">
                <div className="card h-100">
                  <div className="card-body">
                    <h6 className="card-title">Candidatos</h6>
                    <p className="card-text">Rever candidaturas e pipeline de recrutamento.</p>
                    <Link className="btn btn-outline-dark" to="/candidatos">Ver Candidatos</Link>
                  </div>
                </div>
              </div>

              <div className="col-md-4">
                <div className="card h-100">
                  <div className="card-body">
                    <h6 className="card-title">Funcionários</h6>
                    <p className="card-text">Gestão de colaboradores e dados contratuais.</p>
                    <Link className="btn btn-outline-dark" to="/funcionarios">Gestão de Funcionários</Link>
                  </div>
                </div>
              </div>

              <div className="col-md-4">
                <div className="card h-100 mt-3 mt-md-0">
                  <div className="card-body">
                    <h6 className="card-title">Pagamentos</h6>
                    <p className="card-text">Historico de Pagamentos feitos aos Colaboradores.</p>
                    <Link className="btn btn-outline-dark" to="/gestao-pagamentos">Gestão de Pagamentos Efetuados</Link>
                  </div>
                </div>
              </div>

              <div className="col-md-4">
                <div className="card h-100 mt-3 mt-md-0">
                  <div className="card-body">
                    <h6 className="card-title">Movimentos de Departamento</h6>
                    <p className="card-text">Gerir movimentos de Departamento.</p>
                    <Link className="btn btn-outline-dark" to="/gestao-movimentos">Gestão de Movimentos de Departamento</Link>
                  </div>
                </div>
              </div>
            </div>
          </section>
        )}
      </main>
    </div>
  );
}

export default HomePage;
