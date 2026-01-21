const API_BASE = "http://localhost:5136/api/v1";

export async function getDepartments(token) {
  const res = await fetch(`${API_BASE}/departmenthistory`, {
    headers: { Accept: "application/json", Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Erro ao carregar departamentos");

  const data = await res.json();

  const uniqueDepartments = Array.from(
    new Map(
      data.map(entry => {
        const dep = entry.department;
        return [dep.departmentID, dep];
      })
    ).values()
  );

  return uniqueDepartments;
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

export async function getEmployees(
  token,
  {
    pageNumber = 1,
    pageSize = 20,
    search
  } = {}
) {
  if (!token) throw new Error("Token em falta.");

  const pn = Number.isFinite(pageNumber) && pageNumber > 0 ? pageNumber : 1;
  const ps = Number.isFinite(pageSize) && pageSize > 0 ? pageSize : 20;
  const url = (() => {
      const u = new URL(`${API_BASE}/employee/paged`, origin);
      u.searchParams.set("pageNumber", String(pn));
      u.searchParams.set("pageSize", String(ps));
      if (search && search.trim()) u.searchParams.set("search", search.trim());
      return u.toString();
  })();

  const res = await fetch(url, {
    method: "GET",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  let metaFromHeader = null;
  try {
    const paginationHeader = res.headers.get("X-Pagination");
    if (paginationHeader) metaFromHeader = JSON.parse(paginationHeader);
  } catch { /* ignore */ }

  let body = null;
  try {
    body = await res.json();
  } catch {
    body = null;
  }

  const items =
    Array.isArray(body?.items) ? body.items
    : Array.isArray(body?.Items) ? body.Items
    : Array.isArray(body) ? body
    : [];

  const totalCount =
    Number.isFinite(body?.totalCount) ? body.totalCount
    : Number.isFinite(body?.TotalCount) ? body.TotalCount
    : Number.isFinite(metaFromHeader?.TotalCount) ? metaFromHeader.TotalCount
    : items.length;

  const pageNum =
    Number.isFinite(body?.pageNumber) ? body.pageNumber
    : Number.isFinite(body?.PageNumber) ? body.PageNumber
    : Number.isFinite(metaFromHeader?.PageNumber) ? metaFromHeader.PageNumber
    : pn;

  const size =
    Number.isFinite(body?.pageSize) ? body.pageSize
    : Number.isFinite(body?.PageSize) ? body.PageSize
    : Number.isFinite(metaFromHeader?.PageSize) ? metaFromHeader.PageSize
    : ps;

  const totalPages =
    Number.isFinite(body?.totalPages) ? body.totalPages
    : Number.isFinite(body?.TotalPages) ? body.TotalPages
    : Number.isFinite(metaFromHeader?.TotalPages) ? metaFromHeader.TotalPages
    : Math.max(1, Math.ceil((totalCount || 0) / (size || 1)));

  const hasPrevious =
    typeof body?.hasPrevious === "boolean" ? body.hasPrevious
    : typeof body?.HasPrevious === "boolean" ? body.HasPrevious
    : typeof metaFromHeader?.HasPrevious === "boolean" ? metaFromHeader.HasPrevious
    : pageNum > 1;

  const hasNext =
    typeof body?.hasNext === "boolean" ? body.hasNext
    : typeof body?.HasNext === "boolean" ? body.HasNext
    : typeof metaFromHeader?.HasNext === "boolean" ? metaFromHeader.HasNext
    : pageNum < totalPages;

  return {
    items,
    meta: {
      totalCount,
      pageNumber: pageNum,
      pageSize: size,
      totalPages,
      hasPrevious,
      hasNext,
    },
    raw: body,
  };
}


export async function deleteEmployee(token, businessEntityID) {
  const res = await fetch(`${API_BASE}/employee/${encodeURIComponent(String(businessEntityID))}`, {
    method: "DELETE",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  if (!res.ok) {
    throw new Error(`Erro ao eliminar funcionário (HTTP ${res.status})`);
  }

  if (res.status === 204) return true;
  try {
    await res.json();
  } catch { }
  return true;
}

export async function getAllEmployees(token) {
  const res = await fetch(`${API_BASE}/employee/`, {
    method: "GET",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  if (!res.ok) {
    let serverMsg = "";
    try {
      const errBody = await res.json();
      serverMsg = errBody?.message || errBody?.error || "";
    } catch { }
    const suffix = serverMsg ? ` - ${serverMsg}` : "";
    throw new Error(`Erro ao obter funcionários (HTTP ${res.status})${suffix}`);
  }

  if (res.status === 204) return [];

  try {
    const data = await res.json();
    if (Array.isArray(data)) return data;
    if (Array.isArray(data?.items)) return data.items;
    return [];
  } catch {
    return [];
  }
}
