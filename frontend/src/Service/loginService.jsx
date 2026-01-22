const API_BASE = "http://localhost:5136/api/v1";

export async function login(username, password) {
  const res = await fetch(`${API_BASE}/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
    },
    body: JSON.stringify({ username, password }),
  });

  if (!res.ok) {
    let msg = `Falha no login (HTTP ${res.status})`;
    try {
      const ct = res.headers.get("content-type") || "";
      if (ct.includes("application/json")) {
        const err = await res.json();
        if (err?.message) msg = err.message;
        else msg = `${msg} - ${JSON.stringify(err)}`;
      } else {
        const txt = await res.text();
        if (txt) msg = `${msg} - ${txt}`;
      }
    } catch { }
    throw new Error(msg);
  }

  return res.json();
}