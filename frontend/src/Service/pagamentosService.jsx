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
 

// Service/employeeService.js


// Service/employeeService.js

export async function getEmployeesPaged({
  pageNumber = 1,
  pageSize = 20,
  search = "",
  signal,
} = {}) {
  const token = localStorage.getItem("authToken");

  const url = new URL("http://localhost:5136/api/v1/employee/paged");
  url.searchParams.set("pageNumber", String(Math.max(1, Number(pageNumber) || 1)));
  url.searchParams.set("pageSize", String(Math.max(1, Number(pageSize) || 20)));
  if (typeof search === "string" && search.trim()) {
    url.searchParams.set("search", search.trim());
  }

  const headers = {
    Accept: "application/json",
  };
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  let res;
  try {
    res = await fetch(url.toString(), {
      method: "GET",
      headers,
      signal,
    });
  } catch (err) {
    // Erros de rede/abort
    if (err?.name === "AbortError") throw err;
    throw new Error(`Falha na ligação ao servidor: ${err?.message || "erro de rede"}`);
  }

  if (!res.ok) {
    let serverMsg = "";
    try {
      const errJson = await res.clone().json();
      serverMsg = errJson?.message || errJson?.error || "";
    } catch {
      try {
        const errText = await res.clone().text();
        serverMsg = errText || "";
      } catch {}
    }
    throw new Error(
      `Erro ao obter funcionários (HTTP ${res.status})${serverMsg ? " - " + serverMsg : ""}`
    );
  }

  let data;
  try {
    data = await res.json();
  } catch {
    throw new Error("Resposta do servidor inválida (JSON).");
  }

  // Normalização do PagedResult<EmployeeDto>
  const items = Array.isArray(data?.items) ? data.items : [];
  const totalCount = Number(data?.totalCount ?? 0);
  const respPageNumber = Number(data?.pageNumber ?? pageNumber) || 1;
  const respPageSize = Number(data?.pageSize ?? pageSize) || 20;

  // Usa totalPages do backend se existir; caso contrário, calcula
  const totalPagesFromServer = Number(data?.totalPages);
  const computedTotalPages = Math.max(
    1,
    Math.ceil((isFinite(totalCount) ? totalCount : items.length) / Math.max(1, respPageSize))
  );
  const totalPages = isFinite(totalPagesFromServer) && totalPagesFromServer > 0
    ? totalPagesFromServer
    : computedTotalPages;

  return {
    items,
    totalCount,
    pageNumber: respPageNumber,
    pageSize: respPageSize,
    totalPages,
  };
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


  // ⚠️ Este endpoint devolve EmployeeDto, não devolve { items, meta }
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


  const data = await res.json();

  
  // Extrai histories do DTO (suporta departmentHistories/DepartmentHistories)
  const allPayments = data?.payHistories;

      const totalCount = allPayments.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  // Paginação client-side
  const items = allPayments

  return {
    items,
    meta: { pageNumber, pageSize, totalPages, totalCount },
  };
}

