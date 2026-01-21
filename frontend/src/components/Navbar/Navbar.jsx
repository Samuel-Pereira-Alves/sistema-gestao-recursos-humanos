
import React, { useEffect, useState, useRef, useCallback } from "react";
import { Link } from "react-router-dom";
import NotificationBell from "../NotificationBell/NotificationBell";
import { getEmployee } from "../../Service/employeeService";

function Navbar() {
  const [open, setOpen] = useState(false);
  const bellBtnRef = useRef(null);
  const dropdownRef = useRef(null);

  // ✅ Estado que reflete o localStorage (torna reativo)
  const [businessEntityId, setBusinessEntityId] = useState(() => localStorage.getItem("businessEntityId"));
  const [firstName, setFirstName] = useState(() => localStorage.getItem("firstName") || "");
  const [lastName, setLastName] = useState(() => localStorage.getItem("lastName") || "");
  const [loadingName, setLoadingName] = useState(false);

  const isLoggedIn = Boolean(businessEntityId);
  const profileUrl = businessEntityId ? `/profile/${businessEntityId}` : "/profile";

  // ✅ Função para sincronizar o estado a partir do localStorage
  const syncFromStorage = useCallback(() => {
    setBusinessEntityId(localStorage.getItem("businessEntityId"));
    setFirstName(localStorage.getItem("firstName") || "");
    setLastName(localStorage.getItem("lastName") || "");
  }, []);

  // ✅ Ouve o evento custom "authChanged" (disparado no login) e o nativo "storage"
  useEffect(() => {
    const onAuthChanged = () => syncFromStorage();
    const onStorage = (e) => {
      if (["businessEntityId", "firstName", "lastName", "authToken"].includes(e.key)) {
        syncFromStorage();
      }
    };

    window.addEventListener("authChanged", onAuthChanged);
    window.addEventListener("storage", onStorage);
    return () => {
      window.removeEventListener("authChanged", onAuthChanged);
      window.removeEventListener("storage", onStorage);
    };
  }, [syncFromStorage]);

  // ✅ Carregar o nome após login (se faltar) — sem ler localStorage nas deps
  useEffect(() => {
    let cancelled = false;
    async function loadUserName() {
      if (!isLoggedIn || !businessEntityId) return;
      if (firstName && lastName) return; // já tens
      try {
        setLoadingName(true);
        const token = localStorage.getItem("authToken");
        const data = await getEmployee(businessEntityId, token);
        const first = data?.person?.firstName || "";
        const middle = data?.person?.middleName || "";
        const last = data?.person?.lastName || "";
        const full = [first, middle, last].filter(Boolean).join(" ").trim() || "Utilizador";

        if (!cancelled) {
          // Atualiza storage + estado para re-render imediato
          localStorage.setItem("userName", full);
          localStorage.setItem("firstName", first);
          localStorage.setItem("lastName", last);
          setFirstName(first);
          setLastName(last);
          // (Opcional) notifica outros listeners
          window.dispatchEvent(new Event("authChanged"));
        }
      } catch (err) {
        console.error(err);
      } finally {
        if (!cancelled) setLoadingName(false);
      }
    }
    loadUserName();
    return () => { cancelled = true; };
  }, [isLoggedIn, businessEntityId, firstName, lastName]);

  const initials = ((firstName?.charAt(0) || "") + (lastName?.charAt(0) || "")).toUpperCase() || "U";

  // Outside click do dropdown
  useEffect(() => {
    function handleClickOutside(e) {
      if (!open) return;
      const insideDropdown = dropdownRef.current?.contains(e.target);
      const insideButton = bellBtnRef.current?.contains(e.target);
      if (!insideDropdown && !insideButton) setOpen(false);
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [open]);

  const logout = () => {
    localStorage.clear();
    // Atualiza estado imediatamente + notifica
    setBusinessEntityId(null);
    setFirstName("");
    setLastName("");
    window.dispatchEvent(new Event("authChanged"));
    window.location.href = "/";
  };

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
                {/* Sino */}
                <NotificationBell className="me-3" />

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
