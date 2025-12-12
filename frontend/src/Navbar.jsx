// src/components/layout/Navbar.jsx
import React, { useContext } from "react";
import { Link } from "react-router-dom";

function Navbar() {
  const role = localStorage.getItem("role");
  const logout = () => {
    localStorage.clear();
    window.location.href = "/";
  }

  return (
    <nav className="navbar navbar-expand-lg navbar-dark bg-dark fixed-top">
      <div className="container-fluid">
        <Link className="navbar-brand" to="/">HR Management</Link>
        <div className="collapse navbar-collapse" id="navbarNav">
          <ul className="navbar-nav ms-auto">
            {!role ? (
              <li className="nav-item">
                <Link className="nav-link" to="/login">Login</Link>
              </li>
            ) : (
              <>
                {/* Sempre visível para qualquer user autenticado */}
                <li className="nav-item">
                  <Link className="nav-link" to="/profile">Perfil</Link>
                </li>
                <li className="nav-item">
                  <Link className="nav-link" to="/payhistory">Pagamentos</Link>
                </li>
                <li className="nav-item">
                  <Link className="nav-link" to="/dephistory">Departamentos</Link>
                </li>

                {/* Só admin vê estes extras */}
                {role === "admin" && (
                  <>
                    <li className="nav-item">
                      <Link className="nav-link" to="/rh">Recursos Humanos</Link>
                    </li>
                    <li className="nav-item">
                      <Link className="nav-link" to="/candidates">Candidatos</Link>
                    </li>
                    <li className="nav-item">
                      <Link className="nav-link" to="/funcionarios">Funcionarios</Link>
                    </li>
                    <li className="nav-item">
                      <Link className="nav-link" to="/gestao-pagamentos">Pagamentos</Link>
                    </li>
                    <li className="nav-item">
                      <Link className="nav-link" to="/gestao-movimentos">Movimentos</Link>
                    </li>
                  </>
                )}

                <li className="nav-item">
                  <button className="btn btn-link nav-link" onClick={logout}>
                    Logout
                  </button>
                </li>
              </>
            )}
          </ul>
        </div>
      </div>
    </nav>
  );
}

export default Navbar;