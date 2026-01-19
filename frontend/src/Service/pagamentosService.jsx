const API = "http://localhost:5136/api/v1";

function authHeaders() {
  const token = localStorage.getItem("authToken");
  return {
    Accept: "application/json",
    Authorization: token ? `Bearer ${token}` : undefined,
  };
}

function isJsonContentType(ct) {
  const v = (ct || "").toLowerCase();
  return (
    v.includes("application/json") || v.includes("application/problem+json")
  );
}
 
async function http(path, init = {}) {
  const headers = { ...authHeaders(), ...(init.headers || {}) };
  const res = await fetch(`${API}${path}`, { ...init, headers });
 
  const ct = res.headers.get("content-type") || "";
  let body;
 
  try {
    if (isJsonContentType(ct)) {
      body = await res.json();
    } else {
      body = await res.text();
    }
  } catch {
    // fallback defensivo
    body = await res.text();
  }
 
  // Se por alguma razão vier JSON como string, tenta converter
  if (typeof body === "string") {
    try {
      body = JSON.parse(body);
    } catch {
      /* fica como texto */
    }
  }
 
  if (!res.ok) {
    // Erro coerente em TODO o app
    throw { status: res.status, body };
  }
 
  return body;
}
 


// Service/pagamentosService.js
export async function listPagamentosFlattened(pageNumber = 1, pageSize = 10) {
  const token = localStorage.getItem("authToken");

  // constrói a URL de forma segura
  const url = new URL("http://localhost:5136/api/v1/employee/paged");
  url.searchParams.set("pageNumber", String(pageNumber));
  url.searchParams.set("pageSize", String(pageSize));

  const res = await fetch(url.toString(), {
    method: "GET",
    headers: {
      "Accept": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  const data = await res.json();

  const employees = Array.isArray(data) ? data : (data?.items ?? []);

  const flattened = employees.flatMap(emp =>
    (emp.payHistories ?? []).map(ph => ({ ...ph, employee: emp }))
  );

  flattened.sort((a, b) => new Date(b.rateChangeDate) - new Date(a.rateChangeDate));

  const meta = {
    totalCount: data?.totalCount ?? employees.length,
    pageNumber: data?.pageNumber ?? pageNumber,
    pageSize: data?.pageSize ?? pageSize,
    totalPages:
      data?.totalPages ??
      Math.ceil((data?.totalCount ?? employees.length) / (Number(pageSize) || 1)),
  };

  return { employees, pagamentos: flattened, meta };
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

export async function getPayHistoryById(token, id, opts = {}) {
  const pageNumber = Number.isFinite(opts.pageNumber) ? Math.max(1, opts.pageNumber) : 1;
  const pageSize   = Number.isFinite(opts.pageSize)   ? Math.max(1, opts.pageSize)   : 10;

  // Não concatena "&amp;", usa searchParams.
  const url = new URL(`http://localhost:5136/api/v1/employee/${id}/paged`, window.location?.origin || "http://localhost");
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
  const allPayments = Array.isArray(employee?.payHistories ?? employee?.payHistories)
    ? (employee.payHistories ?? employee.payHistories)
    : [];

      const totalCount = allPayments.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));


  // Paginação client-side
  const items = allPayments

  return {
    items,
    meta: { pageNumber, pageSize, totalPages, totalCount },
  };
}

