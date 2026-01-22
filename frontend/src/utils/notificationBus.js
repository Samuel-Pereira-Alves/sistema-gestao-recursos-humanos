
let _notifications = [];
let _subscribers = new Set();


function _emit() {
  const snapshot = _notifications.slice();
  _subscribers.forEach((fn) => {
    try {
      fn(snapshot);
    } catch (e) {
      console.error("notificationBus subscriber error:", e);
    }
  });
}

export async function syncNotificationsFromServer() {
  const token = localStorage.getItem("authToken");
  try {
    const id = localStorage.getItem("businessEntityId") || "";
    const res = await fetch(
      `http://localhost:5136/api/v1/notification/by-entity/${id}`,
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
    return _notifications;
  } catch (err) {
    console.error("Failed to fetch notifications from server:", err);
    return _notifications;
  }
}

export async function addNotification(message, role, meta = {}) {
  try {
    await fetch(`http://localhost:5136/api/v1/notification/${role}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ message, type: meta.type ?? null }),
    });
  } catch (err) {
    console.error("Failed to send notification to server:", err);
  }

  await syncNotificationsFromServer();
}

export async function addNotificationForUser(message, id, meta = {}) {
  const token = localStorage.getItem("authToken");

  try {
    await fetch(`http://localhost:5136/api/v1/notification/`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({
        message,
        businessEntityId: id,
        type: meta.type ?? null,
      }),
    });
  } catch (err) {
    console.error("Failed to send notification to server:", err);
  }

  await syncNotificationsFromServer();
}

export async function deleteNotification(id) {
  const token = localStorage.getItem("authToken");
  try {
    await fetch(`http://localhost:5136/api/v1/notification/${id}`, {
      method: "DELETE",
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
      },
    });
  } catch (err) {
    console.error("Failed to delete notification from server:", err);
  }

  _notifications = _notifications.filter((n) => n.id !== id);
  _emit();
}

export async function clearNotifications() {
  const token = localStorage.getItem("authToken");

  for (const n of _notifications) {
    try {
      await fetch(`http://localhost:5136/api/v1/notification/${n.id}`, {
        method: "DELETE",
        headers: {
          Accept: "application/json",
          Authorization: `Bearer ${token}`,
        },
      });
    } catch (err) {
      console.error("Failed to delete notification from server:", err);
    }
  }

  _notifications = [];
  _emit();
}

export async function removeNotifications() {
  const token = localStorage.getItem("authToken");
  const id = localStorage.getItem("businessEntityId") || "";

  try {
    await fetch(`http://localhost:5136/api/v1/notification/by-entity/${id}`, {
      method: "DELETE",
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
      },
    });
  } catch (err) {
    console.error("Failed to delete notifications from server:", err);
  }
  _notifications = [];
  _emit();
}

export function getNotifications() {
  return _notifications.slice();
}

export function subscribe(listener) {
  _subscribers.add(listener);
  try {
    listener(_notifications.slice()); 
  } catch (e) {
    console.error("notificationBus immediate dispatch error:", e);
  }

  return () => {
    _subscribers.delete(listener);
  };
}
