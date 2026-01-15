const API_BASE = "http://localhost:5136/api/v1";

export async function login(username, password) {
  const res = await fetch(`${API_BASE}/login`, {
    headers: { "Content-Type": "application/json"},
    body: JSON.stringify({ username, password }),
    method: "POST"
  });
  if (!res.ok) throw new Error("Erro ao carregar departamentos");
  return res;
}
