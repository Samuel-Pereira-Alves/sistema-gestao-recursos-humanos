
import React, { useEffect, useState, useCallback, useRef } from "react";
import { useNavigate } from "react-router-dom";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import { addNotification } from "../../utils/notificationBus";
import { getNomeCompleto, getDepartamentoAtualNome } from "../../utils/Utils";
import { deleteEmployee, getEmployees } from "../../Service/employeeService";

function Funcionarios() {
  const navigate = useNavigate();

  // Lista e metadados vindos do servidor
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
  const itemsPerPage = 5; // vai como pageSize para a API

  // (se precisares de ordenação, liga no backend e envia no service)
  // const [sortBy] = useState("HireDate");
  // const [sortDir] = useState("desc");

  // Debounce: só envia para o servidor após 300ms sem digitar
  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(searchTerm), 300);
    return () => clearTimeout(t);
  }, [searchTerm]);

  // Sempre que a pesquisa (debounced) mudar, volta para a página 1
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
    // setRows((prev) => prev); // opcional: não é necessário

    try {
      const { items, meta } = await getEmployees(token, {
        pageNumber: page,
        pageSize: itemsPerPage,
        search: debouncedSearch, // pesquisa server-side
      });

      setRows(items);
      setMeta(meta);
      return { items, meta };
    } catch (error) {
      if (error?.name !== "AbortError") {
        console.error(error);
        alert(error?.message || "Erro ao carregar funcionários.");
        setRows([]);
        setMeta((m) => ({ ...m, totalCount: 0, totalPages: 1, pageNumber: 1 }));
      }
      return { items: [], meta: { totalPages: 1, pageNumber: page } };
    } finally {
      // 👇 SEMPRE desligar o loading
      setLoading(false);
    }
  },
  [currentPage, itemsPerPage, debouncedSearch]
);


  // Dispara quando página ou pesquisa mudam
  useEffect(() => {
    fetchFuncionarios(currentPage);
  }, [fetchFuncionarios, currentPage]);

  // Ao apagar, recarrega e, se a página ficar vazia, recua 1 página
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
          setCurrentPage(newPage); // o useEffect volta a fazer fetch
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

  // Modal (mantive a tua lógica)
  const [selectedFuncionario, setSelectedFuncionario] = useState(null);
  const [showModal, setShowModal] = useState(false);

  const closeModal = () => {
    setShowModal(false);
    setSelectedFuncionario(null);
  };

  const handleChange = (e) => {
    setSelectedFuncionario({
      ...selectedFuncionario,
      [e.target.name]: e.target.value,
    });
  };

  const handleSave = async () => {
    try {
      setRows((prev) =>
        prev.map((f) => (f.id === selectedFuncionario.id ? selectedFuncionario : f))
      );
      closeModal();
    } catch (error) {
      console.error(error);
      alert("Erro ao atualizar funcionário");
    }
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

        {/* Search */}
        <div className="card mb-3 border-0 shadow-sm">
          <div className="card-body py-3">
            <input
              type="text"
              className="form-control form-control-sm"
              placeholder="Procurar funcionários por nome, email, cargo, ID…"
              value={searchTerm}
              onChange={(e) => {  
                const v = e.target.value;
                setSearchTerm(v); // 👈 só atualiza: debounce + efeitos tratam o resto
              }}
              aria-label="Pesquisar funcionários"
            />
          </div>
        </div>

        {/* Content */}
        <div className="card shadow-sm">
          <div className="card-body p-0">
            {loading ? (
              <div className="text-center py-5">
                <div className="spinner-border text-primary" role="status">
                  <span className="visually-hidden">Carregando...</span>
                </div>
              </div>
            ) : (
              <>
                {/* Desktop Table */}
                <div className="table-responsive d-none d-md-block">
                  <table className="table table-hover mb-0 table-sm">
                    <thead className="table-light">
                      <tr>
                        <th className="px-4 py-3">Nome</th>
                        <th className="px-4 py-3">Cargo</th>
                        <th className="px-4 py-3">Departamento</th>
                        <th className="px-4 py-3 text-center">Ações</th>
                      </tr>
                    </thead>
                    <tbody>
                      {rows.map((f) => {
                        const id = f.businessEntityID ?? f.id;
                        const isDeleting = String(deleteLoadingId) === String(id);

                        return (
                          <tr key={id}>
                            <td className="px-3 py-2">{getNomeCompleto(f)}</td>
                            <td className="px-3 py-2 text-muted">{f.jobTitle}</td>
                            <td className="px-3 py-2 text-muted">
                              {getDepartamentoAtualNome(f)}
                            </td>

                            <td className="px-3 py-2 text-end">
                              <div className="d-flex justify-content-end gap-2">
                                <button
                                  className="btn btn-sm btn-outline-secondary"
                                  onClick={() => viewProfile(f.businessEntityID ?? f.id)}
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
                          <td className="px-3 py-3 text-center text-muted" colSpan={4}>
                            Nenhum funcionário encontrado.
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>

                {/* Mobile Cards */}
                <div className="d-md-none">
                  {rows.map((f) => {
                    const id = f.businessEntityID ?? f.id;
                    return (
                      <div key={id} className="border-bottom p-3">
                        <h6 className="mb-1">{getNomeCompleto(f)}</h6>
                        <p className="text-muted small mb-1">{f.jobTitle}</p>
                        <p className="text-muted small mb-2">
                          {getDepartamentoAtualNome(f)}
                        </p>

                        <div className="d-flex gap-1">
                          <button
                            className="btn btn-sm btn-outline-secondary flex-fill"
                            onClick={() => viewProfile(f.businessEntityID ?? f.id)}
                          >
                            Ver Perfil
                          </button>
                          <button
                            className="btn btn-sm btn-outline-danger"
                            onClick={() => handleDelete(f)}
                            disabled={
                              String(localStorage.getItem("businessEntityId")) ===
                              String(id)
                            }
                          >
                            Eliminar
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>

                {/* Pagination */}
                <Pagination
                  currentPage={currentPage}
                  totalPages={meta.totalPages}
                  setPage={setCurrentPage}
                />
              </>
            )}
          </div>
        </div>
      </div>
    </>
  );
}

export default Funcionarios;
