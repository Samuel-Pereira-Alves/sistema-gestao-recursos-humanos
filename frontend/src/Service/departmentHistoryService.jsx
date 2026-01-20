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

  // Endpoint que devolve { employee, depHistories: {items, meta}, payHistories: {items, meta} }
  const url = new URL(`${API_BASE}/v1/employee/${beid}/paged`, window.location?.origin || "http://localhost");
  url.searchParams.set("pageNumber", String(pageNumber));
  url.searchParams.set("pageSize", String(pageSize));

  const res = await fetch(url.toString(), {
    method: "GET",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  if (!res.ok) {
    let serverMsg = "";
    try {
      const err = await res.clone().json();
      serverMsg = err?.message || err?.error || "";
    } catch {}
    throw new Error(`Erro ao obter histories (HTTP ${res.status})${serverMsg ? " - " + serverMsg : ""}`);
  }

  const data = await res.json();

  // ✅ Formato novo suportado pelo teu endpoint
  if (data?.depHistories && data?.payHistories) {
    const depItems = Array.isArray(data.depHistories.items) ? data.depHistories.items : [];
    const payItems = Array.isArray(data.payHistories.items) ? data.payHistories.items : [];

    const depMeta = {
      pageNumber: Number(data.depHistories?.meta?.pageNumber ?? pageNumber),
      pageSize:   Number(data.depHistories?.meta?.pageSize   ?? pageSize),
      totalCount: Number(data.depHistories?.meta?.totalCount ?? depItems.length),
      totalPages: Number(
        data.depHistories?.meta?.totalPages ??
        Math.max(1, Math.ceil((Number(data.depHistories?.meta?.totalCount ?? depItems.length)) / Math.max(1, pageSize)))
      ),
    };

    const payMeta = {
      pageNumber: Number(data.payHistories?.meta?.pageNumber ?? pageNumber),
      pageSize:   Number(data.payHistories?.meta?.pageSize   ?? pageSize),
      totalCount: Number(data.payHistories?.meta?.totalCount ?? payItems.length),
      totalPages: Number(
        data.payHistories?.meta?.totalPages ??
        Math.max(1, Math.ceil((Number(data.payHistories?.meta?.totalCount ?? payItems.length)) / Math.max(1, pageSize)))
      ),
    };

    return {
      employee: data.employee ?? null,
      depHistories: { items: depItems, meta: depMeta },
      payHistories: { items: payItems, meta: payMeta },
      raw: data,
    };
  }

  // 🔁 Fallback (formato antigo): endpoint /employee/{id} que devolvia EmployeeDto com departmentHistories
  // Útil se, por engano, chamares outra rota. Faz paginação local apenas para não rebentar.
  const allDep = Array.isArray(data?.departmentHistories ?? data?.DepartmentHistories)
    ? (data.departmentHistories ?? data.DepartmentHistories)
    : [];

  const sortStart = (h) => new Date(h?.startDate ?? h?.StartDate ?? 0).getTime();
  const depSorted = [...allDep].sort((a, b) => sortStart(b) - sortStart(a));

  const totalCount = depSorted.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const startIdx = (pageNumber - 1) * pageSize;
  const depPageItems = depSorted.slice(startIdx, startIdx + pageSize);

  return {
    employee: data?.employee ?? null,
    depHistories: {
      items: depPageItems,
      meta: { pageNumber, pageSize, totalCount, totalPages },
    },
    payHistories: {
      items: [], // não disponível neste formato
      meta: { pageNumber, pageSize, totalCount: 0, totalPages: 1 },
    },
    raw: data,
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
