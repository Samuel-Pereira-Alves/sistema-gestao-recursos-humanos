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
