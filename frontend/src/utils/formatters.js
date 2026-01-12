
export function formatDate(dateStr) {
  if (!dateStr) return "—";
  const d = new Date(dateStr);
  if (isNaN(d)) return "—";
  return d.toLocaleDateString("pt-PT");
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

export function normalizarTexto(t) {
  if (!t) return "";
  return String(t)
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
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

export function toIdString(id) {
  if (id == null) return "";
  return String(id).trim();
}

export function dateInputToIsoMidnight(dateStr) {
  if (!dateStr) return "";
  return `${dateStr}T00:00:00`;
}