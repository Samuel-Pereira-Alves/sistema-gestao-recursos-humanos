
// src/store/notificationBus.js

let _notifications = []; // array de strings ou objetos, simples para já
let _subscribers = new Set();

/**
 * Adiciona uma notificação (string simples).
 * Ex.: addNotification("teste");
 */
export function addNotification(message, role) {
  fetch(`http://localhost:5136/api/v1/notification/${role}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message }),
  }).catch((err) => {
    console.error('Failed to send notification to server:', err);
  });

  syncNotificationsFromServer();
  _emit();
}

export function addNotificationForUser(message, id) {
  const token = localStorage.getItem("authToken");
  fetch(`http://localhost:5136/api/v1/notification/`, {
    method: 'POST',
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ message, businessEntityId: id }),
  }).catch((err) => {
    console.error('Failed to send notification to server:', err);
  });

  syncNotificationsFromServer();
  _emit();
}

/** Limpa todas as notificações */
export async function clearNotifications() {
  const token = localStorage.getItem("authToken");
  for (const n of _notifications) {
    await fetch(`http://localhost:5136/api/v1/notification/${n.id}`, {
      method: 'DELETE',
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
      },
    }).catch((err) => {
      console.error('Failed to delete notification from server:', err);
    });
  }
  _notifications = [];
  _emit();
}

/** (Opcional) remove por índice */
export async function removeNotifications() {
  const token = localStorage.getItem("authToken");

  const id = localStorage.getItem('businessEntityId') || '';
  await fetch(`http://localhost:5136/api/v1/notification/by-entity/${id}`, {
    method: 'DELETE',
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  }).catch((err) => {
    console.error('Failed to delete notification from server:', err);
  });
  _emit();
}


export async function syncNotificationsFromServer() {
  const token = localStorage.getItem("authToken");
  try {
    const id = localStorage.getItem('businessEntityId') || '';
    const res = await fetch(`http://localhost:5136/api/v1/notification/by-entity/${id}`,
      {
        method: "GET",
        headers: {
          Accept: "application/json",
          Authorization: `Bearer ${token}`,
        },
      }
    );
    const data = await res.json();
    _notifications = Array.isArray(data) ? data : [];
    _emit();
  } catch (err) {
    console.error('Failed to fetch notifications from server:', err);
  }
}

export function getNotifications() {
  return _notifications.slice();
}

function _emit() {
  const snapshot = _notifications.slice();
  _subscribers.forEach((fn) => {
    try { fn(snapshot); } catch (e) { console.error("notificationBus subscriber error: ", e); }
  });
}

/** Subscreve para receber atualizações */
export function subscribe(listener) {
  _subscribers.add(listener);
  // devolve unsubscribe
  return () => {
    _subscribers.delete(listener);
  };
}