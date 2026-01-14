const API_BASE = "http://localhost:5136/api/v1";

export async function getDepartments(token) {
  const res = await fetch(`${API_BASE}/departmenthistory`, {
    headers: { Accept: "application/json", Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Erro ao carregar departamentos");
  return res.json();
}

export async function getEmployee(id, token) {
  const res = await fetch(`${API_BASE}/employee/${id}`, {
    headers: { Accept: "application/json", Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error(`Erro ao carregar funcionário (HTTP ${res.status})`);
  return res.json();
}

export async function updateEmployee(id, payload, token) {
  const res = await fetch(`${API_BASE}/employee/${id}`, {
    method: "PATCH",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}