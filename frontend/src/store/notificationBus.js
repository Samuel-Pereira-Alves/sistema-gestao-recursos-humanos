
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

/** Lê o estado atual (sem subscrever) */
export function getNotifications() {
  return _notifications.slice();
}

/** Subscreve para receber atualizações */
export function subscribe(listener) {
  _subscribers.add(listener);
  // devolve unsubscribe
  return () => {
    _subscribers.delete(listener);
  };
}

/** Notifica todos os listeners */
function _emit() {
  const snapshot = getNotifications();
  _subscribers.forEach((fn) => {
    try {
      fn(snapshot);
    } catch (e) {
      console.error("notificationBus subscriber error: ", e);
    }
  });
}
