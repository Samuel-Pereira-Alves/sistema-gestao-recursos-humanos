
// src/store/notificationBus.js

let _notifications = []; // array de strings ou objetos, simples para já
let _subscribers = new Set();

/**
 * Adiciona uma notificação (string simples).
 * Ex.: addNotification("teste");
 */
export function addNotification(message) {
  const text = String(message ?? "").trim();
  if (!text) return;

  _notifications = [..._notifications, text];
  fetch('http://localhost:5136/api/v1/notification', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message: text }),
  }).catch((err) => {
    console.error('Failed to send notification to server:', err);
  });
  _emit();
}

/** Limpa todas as notificações */
export function clearNotifications() {
  _notifications = [];
  _emit();
}

/** (Opcional) remove por índice */
export function removeNotificationAt(index) {
  if (index < 0 || index >= _notifications.length) return;
  _notifications = _notifications.filter((_, i) => i !== index);
  _emit();
}


export async function syncNotificationsFromServer() {
  try {
    const res = await fetch('http://localhost:5136/api/v1/notification');
    const data = await res.json(); // <- espera Array<{id, message}>
    _notifications = Array.isArray(data) ? data : [];
    console.debug('Notifications synced from server:', _notifications);
    _emit();
  } catch (err) {
    console.error('Failed to fetch notifications from server:', err);
  }
}

export function getNotifications() {
  return _notifications.slice(); // objetos
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


