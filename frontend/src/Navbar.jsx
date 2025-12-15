
import React, { useEffect, useState, useRef } from "react";
import { Link } from "react-router-dom";
import NotificationBell from "./components/NotificationBell";
import { addNotification } from "./store/notificationBus";

function Navbar() {
  const [open, setOpen] = useState(false);
  const bellBtnRef = useRef(null);
  const dropdownRef = useRef(null);

  // Ajusta aqui o número do badge (pode vir de API no futuro)
  const NOTIFICATION_COUNT = 3;

  const businessEntityId = localStorage.getItem("businessEntityId");
  const isLoggedIn = Boolean(businessEntityId);

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
          localStorage.setItem("userName", full);
          if (first) localStorage.setItem("firstName", first);
          if (last) localStorage.setItem("lastName", last);
        }
      } catch (err) {
        console.error(err);
      } finally {
        if (!cancelled) setLoadingName(false);
      }
    }
    loadUserName();
    return () => { cancelled = true; };
  }, [isLoggedIn, businessEntityId]);

  const profileUrl = "/profile";
  const logout = () => {
    localStorage.clear();
    window.location.href = "/";
  };

  const initials = (userName || "U")
    .split(" ")
    .map((p) => p[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();

  // Fechar ao clicar fora / ESC (opcional mas recomendado)
  useEffect(() => {
    function handleClickOutside(e) {
      if (!open) return;
      const insideDropdown = dropdownRef.current?.contains(e.target);
      const insideButton = bellBtnRef.current?.contains(e.target);
      if (!insideDropdown && !insideButton) setOpen(false);
    }
    function handleEsc(e) {
      if (e.key === "Escape") {
        setOpen(false);
        bellBtnRef.current?.focus();
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("keydown", handleEsc);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleEsc);
    };
  }, [open]);

  return (
    <nav className="navbar navbar-expand-lg navbar-light bg-light border-bottom shadow-sm fixed-top">
      <div className="container-fluid">
        <Link className="navbar-brand fw-semibold text-dark" to="/">
          HR Management
        </Link>

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

        <div className="collapse navbar-collapse" id="navbarNav">
          <ul className="navbar-nav ms-auto align-items-lg-center">
            {isLoggedIn ? (
              <>
              <button onClick={() => addNotification("testee")}>
                <p>Clica me</p>
              </button>
                {/* Sino + badge */}
                <NotificationBell className="me-3" />

                {/* Bem-vindo + perfil */}
                <li className="nav-item d-flex align-items-center me-lg-3 ">
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