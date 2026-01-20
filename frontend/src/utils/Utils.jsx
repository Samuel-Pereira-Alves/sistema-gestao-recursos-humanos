export function getDepartamentoAtualNome(funcionario) {
  const historicos = funcionario?.departmentHistories ?? [];
  if (historicos.length === 0) return "Sem departamento";

  const hoje = new Date();
  const validos = historicos.filter(h => new Date(h.startDate) <= hoje);

  if (validos.length === 0) return "Sem departamento";

  const atual = validos.find(h =>
    (h.endDate == null || new Date(h.endDate) >= hoje)
  );

  if (atual) {
    return atual.department?.name ?? "Departamento desconhecido";
  }

  const ultimo = validos
    .slice()
    .sort((a, b) => new Date(b.startDate) - new Date(a.startDate))[0];

  return ultimo?.department?.name ?? "Departamento desconhecido";
}

export function formatDate(date) {
  return date ? new Date(date).toLocaleDateString("pt-PT") : "N/A";
}

export function getEmployeeId(routeId) {
   return routeId;
}

export function filterPagamentos(pagamentos, term) {
  const raw = (term ?? "").trim();
  if (!raw) return pagamentos;

  const isNumeric = /^\d+$/.test(raw);
  const termo = normalize(term);

  return pagamentos.filter((p) => {
    if (isNumeric) {
      return idToString(p.employee?.businessEntityID) === raw;
    }
    const first = normalize(p.employee?.person?.firstName);
    const last  = normalize(p.employee?.person?.lastName);
    const full  = `${first} ${last}`.trim();

    const freq  = normalize(freqLabel(p.payFrequency));
    const valor = normalize(formatCurrencyEUR(p.rate));
    const data  = normalize(formatDate(p.rateChangeDate));

    return (
      (full && full.includes(termo)) ||
      (freq && freq.includes(termo)) ||
      (valor && valor.includes(termo)) ||
      (data && data.includes(termo))
    );
  });
}

export function paginate(items, page, perPage) {
  const totalPages = Math.max(1, Math.ceil(items.length / perPage));
  const currentPage = Math.min(Math.max(1, page), totalPages);
  const start = (currentPage - 1) * perPage;
  const slice = items.slice(start, start + perPage);
  return { slice, totalPages, currentPage };
}

export function getNomeCompleto(f) {
  if (f?.nome) return f.nome;
  const p = f?.person ?? {};
  const partes = [p.firstName, p.middleName, p.lastName].filter(Boolean);
  return partes.join(" ") || "Sem nome";
}

export function isCurrent(f) {
  return f?.currentFlag === true;
}

export function mapPayHistories(payHistories) {
  return (payHistories ?? [])
    .slice()
    .sort((a, b) => new Date(b.rateChangeDate) - new Date(a.rateChangeDate))
    .map((p, idx) => ({
      key: `${p.payHistoryId ?? idx}-${p.employeeId}-${p.rateChangeDate ?? idx}`,
      payHistoryId: p.payHistoryId ?? null,
      employeeId: p.employeeId ?? null,
      rateChangeDate: p.rateChangeDate ?? null,
      rate: p.rate ?? null,
      payFrequency: p.payFrequency ?? null,
    }));
}

export function mapDepartmentHistories(list) {
  return (list ?? [])
    .slice()
    .sort((a, b) => new Date(b.startDate) - new Date(a.startDate))
    .map((h, idx) => ({
      key: `${h.departmentId}-${h.startDate}-${idx}`,
      departmentId: h.departmentId,
      name: h.department?.name ?? `ID ${h.departmentId}`,
      groupName: h.department?.groupName ?? "",
      startDate: h.startDate,
      endDate: h.endDate,
    }));
}


export function formatCurrencyEUR(value) {
  if (value == null) return "—";
  const n = Number(value);
  if (Number.isNaN(n)) return "—";
  return n.toLocaleString("pt-PT", { style: "currency", currency: "EUR" });
}

export function freqLabel(code) {
  switch (Number(code)) {
    case 1:
      return "Mensal";
    case 2:
      return "Quinzenal";
    default:
      return code != null ? `Código ${code}` : "—";
  }
}

export function normalize(t) {
  if (!t) return "";
  return String(t)
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}

export function idToString(id) {
  if (id == null) return "";
  return String(id).trim();
}

export function dateInputToIsoMidnight(dateStr) {
  if (!dateStr) return "";
  return `${dateStr}T00:00:00`;
}

// Utils.jsx
export const getBusinessEntityID = (h) => {
  return (
    h?.businessEntityID ??
    h?.BusinessEntityID ??
    h?.employee?.businessEntityID ??
    ""
  );
};


export const getDepartmentID = (h) =>{
  return h.departmentId
}

export const getShiftID = (h) => {
  return h?.shiftID
}
export const getStartDate = (h) => h?.startDate ?? h?.StartDate ?? "";
export const getEndDate = (h) => h?.endDate ?? h?.EndDate ?? "";
export const getDepartmentName = (h) =>
  h?.department?.name ??
  h?.department?.Name ??
  h?.departmentName ??
  h?.DepartmentName ??
  "—";

export const getGroupName = (h) =>
  h?.dep.groupName;


  export function formatDateForRoute(input) {
    const d = (input instanceof Date) ? input : new Date(input);

    if (!(d instanceof Date) || Number.isNaN(d.getTime())) {
      throw new Error("StartDate inválida. Use uma data existente (ex.: 2020-02-29).");
    }

    const pad = (n) => String(n).padStart(2, "0");

    const year = d.getFullYear();
    const month = pad(d.getMonth() + 1);
    const day = pad(d.getDate());
    const hours = pad(d.getHours());
    const minutes = pad(d.getMinutes());
    const seconds = pad(d.getSeconds());

    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
  }

  export function buildDerivedDepartments(list) {
      const map = new Map();
      list.forEach((h) => {
        const id = getDepartmentID(h);
        const name = getDepartmentName(h);
        if (id) {
          const key = String(id);
          if (!map.has(key)) {
            map.set(key, { departmentID: key, name });
          } else {
            const existing = map.get(key);
            if (
              (!existing.name || existing.name === "—") &&
              name &&
              name !== "—"
            ) {
              map.set(key, { departmentID: key, name });
            }
          }
        }
      });
      const derived = Array.from(map.values());
      derived.sort((a, b) =>
        String(a.name).localeCompare(String(b.name), "pt-PT", {
          sensitivity: "base",
        })
      );
      return derived;
    }

    export function normalizeApiError(err) {
  // Erros DataAnnotations
  if (err.body?.errors) {
    return Object.values(err.body.errors).flat().join("\n");
  }
 
  // ProblemDetails do backend (Conflict, 500, etc.)
  if (err.body?.detail) {
    return err.body.detail;
  }
 
  // erros front end ( Campos obrigatórios, etc. )
  if (err.message) {
    return err.message;
  }
 
  // 4. fallback
  return "Ocorreu um erro inesperado.";
}