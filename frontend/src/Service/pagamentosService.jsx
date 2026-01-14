const API = "http://localhost:5136/api/v1";

function authHeaders() {
  const token = localStorage.getItem("authToken");
  return {
    Accept: "application/json",
    Authorization: token ? `Bearer ${token}` : undefined,
  };
}

async function http(path, init = {}) {
  const headers = { ...authHeaders(), ...(init.headers || {}) };

  const res = await fetch(`${API}${path}`, { ...init, headers });

  const ct = res.headers.get("content-type") || "";
  return ct.includes("application/json") ? res.json() : res.text();
}

export async function listPagamentosFlattened() {
  const data = await http("/employee");
  const employees = Array.isArray(data) ? data : data?.items ?? [];
  const flattened = employees.flatMap(emp =>
    (emp.payHistories ?? []).map(ph => ({ ...ph, employee: emp }))
  );
  flattened.sort((a, b) => new Date(b.rateChangeDate) - new Date(a.rateChangeDate));
  return { employees, pagamentos: flattened };
}

export async function patchPayHistory(businessEntityID, rateChangeDate, body) {
  return http(`/payhistory/${businessEntityID}/${rateChangeDate}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" }, 
    body: JSON.stringify(body),
  });
}

export async function deletePayHistory(businessEntityID, rateChangeDate) {
  return http(`/payhistory/${businessEntityID}/${rateChangeDate}`, {
    method: "DELETE",
  });
}

export async function createPayHistory(body) {
  return http(`/payhistory`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
}
