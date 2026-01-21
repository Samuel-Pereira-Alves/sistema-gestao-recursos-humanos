
  import React, { useEffect, useMemo, useRef, useState, useCallback } from "react";
  import "bootstrap/dist/css/bootstrap.min.css";
  import BackButton from "../../components/Button/BackButton";
  import Pagination from "../../components/Pagination/Pagination";
  import Loading from "../../components/Loading/Loading";
  import ReadOnlyField from "../../components/ReadOnlyField/ReadOnlyField";
  import { formatDate } from "../../utils/Utils";
  import { getDepHistoriesById } from "../../Service/departmentHistoryService";

  export default function DepartmentHistoryList() {
    const [loading, setLoading] = useState(false);
    const [fetchError, setFetchError] = useState(null);

    // Pesquisa e paginação
    const [searchTerm, setSearchTerm] = useState("");
    const [debouncedTerm, setDebouncedTerm] = useState("");
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 5;

    // Dados
    const [departamentos, setDepartamentos] = useState([]);
    const [totalPages, setTotalPages] = useState(1);

    const id = localStorage.getItem("businessEntityId");
    const token = localStorage.getItem("authToken");

    const lastTermRef = useRef("");

    // Debounce 350ms
    useEffect(() => {
      const t = setTimeout(() => setDebouncedTerm(searchTerm.trim()), 350);
      return () => clearTimeout(t);
    }, [searchTerm]);

    const load = useCallback(
      async (page, term) => {
        if (!id) {
          setFetchError("ID do funcionário não encontrado no localStorage.");
          setDepartamentos([]);
          setTotalPages(1);
          return;
        }


        setLoading(true);
        setFetchError(null);
        try {
          const data = await getDepHistoriesById(token, id, {
            pageNumber: page,
            pageSize: itemsPerPage,
            q: term || "",
          });


          const depItems = Array.isArray(data?.depHistories?.items)
            ? data.depHistories.items
            : Array.isArray(data?.items)
              ? data.items
              : [];

          const depMeta = data?.depHistories?.meta ?? data?.meta ?? {
            pageNumber: page,
            pageSize: itemsPerPage,
            totalCount: depItems.length,
            totalPages: 1,
          };

          const newTotalPages = Math.max(1, Number(depMeta.totalPages || 1));

          if (page > newTotalPages) {
            setTotalPages(newTotalPages);
            setCurrentPage(1);
            return;
          }

          setDepartamentos(depItems);
          setTotalPages(newTotalPages);
        } catch (err) {
          if (err?.name === "AbortError") return;

          console.error(err);
          //setFetchError(err?.message || "Erro desconhecido ao obter dados.");
          setDepartamentos([]);
          setTotalPages(1);
        } finally {
          setLoading(false);
        }
      },
      [id, token]
    );

    useEffect(() => {
      const termChanged = debouncedTerm !== lastTermRef.current;
      if (termChanged) {
        lastTermRef.current = debouncedTerm;
        if (currentPage !== 1) {
          setCurrentPage(1);
          return;
        }
      }
      load(currentPage, debouncedTerm);
    }, [currentPage, debouncedTerm, load]);

    return (
      <div className="container mt-4">
        <BackButton />

        <div className="mb-4 d-flex justify-content-between align-items-center">
          <h1 className="h3 mb-3">Histórico de Departamentos</h1>
        </div>

        <div className="card mb-3 border-0 shadow-sm">
          <div className="card-body position-relative">
            <input
              type="text"
              className="form-control"
              placeholder="Procurar por departamento ou grupo..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              aria-label="Pesquisar histórico de departamentos"
            />
            {loading && (
              <div
                className="position-absolute top-50 end-0 translate-middle-y me-3 text-muted small"
                aria-hidden="true"
              >
                <span className="spinner-border spinner-border-sm" /> A carregar...
              </div>
            )}
          </div>
        </div>

        {/* Content */}
        <div className="card border-0 shadow-sm">
          <div className="card-body p-0">
            {fetchError ? (
              <div className="text-center py-5">
                <div className="alert alert-light border text-muted">{fetchError}</div>
              </div>
            ) : loading && departamentos.length === 0 ? (
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
                      {departamentos.length === 0 ? (
                        <tr>
                          <td colSpan={4} className="text-center text-muted">
                            Registos não encontrados.
                          </td>
                        </tr>
                      ) : (
                        departamentos.map((d) => {
                          const key = `${d.businessEntityID ?? ""}|${d.departmentID ?? d.departmentId ?? d.department?.departmentID ?? ""}|${d.shiftID ?? ""}|${d.startDate ?? ""}`;
                          return (
                            <tr key={key}>
                              <td>{d?.department?.name}</td>
                              <td className="text-muted">
                                {d?.department?.groupName || "—"}
                              </td>
                              <td className="text-muted">{formatDate(d?.startDate)}</td>
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
                  {departamentos.length === 0 ? (
                    <div className="text-center p-3 text-muted">Registos não encontrados.</div>
                  ) : (
                    departamentos.map((d) => {
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

                {/* Pagination */}
                {departamentos.length > 0 ? (
                  <Pagination
                    currentPage={currentPage}
                    totalPages={totalPages}
                    setPage={setCurrentPage}
                  />
                ) : (<> </>)
                }
              </>
            )}
          </div>
        </div>
      </div>
    );
  }
