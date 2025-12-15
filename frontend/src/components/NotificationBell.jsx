
// src/components/layout/NotificationBell.jsx
import React, { useEffect, useRef, useState } from "react";
// Ajusta o caminho conforme a tua estrutura (assumindo src/components/layout -> src/store)
import { subscribe, getNotifications, clearNotifications } from "../store/notificationBus";

export default function NotificationBell() {
  const [open, setOpen] = useState(false);
  const [items, setItems] = useState(() => getNotifications());
  const dropdownRef = useRef(null);
  const bellBtnRef = useRef(null);

  // Subscrever ao bus para atualizar items
  useEffect(() => {
    const unsub = subscribe((list) => setItems(list));
    return unsub;
  }, []);

  // Fecha ao clicar fora / ESC
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

  const count = items.length;

  return (
    // Abre para a ESQUERDA com .dropstart e afasta com me-3
    <li className="nav-item dropstart me-3">
      <button
        type="button"
        ref={bellBtnRef}
        className="btn btn-link nav-link p-0 position-relative"
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="true"
        aria-expanded={open}
        aria-controls="notifications-menu"
        title="Notificações"
        style={{ lineHeight: 1 }}
      >
        <i className="bi bi-bell fs-5" aria-hidden="true"></i>

        {count > 0 && (
          <span
            className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
            style={{ fontSize: "0.70rem" }}
          >
            {count > 99 ? "99+" : count}
            <span className="visually-hidden">notificações não lidas</span>
          </span>
        )}
      </button>

      {/* Dropdown alimentado pelo bus */}
      <div
        id="notifications-menu"
        ref={dropdownRef}
        className={`dropdown-menu shadow ${open ? "show" : ""}`}
        style={{ minWidth: 280, maxWidth: 360, maxHeight: 400, overflowY: "auto" }}
        role="menu"
      >
        <div className="px-3 py-2 border-bottom d-flex justify-content-between align-items-center">
          <strong>Notificações</strong>
          {count > 0 && (
            <button className="btn btn-sm btn-outline-secondary" onClick={clearNotifications}>
              Limpar
            </button>
          )}
        </div>

        {count === 0 ? (
          <div className="px-3 py-3 text-muted">Sem notificações.</div>
        ) : (
          <ul className="list-unstyled mb-0">
            {items.map((msg, idx) => (
                           <li key={`${idx}-${msg}`} className="px-3 py-2 border-bottom">
                <div className="text-wrap">{msg}</div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </li>
  );
}