
import React, { useEffect, useState, useCallback, useRef } from "react";
import { useNavigate } from "react-router-dom";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import { addNotification } from "../../utils/notificationBus";
import { getNomeCompleto, getDepartamentoAtualNome } from "../../utils/Utils";
import { deleteEmployee, getEmployees } from "../../Service/employeeService";
import Loading from "../../components/Loading/Loading";

function Funcionarios() {
  const navigate = useNavigate();

  const [rows, setRows] = useState([]);
  const [meta, setMeta] = useState({
    totalCount: 0,
    pageNumber: 1,
    pageSize: 5,
    totalPages: 1,
    hasPrevious: false,
    hasNext: false,
  });

  const [loading, setLoading] = useState(false);
  const [deleteLoadingId, setDeleteLoadingId] = useState(null);

  // Pesquisa e paginação
  const [searchTerm, setSearchTerm] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5; 

  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(searchTerm), 300);
    return () => clearTimeout(t);
  }, [searchTerm]);

  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch]);

  const viewProfile = (funcionarioId) => {
    navigate(`/profile/${funcionarioId}`);
  };



  const fetchFuncionarios = useCallback(
    async (page = currentPage) => {
      const token = localStorage.getItem("authToken");
      setLoading(true);

      try {
        const { items, meta } = await getEmployees(token, {
          pageNumber: page,
          pageSize: itemsPerPage,
          search: debouncedSearch,
        });

        setRows(items);
        setMeta(meta);
        return { items, meta };
      } catch (error) {
        if (error?.name !== "AbortError") {
          console.error(error);
          setRows([]);
          setMeta((m) => ({ ...m, totalCount: 0, totalPages: 1, pageNumber: 1 }));
        }
        return { items: [], meta: { totalPages: 1, pageNumber: page } };
      } finally {
        setLoading(false);
      }
    },
    [currentPage, itemsPerPage, debouncedSearch]
  );

  useEffect(() => {
    fetchFuncionarios(currentPage);
  }, [fetchFuncionarios, currentPage]);

  const handleDelete = async (f) => {
    const businessEntityID = f.businessEntityID ?? f.employee?.businessEntityID ?? "";
    const ok = window.confirm("Deseja mesmo eliminar este Funcionário?");
    if (!ok) return;

    const token = localStorage.getItem("authToken");

    try {
      setDeleteLoadingId(String(businessEntityID));
      await deleteEmployee(token, businessEntityID);

      const res = await fetchFuncionarios(currentPage);

      if ((res.items?.length ?? 0) === 0 && currentPage > 1) {
        const newPage = Math.max(
          1,
          Math.min(currentPage - 1, res.meta?.totalPages || 1)
        );
        if (newPage !== currentPage) {
          setCurrentPage(newPage);
        }
      }

      addNotification(
        `O funcionário ${getNomeCompleto(f)} saiu dos quadros da empresa.`,
        "admin",
        { type: "EMPLOYEES" }
      );
    } catch (e) {
      console.error(e);
      alert(e?.message || "Erro ao eliminar colaborador.");
    } finally {
      setDeleteLoadingId(null);
    }
  };

  const [selectedFuncionario, setSelectedFuncionario] = useState(null);
  const [showModal, setShowModal] = useState(false);

  const closeModal = () => {
    setShowModal(false);
    setSelectedFuncionario(null);
  };

return (
  <>
    <div className="container mt-4">
      <BackButton />
      {/* Header */}
      <div className="mb-4">
        <h1 className="h4 mb-1">Gestão de Funcionários</h1>
        <small className="text-muted">
          {meta.totalCount} resultado{meta.totalCount === 1 ? "" : "s"}
        </small>
      </div>

      {/* Search (card + spinner alinhado à direita quando loading) */}
      <div className="card mb-3 border-0 shadow-sm">
        <div className="card-body position-relative">
          <input
            type="text"
            className="form-control"
            placeholder="Procurar funcionários por nome…"
            value={searchTerm}
            onChange={(e) => {
              const v = e.target.value;
              setSearchTerm(v); // debounce/efeitos mantidos fora
            }}
            aria-label="Pesquisar funcionários"
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
          {loading && rows.length === 0 ? (
            <div className="py-4">
              <Loading text="Carregando funcionários..." />
            </div>
          ) : (
            <>
              {/* Desktop Table */}
              <div className="table-responsive d-none d-md-block">
                <table className="table table-hover mb-0">
                  <thead className="table-light">
                    <tr>
                      <th>Nome</th>
                      <th>Cargo</th>
                      <th>Departamento</th>
                      <th className="text-end">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rows.map((f) => {
                      const id = f.businessEntityID ?? f.id;
                      const isDeleting =
                        String(deleteLoadingId) === String(id);

                      return (
                        <tr key={id}>
                          <td>{getNomeCompleto(f)}</td>
                          <td className="text-muted">{f.jobTitle}</td>
                          <td className="text-muted">
                            {getDepartamentoAtualNome(f)}
                          </td>

                          <td className="text-end">
                            <div className="d-flex justify-content-end gap-2">
                              <button
                                className="btn btn-sm btn-outline-secondary"
                                onClick={() =>
                                  viewProfile(f.businessEntityID ?? f.id)
                                }
                              >
                                Ver Perfil
                              </button>
                              <button
                                disabled={
                                  String(localStorage.getItem("businessEntityId")) ===
                                    String(id) || isDeleting
                                }
                                className="btn btn-sm btn-outline-danger"
                                onClick={() => handleDelete(f)}
                              >
                                {isDeleting ? "A eliminar..." : "Eliminar"}
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}

                    {rows.length === 0 && (
                      <tr>
                        <td className="text-center text-muted" colSpan={4}>
                          Nenhum funcionário encontrado.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>

              {/* Mobile Cards */}
              <div className="d-md-none">
                {rows.length === 0 ? (
                  <div className="text-center p-3 text-muted">
                    Nenhum funcionário encontrado.
                  </div>
                ) : (
                  rows.map((f) => {
                    const id = f.businessEntityID ?? f.id;
                    const isDeleting =
                      String(deleteLoadingId) === String(id);
                    const isSelf =
                      String(localStorage.getItem("businessEntityId")) ===
                      String(id);

                    return (
                      <div key={id} className="border-bottom p-3">
                        <h6 className="mb-1">{getNomeCompleto(f)}</h6>
                        <p className="text-muted small mb-1">{f.jobTitle}</p>
                        <p className="text-muted small mb-2">
                          {getDepartamentoAtualNome(f)}
                        </p>

                        <div className="d-flex gap-2">
                          <button
                            className="btn btn-sm btn-outline-secondary flex-fill"
                            onClick={() =>
                              viewProfile(f.businessEntityID ?? f.id)
                            }
                          >
                            Ver Perfil
                          </button>
                          <button
                            className="btn btn-sm btn-outline-danger"
                            onClick={() => handleDelete(f)}
                            disabled={isSelf || isDeleting}
                          >
                            {isDeleting ? "A eliminar..." : "Eliminar"}
                          </button>
                        </div>
                      </div>
                    );
                  })
                )}
              </div>

              {/* Pagination */}
              {rows.length > 0 ? (
                <Pagination
                  currentPage={currentPage}
                  totalPages={meta.totalPages}
                  setPage={setCurrentPage}
                />
              ) : (
                <></>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  </>
);
}

export default Funcionarios;
