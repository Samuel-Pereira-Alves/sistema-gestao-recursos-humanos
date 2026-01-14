import React, { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  subscribe,
  getNotifications,
  clearNotifications,
  syncNotificationsFromServer,
  deleteNotification,
} from "../../utils/notificationBus";
import "./NotificationBell.css";

export default function NotificationBell() {
  const [open, setOpen] = useState(false);
  const [allItems, setAllItems] = useState(() => getNotifications());
  const dropdownRef = useRef(null);
  const bellBtnRef = useRef(null);
  const navigate = useNavigate();

  useEffect(() => {
    syncNotificationsFromServer();
  }, []);

  useEffect(() => {
    const unsub = subscribe((list) => {
      setAllItems(Array.isArray(list) ? list : []);
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
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [open]);

  const handleItemClick = async (n) => {
    try {
      // Decide rota com base no tipo
      switch (n.type) {
        case "PROFILE":
          navigate(`/profile/${n.businessEntityID}`);
          break;
        case "PROFILE_ADMIN":
          navigate(`/funcionarios`);
          break;
        case "PAYMENT":
          navigate(`/payhistory`);
          break;
        case "DEPARTMENT":
          navigate(`/dephistory`);
          break;
        case "CANDIDATE":
          navigate(`/candidatos`);
          break;
        case "EMPLOYEES":
          navigate(`/funcionarios`);
          break;
        default:
          console.warn("Tipo de notificação desconhecido:", n.type);
      }
      setOpen(false);
    } catch (e) {
      console.error("Erro ao processar clique na notificação:", e);
    }
  };

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
        style={{
          minWidth: 300,
          maxWidth: 380,
          maxHeight: 400,
          overflowY: "auto",
          marginRight: -29,
        }}
      >
        <div className="px-3 py-2 d-flex justify-content-between align-items-center">
          <strong>Notificações</strong>
          {allItems.length > 0 && (
            <button
              className="btn btn-sm btn-outline-secondary"
              onClick={clearNotifications}
            >
              Limpar Tudo
            </button>
          )}
        </div>

        {allItems.length === 0 ? (
          <div className="px-3 py-3 text-muted">Sem notificações.</div>
        ) : (
          <ul className="list-unstyled mb-0 notifications-menu">
            {allItems.map((n) => (
              <li
                key={n.id}
                className="px-3 py-2 border-top d-flex align-items-center justify-content-between"
                style={{ cursor: "pointer" }}
                onClick={() => handleItemClick(n)}
              >
                <span className="test-dark">
                  {n.message}
                </span>

                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    deleteNotification(n.id);
                  }}
                  className="btn btn-link p-0 icon-hover ms-3"
                  title="Eliminar notificação"
                >
                  <i className="bi bi-trash fs-6 text-black"></i>
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </li>
  );
}
