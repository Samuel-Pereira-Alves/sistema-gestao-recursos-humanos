
import React, { useEffect, useMemo, useState, useCallback } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import Loading from "../../components/Loading/Loading";
import ReadOnlyField from "../../components/ReadOnlyField/ReadOnlyField";
// import EmployeeDetails from "../../components/EmployeeDetails/EmployeeDetails"; // usa se exibires o header
import { normalize, formatDate } from "../../utils/Utils";
import { getDepHistoriesById } from "../../Service/departmentHistoryService";

export default function DepartmentHistoryList() {
  const [loading, setLoading] = useState(false);
  const [fetchError, setFetchError] = useState(null);

  // Pesquisa e paginação
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;

  // Dados
  const [departamentos, setDepartamentos] = useState([]); // depHistories.items
  const [employee, setEmployee] = useState(null);
  const [totalPages, setTotalPages] = useState(1);

  const id = localStorage.getItem("businessEntityId");
  const token = localStorage.getItem("authToken");

  const load = useCallback(async (page) => {
    if (!id) {
      setFetchError("ID do funcionário não encontrado no localStorage.");
      return;
    }
    setLoading(true);
    setFetchError(null);
    try {
      // 👇 agora o service devolve { employee, depHistories: { items, meta }, payHistories ... }
      const data = await getDepHistoriesById(token, id, {
        pageNumber: page,
        pageSize: itemsPerPage,
      });

      // defesa para formatos diferentes
      const depItems = Array.isArray(data?.depHistories?.items)
        ? data.depHistories.items
        : Array.isArray(data?.items) // fallback antigo
        ? data.items
        : [];

      const depMeta = data?.depHistories?.meta ?? data?.meta ?? {
        pageNumber: page,
        pageSize: itemsPerPage,
        totalCount: depItems.length,
        totalPages: 1,
      };

      setEmployee(data?.employee ?? null);
      setDepartamentos(depItems);
      setTotalPages(Number(depMeta.totalPages || 1));
    } catch (err) {
      console.error(err);
      setFetchError(err.message || "Erro desconhecido ao obter dados.");
      setDepartamentos([]);
      setTotalPages(1);
    } finally {
      setLoading(false);
    }
  }, [id, token]);

  // Carrega ao montar e quando muda a página
  useEffect(() => {
    load(currentPage);
  }, [load, currentPage]);

  // Reset para página 1 ao mudar a pesquisa (pesquisa é local, sobre a página atual)
  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm]);

  // Filtro local sobre os items da página atual
  const termo = normalize(searchTerm);
  const filteredDepartamentos = useMemo(() => {
    const list = Array.isArray(departamentos) ? departamentos : [];
    if (!termo) return list;
    return list.filter((d) => {
      const name = normalize(d?.department?.name || "");
      const group = normalize(d?.department?.groupName || "");
      return name.includes(termo) || group.includes(termo);
    });
  }, [departamentos, termo]);

  return (
    <div className="container mt-4">
      <BackButton />

      <div className="mb-4 d-flex justify-content-between align-items-center">
        <h1 className="h3 mb-3">Histórico de Departamentos</h1>
        {/* podes mostrar info leve do colaborador se quiseres */}
        {/* {employee && <EmployeeDetails employee={employee} />} */}
      </div>

      {/* Search */}
      <div className="card mb-3 border-0 shadow-sm">
        <div className="card-body">
          {loading ? (
            <Loading text="Carregando dados..." />
          ) : (
            <input
              type="text"
              className="form-control"
              placeholder="Procurar por departamento ou grupo..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              aria-label="Pesquisar histórico de departamentos"
            />
          )}
        </div>
      </div>

      {/* Content */}
      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          {fetchError ? (
            <div className="text-center py-5">
              <div className="alert alert-light border text-muted">
                {fetchError}
              </div>
            </div>
          ) : loading ? (
            <Loading text="Carregando histórico..." />
          ) : (
            <>
              {/* Desktop Table */}
              <div className="table-responsive d-none d-md-block">
                <table className="table table-hover mb-0">
                  <thead className="table-light">
                    <tr>
                      <th>Departamento</th>
                      <th>Grupo</th>
                      <th>Data Início</th>
                      <th>Data Fim</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredDepartamentos.length === 0 ? (
                      <tr>
                        <td colSpan={4} className="text-center text-muted">
                          Sem registos
                        </td>
                      </tr>
                    ) : (
                      filteredDepartamentos.map((d) => {
                        // chave mais estável que só o departmentID
                        const key = `${d.businessEntityID ?? ""}|${d.departmentID ?? d.departmentId ?? d.department?.departmentID ?? ""}|${d.shiftID ?? ""}|${d.startDate ?? ""}`;
                        return (
                          <tr key={key}>
                            <td>{d?.department?.name}</td>
                            <td className="text-muted">
                              {d?.department?.groupName || "—"}
                            </td>
                            <td className="text-muted">
                              {formatDate(d?.startDate)}
                            </td>
                            <td className="text-muted">
                              {d?.endDate == null ? "Atual" : formatDate(d?.endDate)}
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>

              {/* Mobile Cards */}
              <div className="d-md-none">
                {filteredDepartamentos.length === 0 ? (
                  <div className="text-center p-3 text-muted">Sem registos</div>
                ) : (
                  filteredDepartamentos.map((d) => {
                    const key = `${d.businessEntityID ?? ""}|${d.departmentID ?? d.departmentId ?? d.department?.departmentID ?? ""}|${d.shiftID ?? ""}|${d.startDate ?? ""}`;
                    return (
                      <div key={key} className="border-bottom p-3">
                        <h6>{d?.department?.name}</h6>
                        {d?.department?.groupName && (
                          <p className="text-muted small">{d.department.groupName}</p>
                        )}
                        <ReadOnlyField label="Início" value={formatDate(d?.startDate)} />
                        <ReadOnlyField
                          label="Fim"
                          value={d?.endDate == null ? "Atual" : formatDate(d?.endDate)}
                        />
                      </div>
                    );
                  })
                )}
              </div>

              {/* Pagination — vem do servidor */}
              <Pagination
                currentPage={currentPage}
                totalPages={totalPages}
                setPage={setCurrentPage}
              />
            </>
          )}
        </div>
      </div>
    </div>
  );
}
