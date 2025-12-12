
import React, { useEffect, useMemo, useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

/* Utils */
function formatDate(dateStr) {
  if (!dateStr) return "—";
  const d = new Date(dateStr);
  if (isNaN(d)) return "—";
  return d.toLocaleDateString("pt-PT");
}
function normalizarTexto(str) {
  return (str || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}

/**
 * Component: Histórico de Departamentos
 * - Lê o ID do funcionário APENAS do localStorage ("businessEntityId").
 * - Faz fetch a GET /api/v1/employee/:id e extrai departmentHistories.
 * - Pesquisa por nome/ grupo, paginação e UI responsiva em tons cinza.
 */
export default function DepartmentHistoryList() {
  const [loading, setLoading] = useState(false);
  const [fetchError, setFetchError] = useState(null);

  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  const [departamentos, setDepartamentos] = useState([]);
  const [employeeId, setEmployeeId] = useState(null);

  useEffect(() => {
    // Lê o ID do localStorage
    const id = localStorage.getItem("businessEntityId");
    setEmployeeId(id);

    if (!id) {
      setFetchError("ID do funcionário não encontrado no localStorage.");
      return;
    }

    const controller = new AbortController();

    async function load() {
      setLoading(true);
      setFetchError(null);
      try {
        const res = await fetch(`http://localhost:5136/api/v1/employee/${id}`, {
          signal: controller.signal,
        });
        if (!res.ok) throw new Error(`Erro ao carregar funcionário (HTTP ${res.status})`);
        const data = await res.json();

        // Mapeia e ordena os históricos (mais recentes primeiro)
        const list = (data?.departmentHistories ?? [])
          .slice()
          .sort((a, b) => new Date(b.startDate) - new Date(a.startDate))
          .map((h, idx) => ({
            key: `${h.departmentId}-${h.startDate}-${idx}`,
            departmentId: h.departmentId,
            name: h.department?.name || `ID ${h.departmentId}`,
            groupName: h.department?.groupName || "",
            startDate: h.startDate,
            endDate: h.endDate,
          }));

        setDepartamentos(list);
      } catch (err) {
        if (err.name === "AbortError") return;
        console.error(err);
        setFetchError(err.message || "Erro desconhecido ao obter dados.");
      } finally {
        setLoading(false);
      }
    }

    load();
    return () => controller.abort();
  }, []);

  // Reset para a primeira página quando muda o termo de pesquisa
  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm]);

  // Filtro por nome do departamento / grupo (insensível a acentos)
  const termo = normalizarTexto(searchTerm);
  const filteredDepartamentos = useMemo(() => {
    return departamentos.filter((d) => {
      const nome = normalizarTexto(d.name);
      const grupo = normalizarTexto(d.groupName);
      return nome.includes(termo) || grupo.includes(termo);
    });
  }, [departamentos, termo]);

  // Paginação
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const currentDepartamentos = filteredDepartamentos.slice(indexOfFirst, indexOfLast);
  const totalPages = Math.max(1, Math.ceil(filteredDepartamentos.length / itemsPerPage));

  return (
    <>
      <div className="container mt-4">
        {/* Header */}
        <div className="mb-4 d-flex justify-content-between align-items-center">
          <h1 className="h3 mb-0">Histórico de Departamentos</h1>
          <div className="text-muted small">
            {employeeId ? (
              <>
                Funcionário ID:{" "}
                <span className="badge bg-secondary bg-opacity-50 text-dark">{employeeId}</span>
              </>
            ) : (
              <>Sem ID no localStorage</>
            )}
          </div>
        </div>

        {/* Search */}
        <div className="card mb-3 border-0 shadow-sm">
          <div className="card-body">
            {loading ? (
              <div className="text-center py-3">
                <div className="spinner-border text-secondary" role="status">
                  <span className="visually-hidden">Carregando...</span>
                </div>
              </div>
            ) : (
              <input
                type="text"
                className="form-control"
                placeholder="Procurar por departamento ou grupo..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            )}
          </div>
        </div>

        {/* Content */}
        <div className="card border-0 shadow-sm">
          <div className="card-body p-0">
            {fetchError ? (
              <div className="text-center py-5">
                <div className="alert alert-light border text-muted d-inline-block">
                  {fetchError}
                </div>
              </div>
            ) : loading ? (
              <div className="text-center py-5">
                <div className="spinner-border text-secondary" role="status">
                  <span className="visually-hidden">Carregando...</span>
                </div>
              </div>
            ) : (
              <>
                {/* Desktop Table */}
                <div className="table-responsive d-none d-md-block">
                  <table className="table table-hover mb-0">
                    <thead className="table-light">
                      <tr>
                        <th className="px-4 py-3">Departamento</th>
                        <th className="px-4 py-3">Grupo</th>
                        <th className="px-4 py-3">Data Início</th>
                        <th className="px-4 py-3">Data Fim</th>
                      </tr>
                    </thead>
                    <tbody>
                      {currentDepartamentos.length === 0 ? (
                        <tr>
                          <td colSpan={4} className="px-4 py-4 text-center text-muted">
                            Sem registos
                          </td>
                        </tr>
                      ) : (
                        currentDepartamentos.map((d) => (
                          <tr key={d.key}>
                            <td className="px-4 py-3">{d.name}</td>
                            <td className="px-4 py-3 text-muted">{d.groupName || "—"}</td>
                            <td className="px-4 py-3 text-muted">{formatDate(d.startDate)}</td>
                            <td className="px-4 py-3 text-muted">
                              {d.endDate == null ? "Atual" : formatDate(d.endDate)}
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>

                {/* Mobile Cards */}
                <div className="d-md-none">
                  {currentDepartamentos.length === 0 ? (
                    <div className="text-center p-3 text-muted">Sem registos</div>
                  ) : (
                    currentDepartamentos.map((d) => (
                      <div key={d.key} className="border-bottom p-3">
                        <h6 className="mb-1">{d.name}</h6>
                        {d.groupName && (
                          <p className="text-muted small mb-1">{d.groupName}</p>
                        )}
                        <p className="text-muted small mb-1">
                          <strong>Início:</strong> {formatDate(d.startDate)}
                        </p>
                        <p className="text-muted small mb-0">
                          <strong>Fim:</strong> {d.endDate == null ? "Atual" : formatDate(d.endDate)}
                        </p>
                      </div>
                    ))
                  )}
                </div>

                {/* Pagination */}
                <div className="border-top p-3">
                  <div className="d-flex justify-content-between align-items-center">
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === 1}
                      onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                      type="button"
                                       >
                      ← Anterior
                    </button>
                    <span className="text-muted small">
                      Página {currentPage} de {totalPages}
                    </span>
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === totalPages}
                      onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                      type="button"
                    >
                      Próxima →
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </>
  );
} 