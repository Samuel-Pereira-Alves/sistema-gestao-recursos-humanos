
// src/components/layout/Navbar.jsx
// Se estiveres a usar Next.js App Router:
// 'use client';

import React, { useEffect, useState } from "react";
import { Link } from "react-router-dom";

function Navbar() {
  // Consideramos "logado" se existir businessEntityId (podes trocar por token/role)
  const businessEntityId = localStorage.getItem("businessEntityId");
  const isLoggedIn = Boolean(businessEntityId);

  // Estado para o nome (inicializa com localStorage para evitar flash)
  const [userName, setUserName] = useState(() => {
    const stored =
      localStorage.getItem("userName") ||
      [localStorage.getItem("firstName"), localStorage.getItem("lastName")]
        .filter(Boolean)
        .join(" ")
        .trim();
    return stored || "Utilizador";
  });

  const [loadingName, setLoadingName] = useState(false);

  // Buscar nome pela API (opcional) quando logado
  useEffect(() => {
    let cancelled = false;

    async function loadUserName() {
      if (!isLoggedIn || !businessEntityId) return;

      try {
        setLoadingName(true);
        const res = await fetch(`http://localhost:5136/api/v1/employee/${businessEntityId}`);
        if (!res.ok) throw new Error(`Falha ao obter utilizador (HTTP ${res.status})`);
        const data = await res.json();

        const first = data?.person?.firstName || "";
        const middle = data?.person?.middleName || "";
        const last = data?.person?.lastName || "";
        const full = [first, middle, last].filter(Boolean).join(" ").trim() || "Utilizador";

        if (!cancelled) {
          setUserName(full);
          // Guarda para arranques futuros rápidos
          localStorage.setItem("userName", full);
          if (first) localStorage.setItem("firstName", first);
          if (last) localStorage.setItem("lastName", last);
        }
      } catch (err) {
        console.error(err);
        // Mantém fallback do localStorage; podes mostrar um toast se quiseres
      } finally {
        if (!cancelled) setLoadingName(false);
      }
    }

    loadUserName();
    return () => {
      cancelled = true;
    };
  }, [isLoggedIn, businessEntityId]);

  const profileUrl = "/profile";

  const logout = () => {
    localStorage.clear();
    window.location.href = "/"; // redireciona para home/login
  };

  const initials = (userName || "U")
    .split(" ")
    .map((p) => p[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();

  return (
    <nav className="navbar navbar-expand-lg navbar-light bg-light border-bottom shadow-sm fixed-top">
      <div className="container-fluid">
        {/* Brand minimalista */}
        <Link className="navbar-brand fw-semibold text-dark" to="/">
          HR Management
        </Link>

        {/* Toggler (mobile) */}
        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#navbarNav"
          aria-controls="navbarNav"
          aria-expanded="false"
          aria-label="Alternar navegação"
        >
          <span className="navbar-toggler-icon"></span>
        </button>

        {/* Itens de navegação (direita) */}
        <div className="collapse navbar-collapse" id="navbarNav">
          <ul className="navbar-nav ms-auto align-items-lg-center">
            {isLoggedIn ? (
              <>
                {/* Bem-vindo + link para perfil */}
                <li className="nav-item d-flex align-items-center me-lg-3">
                  <span className="text-muted me-2 d-none d-md-inline">Bem-vindo,</span>
                  <Link
                    to={profileUrl}
                    className="nav-link px-0 d-flex align-items-center"
                    title="Ir para o seu perfil"
                  >
                    <div
                      className="rounded-circle bg-secondary bg-opacity-25 d-flex align-items-center justify-content-center me-2"
                      style={{ width: 28, height: 28 }}
                      aria-label="Avatar"
                    >
                      <span className="text-muted small fw-bold">
                        {loadingName ? "…" : initials}
                      </span>
                    </div>
                    
                  </Link>
                </li>

                {/* Logout */}
                <li className="nav-item">
                  <button
                    type="button"
                    className="btn btn-link nav-link text-decoration-none text-dark px-0"
                    onClick={logout}
                  >
                    Logout
                  </button>
                </li>
              </>
            ) : (
              // Não logado: apenas Login
              <li className="nav-item">
                <Link className="nav-link" to="/login">
                  Login
                </Link>
              </li>
            )}
          </ul>
        </div>
      </div>
    </nav>
   );
}


export default Navbar;