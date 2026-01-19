const API_BASE = "http://localhost:5136/api";

export async function deleteDepHistory(token, beid, depId, shId, formattedDate) {
  const url = `${API_BASE}/v1/departmenthistory/${encodeURIComponent(beid)}/${encodeURIComponent(depId)}/${encodeURIComponent(shId)}/${encodeURIComponent(formattedDate)}`;
    await fetch(url, {
      method: "DELETE",
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
      },
    });
}

export async function createDepartmentHistory(body) {
  const token = localStorage.getItem("authToken");
  if (!token) throw new Error("Token ausente. Faça login novamente.");

  await fetch(`${API_BASE}/v1/departmenthistory`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(body),
  });

}

export async function patchDepartmentHistory(businessEntityID, departmentID, shiftID, startDate , body) {
  const token = localStorage.getItem("authToken");
  if (!token) throw new Error("Token ausente. Faça login novamente.");

  const url = `${API_BASE}/v1/departmenthistory/${encodeURIComponent(businessEntityID)}/${encodeURIComponent(departmentID)}/${encodeURIComponent(shiftID)}/${encodeURIComponent(startDate)}`;

 await fetch(url, {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(body),
    });

}





export async function getDepHistoriesById(token, id, opts = {}) {
  const pageNumber = Number.isFinite(opts.pageNumber) ? Math.max(1, opts.pageNumber) : 1;
  const pageSize   = Number.isFinite(opts.pageSize)   ? Math.max(1, opts.pageSize)   : 10;

  if (!token) throw new Error("Token em falta.");
  const beid = Number(id);
  if (!Number.isFinite(beid)) throw new Error("ID inválido.");

  // ⚠️ Este endpoint devolve EmployeeDto, não devolve { items, meta }
  // Não concatena "&amp;", usa searchParams.
  const url = new URL(`${API_BASE}/v1/employee/${beid}`, window.location?.origin || "http://localhost");
  // Estes params são ignorados pelo servidor neste endpoint, mas não fazem mal.
  url.searchParams.set("pageNumber", String(pageNumber));
  url.searchParams.set("pageSize", String(pageSize));

  const res = await fetch(url.toString(), {
    method: "GET",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  if (res.status === 204) {
    return { items: [], meta: { pageNumber, pageSize, totalCount: 0, totalPages: 1 } };
  }

  if (!res.ok) {
    const errBody = await res.clone().json().catch(() => null);
    const serverMsg = errBody?.message || errBody?.error || "";
    throw new Error(`Erro ao obter Employee (HTTP ${res.status})${serverMsg ? " - " + serverMsg : ""}`);
  }

  const employee = await res.json();

  // Extrai histories do DTO (suporta departmentHistories/DepartmentHistories)
  const allHistories = Array.isArray(employee?.departmentHistories ?? employee?.DepartmentHistories)
    ? (employee.departmentHistories ?? employee.DepartmentHistories)
    : [];

  // Ordena por startDate DESC (suporta casing startDate/StartDate)
  const getStart = (h) => new Date(h?.startDate ?? h?.StartDate ?? 0).getTime();
  const sorted = [...allHistories].sort((a, b) => getStart(b) - getStart(a));

  // Paginação client-side
  const totalCount = sorted.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const startIdx = (pageNumber - 1) * pageSize;
  const items = sorted.slice(startIdx, startIdx + pageSize);

  return {
    items,
    meta: { pageNumber, pageSize, totalCount, totalPages },
  };
}


export async function getAllDepartmentsFromEmployees(token) {
  const res = await fetch(`${API_BASE}/v1/employee/`, {
    method: "GET",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  if (!res.ok) {
    let serverMsg = "";
    try {
      const err = await res.json();
      serverMsg = err?.message || err?.error || "";
    } catch { /* ignore */ }
    const suffix = serverMsg ? ` - ${serverMsg}` : "";
    throw new Error(`Erro ao obter colaboradores (HTTP ${res.status})${suffix}`);
  }

  if (res.status === 204) {
    return [];
  }

  /** @type {any[]} */
  let employees = [];
  try {
    const data = await res.json();
    employees = Array.isArray(data) ? data : (Array.isArray(data?.items) ? data.items : []);
  } catch {
    employees = [];
  }

  // Deduplicação por departmentID
  const map = new Map(); // key: departmentID, value: { departmentID, name, groupName }

  for (const emp of employees) {
    const histories = emp?.departmentHistories ?? emp?.DepartmentHistories ?? [];
    for (const h of histories) {
      // Normaliza campos (alguns backends usam departmentId vs departmentID)
      const depObj = h?.department ?? {};
      const departmentID =
        Number(h?.departmentId ?? h?.departmentID ?? depObj?.departmentID ?? depObj?.departmentId);

      if (!Number.isFinite(departmentID)) continue;

      const name = String(depObj?.name ?? "").trim() || "—";
      const groupName = String(depObj?.groupName ?? "").trim() || "—";

      if (!map.has(departmentID)) {
        map.set(departmentID, { departmentID, name, groupName });
      }
    }
  }

  // Ordena por nome do departamento
  const departments = Array.from(map.values()).sort((a, b) =>
    a.name.localeCompare(b.name, "pt", { sensitivity: "base" })
  );

  return departments;
}
