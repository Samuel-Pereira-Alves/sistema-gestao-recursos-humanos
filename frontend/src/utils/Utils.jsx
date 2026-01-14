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
  const termo = normalizarTexto(term);

  return pagamentos.filter((p) => {
    if (isNumeric) {
      return toIdString(p.employee?.businessEntityID) === raw;
    }
    const first = normalizarTexto(p.employee?.person?.firstName);
    const last  = normalizarTexto(p.employee?.person?.lastName);
    const full  = `${first} ${last}`.trim();

    const freq  = normalizarTexto(freqLabel(p.payFrequency));
    const valor = normalizarTexto(formatCurrencyEUR(p.rate));
    const data  = normalizarTexto(formatDate(p.rateChangeDate));

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