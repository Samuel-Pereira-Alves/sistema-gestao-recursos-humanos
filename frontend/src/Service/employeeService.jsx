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

export async function getEmployees(
  token,
  {
    pageNumber = 1,
    pageSize = 20,
    sortBy,
    sortDir = "asc",
    search,
  } = {}
) {

  const url = new URL(`${API_BASE}/employee/paged`);
  const params = new URLSearchParams();

  params.set("pageNumber", String(pageNumber));
  params.set("pageSize", String(pageSize));

  if (sortBy) params.set("sortBy", sortBy);
  if (sortDir) params.set("sortDir", sortDir);

  if (search && search.trim().length > 0) params.set("search", search.trim());

  url.search = params.toString();

  const res = await fetch(url.toString(), {
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  if (!res.ok) {
    throw new Error(`Erro ao carregar funcionários (HTTP ${res.status})`);
  }

  let metaFromHeader = null;
  const paginationHeader = res.headers.get("X-Pagination");
  if (paginationHeader) {
    metaFromHeader = JSON.parse(paginationHeader);
  }

  const body = await res.json();
  const items = Array.isArray(body?.items) ? body.items : Array.isArray(body) ? body : [];
  const totalCount = Number.isFinite(body?.totalCount) ? body.totalCount : (metaFromHeader?.TotalCount ?? items.length);
  const pageNum = Number.isFinite(body?.pageNumber) ? body.pageNumber : (metaFromHeader?.PageNumber ?? pageNumber);
  const size = Number.isFinite(body?.pageSize) ? body.pageSize : (metaFromHeader?.PageSize ?? pageSize);

  const totalPages =
    Number.isFinite(body?.totalPages) ? body.totalPages
      : Number.isFinite(metaFromHeader?.TotalPages) ? metaFromHeader.TotalPages
        : Math.max(1, Math.ceil((totalCount || 0) / (size || 1)));

  const hasPrevious =
    typeof body?.hasPrevious === "boolean" ? body.hasPrevious
      : typeof metaFromHeader?.HasPrevious === "boolean" ? metaFromHeader.HasPrevious
        : pageNum > 1;

  const hasNext =
    typeof body?.hasNext === "boolean" ? body.hasNext
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

  console.log(res)

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
    // tenta capturar mensagem do servidor
    let serverMsg = "";
    try {
      const errBody = await res.json();
      serverMsg = errBody?.message || errBody?.error || "";
    } catch { /* ignore */ }
    const suffix = serverMsg ? ` - ${serverMsg}` : "";
    throw new Error(`Erro ao obter funcionários (HTTP ${res.status})${suffix}`);
  }

  // 204 No Content → devolve lista vazia
  if (res.status === 204) return [];

  // tenta parse do JSON; como o teu endpoint devolve um array, devolve-o tal e qual
  try {
    const data = await res.json();
    if (Array.isArray(data)) return data;
    // fallback defensivo (caso algum dia mude para wrapper)
    if (Array.isArray(data?.items)) return data.items;
    return [];
  } catch {
    return [];
  }
}
