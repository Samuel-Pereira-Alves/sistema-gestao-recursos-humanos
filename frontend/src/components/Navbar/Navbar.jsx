
import React, { useEffect, useMemo, useState, useCallback } from "react";
import { Link, useLocation } from "react-router-dom";
import NotificationBell from "../NotificationBell/NotificationBell";
import { getEmployee } from "../../Service/employeeService";

function Navbar() {
  const [isOpen, setIsOpen] = useState(false); 
  const [businessEntityId, setBusinessEntityId] = useState(() => localStorage.getItem("businessEntityId"));
  const [firstName, setFirstName] = useState(() => localStorage.getItem("firstName") || "");
  const [lastName, setLastName] = useState(() => localStorage.getItem("lastName") || "");
  const [loadingName, setLoadingName] = useState(false);

  const isLoggedIn = Boolean(businessEntityId);
  const profileUrl = useMemo(
    () => (businessEntityId ? `/profile/${businessEntityId}` : "/profile"),
    [businessEntityId]
  );

  const location = useLocation();
  useEffect(() => {
    setIsOpen(false);
  }, [location.pathname]);

  const syncFromStorage = useCallback(() => {
    setBusinessEntityId(localStorage.getItem("businessEntityId"));
    setFirstName(localStorage.getItem("firstName") || "");
    setLastName(localStorage.getItem("lastName") || "");
  }, []);

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

  useEffect(() => {
    let cancelled = false;
    async function loadUserName() {
      if (!isLoggedIn || !businessEntityId) return;
      if (firstName && lastName) return;
      try {
        setLoadingName(true);
        const token = localStorage.getItem("authToken");
        const data = await getEmployee(businessEntityId, token);
        const first = data?.person?.firstName || "";
        const middle = data?.person?.middleName || "";
        const last = data?.person?.lastName || "";
        const full = [first, middle, last].filter(Boolean).join(" ").trim() || "Utilizador";

        if (!cancelled) {
          localStorage.setItem("userName", full);
          localStorage.setItem("firstName", first);
          localStorage.setItem("lastName", last);
          setFirstName(first);
          setLastName(last);
          window.dispatchEvent(new Event("authChanged"));
        }
      } catch (err) {
        console.error("Erro ao carregar nome:", err);
      } finally {
        if (!cancelled) setLoadingName(false);
      }
    }
    loadUserName();
    return () => {
      cancelled = true;
    };
  }, [isLoggedIn, businessEntityId, firstName, lastName]);

  const initials = ((firstName?.charAt(0) || "") + (lastName?.charAt(0) || "")).toUpperCase() || "U";

  const toggleNav = () => setIsOpen((v) => !v);
  const closeNav = () => setIsOpen(false);

  const logout = () => {
    localStorage.clear();
    setBusinessEntityId(null);
    setFirstName("");
    setLastName("");
    window.dispatchEvent(new Event("authChanged"));
    window.location.href = "/";
  };

  return (
    <nav className="navbar navbar-expand-lg navbar-light bg-light border-bottom shadow-sm fixed-top">
      <div className="container-fluid">
        <Link className="navbar-brand fw-semibold text-dark" to="/" onClick={closeNav}>
          HR Management
        </Link>

        {/* Botão do toggler */}
        <button
          className="navbar-toggler"
          type="button"
          aria-controls="navbarNav"
          aria-expanded={isOpen}
          aria-label="Alternar navegação"
          onClick={toggleNav}
        >
          <span className="navbar-toggler-icon"></span>
        </button>

        {/* Collapse */}
        <div className={`collapse navbar-collapse ${isOpen ? "show" : ""}`} id="navbarNav">
          <ul className="navbar-nav ms-auto align-items-lg-center">
            <li className="nav-item d-lg-none">
            </li>

            {isLoggedIn ? (
              <>
                <NotificationBell className="me-2" />
                <li className="nav-item d-flex align-items-center me-lg-3">
                  <span className="text-muted me-2 d-none d-md-inline">Bem-vindo,</span>
                  <Link
                    to={profileUrl}
                    className="nav-link px-0 d-flex align-items-center"
                    title="Ir para o seu perfil"
                    onClick={closeNav}
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

                <li className="nav-item">
                  <button
                    type="button"
                    className="btn btn-link nav-link text-decoration-none text-dark px-0"
                    onClick={() => {
                      closeNav();
                      logout();
                    }}
                  >
                    Logout
                  </button>
                </li>
              </>
            ) : (
              <li className="nav-item">
                <Link className="nav-link" to="/login" onClick={closeNav}>
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