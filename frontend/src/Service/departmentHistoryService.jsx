const API_BASE = "http://localhost:5136/api";

export async function deleteDepHistory(
  token,
  beid,
  depId,
  shId,
  formattedDate,
) {
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

  const res = await fetch(`${API_BASE}/v1/departmenthistory`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/problem+json, application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    let msg = `Erro ao criar (HTTP ${res.status})`;

    try {
      const err = await res.clone().json();
      if (err?.detail || err?.title || err?.message) {
        msg = err.detail || err.title || err.message;
      } else if (err?.errors) {
        const flat = Object.values(err.errors).flat().join("\n");
        msg = flat || msg;
      }
    } catch {
      const txt = await res.text().catch(() => "");
      if (txt) msg = txt;
    }

    throw new Error(msg);
  }

  return res.status === 204 ? null : res.json().catch(() => null);
}

export async function patchDepartmentHistory(
  businessEntityID,
  departmentID,
  shiftID,
  startDate,
  body,
) {
  const token = localStorage.getItem("authToken");
  if (!token) throw new Error("Token ausente. Faça login novamente.");

  const url = `${API_BASE}/v1/departmenthistory/${encodeURIComponent(
    businessEntityID,
  )}/${encodeURIComponent(departmentID)}/${encodeURIComponent(
    shiftID,
  )}/${encodeURIComponent(startDate)}`;

  const res = await fetch(url, {
    method: "PATCH",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/problem+json, application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    let msg = `Erro ao atualizar (HTTP ${res.status})`;

    try {
      const error = await res.json();
      msg = error.detail || error.title || error.message || msg;
    } catch {
      const text = await res.text().catch(() => "");
      if (text) msg = text;
    }

    throw new Error(msg);
  }

  return res.json().catch(() => null);
}

export async function getDepHistoriesById(token, id, opts = {}) {
  const pageNumber = Number.isFinite(opts.pageNumber)
    ? Math.max(1, opts.pageNumber)
    : 1;
  const pageSize = Number.isFinite(opts.pageSize)
    ? Math.max(1, opts.pageSize)
    : 10;
  const q = typeof opts.q === "string" ? opts.q.trim() : "";

  if (!token) throw new Error("Token em falta.");
  const beid = Number(id);
  if (!Number.isFinite(beid)) throw new Error("ID inválido.");

  const url = new URL(
    `${API_BASE}/v1/employee/${beid}/paged`,
    window.location?.origin || "http://localhost",
  );
  url.searchParams.set("pageNumber", String(pageNumber));
  url.searchParams.set("pageSize", String(pageSize));
  if (q) url.searchParams.set("q", q);

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
    } catch { }
    throw new Error(
      `Erro ao obter histories (HTTP ${res.status})${serverMsg ? " - " + serverMsg : ""}`,
    );
  }

  const data = await res.json();

  return {
    employee: data.employee ?? null,
    depHistories: {
      items: Array.isArray(data?.depHistories?.items)
        ? data.depHistories.items
        : [],
      meta: {
        pageNumber: Number(data?.depHistories?.meta?.pageNumber ?? pageNumber),
        pageSize: Number(data?.depHistories?.meta?.pageSize ?? pageSize),
        totalCount: Number(data?.depHistories?.meta?.totalCount ?? 0),
        totalPages: Number(data?.depHistories?.meta?.totalPages ?? 1),
      },
    },
    payHistories: {
      items: Array.isArray(data?.payHistories?.items)
        ? data.payHistories.items
        : [],
      meta: {
        pageNumber: Number(data?.payHistories?.meta?.pageNumber ?? pageNumber),
        pageSize: Number(data?.payHistories?.meta?.pageSize ?? pageSize),
        totalCount: Number(data?.payHistories?.meta?.totalCount ?? 0),
        totalPages: Number(data?.payHistories?.meta?.totalPages ?? 1),
      },
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
    } catch {
    }
    const suffix = serverMsg ? ` - ${serverMsg}` : "";
    throw new Error(
      `Erro ao obter colaboradores (HTTP ${res.status})${suffix}`,
    );
  }

  if (res.status === 204) {
    return [];
  }

  let employees = [];
  try {
    const data = await res.json();
    employees = Array.isArray(data)
      ? data
      : Array.isArray(data?.items)
        ? data.items
        : [];
  } catch {
    employees = [];
  }

  const map = new Map();

  for (const emp of employees) {
    const histories =
      emp?.departmentHistories ?? emp?.DepartmentHistories ?? [];
    for (const h of histories) {
      const depObj = h?.department ?? {};
      const departmentID = Number(
        h?.departmentId ??
        h?.departmentID ??
        depObj?.departmentID ??
        depObj?.departmentId,
      );

      if (!Number.isFinite(departmentID)) continue;

      const name = String(depObj?.name ?? "").trim() || "—";
      const groupName = String(depObj?.groupName ?? "").trim() || "—";

      if (!map.has(departmentID)) {
        map.set(departmentID, { departmentID, name, groupName });
      }
    }
  }

  const departments = Array.from(map.values()).sort((a, b) =>
    a.name.localeCompare(b.name, "pt", { sensitivity: "base" }),
  );

  return departments;
}

export async function getAllDepartments({
  pageNumber = 1,
  pageSize = 5,
  query = ""
} = {}) {
  const token = localStorage.getItem("authToken");

  const url = new URL(
    "http://localhost:5136/api/v1/departmentHistory/departments/paged",
  );

  const qs = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });

  const q = (query ?? "").toString().trim();
  if (q) qs.set("search", q);
  url.search = qs.toString();

  const res = await fetch(url.toString(), {
    method: "GET",
    headers: {
      Accept: "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    }
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    console.error("[getAllDepartments] HTTP", res.status, text);
    throw new Error(text || `Falha ao carregar (${res.status})`);
  }

  const raw = await res.json();

  const normalized = {
    items: raw.items ?? raw.Items ?? [],
    totalCount: raw.totalCount ?? raw.TotalCount ?? 0,
    pageNumber: raw.pageNumber ?? raw.PageNumber ?? pageNumber,
    pageSize: raw.pageSize ?? raw.PageSize ?? pageSize,
    totalPages:
      raw.totalPages ??
      raw.TotalPages ??
      Math.max(
        1,
        Math.ceil(
          (raw.totalCount ?? raw.TotalCount ?? 0) /
          (raw.pageSize ?? raw.PageSize ?? pageSize),
        ),
      ),
    meta: raw.meta ?? raw.Meta ?? null,
  };

  return normalized;
}