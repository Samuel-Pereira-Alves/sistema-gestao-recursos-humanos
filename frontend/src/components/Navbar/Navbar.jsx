import React, { useEffect, useState, useRef } from "react";
import { Link } from "react-router-dom";
import NotificationBell from "../NotificationBell/NotificationBell";
import axios from 'axios';
import { getEmployee } from "../../Service/employeeService";

function Navbar() {
  const [open, setOpen] = useState(false);
  const bellBtnRef = useRef(null);
  const dropdownRef = useRef(null);

  const businessEntityId = localStorage.getItem("businessEntityId");
  const isLoggedIn = Boolean(businessEntityId);

  const [loadingName, setLoadingName] = useState(false);

  useEffect(() => {
    let cancelled = false;
    async function loadUserName() {
      if (!isLoggedIn || !businessEntityId) return;
      try {
        const token = localStorage.getItem("authToken");
        setLoadingName(true);
        const data = await getEmployee(businessEntityId, token);
        const first = data?.person?.firstName || "";
        const middle = data?.person?.middleName || "";
        const last = data?.person?.lastName || "";
        const full = [first, middle, last].filter(Boolean).join(" ").trim() || "Utilizador";
        if (!cancelled) {
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

  const profileUrl = `/profile/${localStorage.getItem("businessEntityId")}`;
  const logout = () => {
    localStorage.clear();
    window.location.href = "/";
  };

  const first = localStorage.getItem("firstName") || "";
  const last = localStorage.getItem("lastName") || "";

  const initials = `${first.charAt(0)}${last.charAt(0)}`.toUpperCase() || "U";

  useEffect(() => {
    function handleClickOutside(e) {
      if (!open) return;
      const insideDropdown = dropdownRef.current?.contains(e.target);
      const insideButton = bellBtnRef.current?.contains(e.target);
      if (!insideDropdown && !insideButton) setOpen(false);
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
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