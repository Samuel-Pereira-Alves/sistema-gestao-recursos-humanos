
import React, { useEffect, useRef, useState } from "react";
import {
  subscribe,
  getNotifications,
  clearNotifications,
  syncNotificationsFromServer,
} from "../../utils/notificationBus";

export default function NotificationBell() {
  const [open, setOpen] = useState(false);
  const [allItems, setAllItems] = useState(() => getNotifications());
  const [role, setRole] = useState(() => getRoleFromStorage());        
  const [userId, setUserId] = useState(() => getUserIdFromStorage()); 
  const dropdownRef = useRef(null);
  const bellBtnRef = useRef(null);

  useEffect(() => {
    const r = getRoleFromStorage();
    const u = getUserIdFromStorage();
    setRole(r);
    setUserId(u);
    syncNotificationsFromServer();
  }, []);

  useEffect(() => {
    function handleStorage(e) {
      if (e.key === "role") {
        const r = getRoleFromStorage();
        setRole(r);
      }

      if (e.key === "businessEntityId") {
        const u = getUserIdFromStorage();
        setUserId(u);
      }
    }
    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, []);

  useEffect(() => {
    const unsub = subscribe((list) => {
      const safeList = Array.isArray(list) ? list : [];
      setAllItems(safeList);
    });
    return unsub;
  }, []);

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

        {allItems.length > 0 && (
          <span
            className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
            style={{ fontSize: "0.70rem" }}
          >
            {allItems.length > 99 ? "99+" : allItems.length}
            <span className="visually-hidden">notificações não lidas</span>
          </span>
        )}
      </button>

      <div
        id="notifications-menu"
        ref={dropdownRef}
        className={`dropdown-menu shadow ${open ? "show" : ""}`}
        style={{ minWidth: 280, maxWidth: 360, maxHeight: 400, overflowY: "auto" }}
      >
        <div className="px-3 py-2 border-bottom d-flex justify-content-between align-items-center">
          <strong>Notificações</strong>
          {allItems.length > 0 && (
            <button className="btn btn-sm btn-outline-secondary" onClick={clearNotifications}>
              Limpar
            </button>
          )}
        </div>

        {allItems.length === 0 ? (
          <div className="px-3 py-3 text-muted">Sem notificações.</div>
        ) : (
          <ul className="list-unstyled mb-0">
            {allItems.map((n) => (
              <li key={`${n.id}`} className="px-3 py-2 border-bottom">
                <div className="text-wrap">{n.message}</div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </li>
  );
}

function getRoleFromStorage() {
  try {
    const raw = window?.localStorage?.getItem("role");
    const v = String(raw || "").trim().toLowerCase();
    if (v === "admin" || v === "employee") return v;
    return "employee"; 
  } catch {
    return "employee";
  }
}

function getUserIdFromStorage() {
  try {
    const raw = window?.localStorage?.getItem("businessEntityId");
    const v = String(raw || "").trim();
    console.debug("[NotificationBell] getUserIdFromStorage ->", v);
    return v; 
  } catch {
    return "";
  }
}