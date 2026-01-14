export function getDepartamentoAtualNome(funcionario) {
  const historicos = funcionario?.departmentHistories ?? [];
  if (historicos.length === 0) return "Sem departamento";

  const atual = historicos.find((h) => h.endDate == null);
  const escolhido =
    atual ??
    historicos
      .slice()
      .sort((a, b) => new Date(b.startDate) - new Date(a.startDate))[0];

  return escolhido?.department?.name ?? "Departamento desconhecido";
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