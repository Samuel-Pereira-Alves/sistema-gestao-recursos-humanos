import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import BackButton from "../../components/Button/BackButton";

function Funcionarios() {
  const navigate = useNavigate();

  const [funcionarios, setFuncionarios] = useState([]);
  const [loading, setLoading] = useState(false);
  const [selectedFuncionario, setSelectedFuncionario] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  const viewProfile = (funcionarioId) => {
    const id = funcionarioId;
    navigate(`/profile?id=${id}`);
  };

  const fetchFuncionarios = async () => {
    try {
      setLoading(true);
      const response = await fetch("http://localhost:5136/api/v1/employee/");
      if (!response.ok) throw new Error("Erro ao carregar funcionários");
      const data = await response.json();
      setFuncionarios(data);
    } catch (error) {
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchFuncionarios();
  }, []);

  const handleEditClick = (funcionario) => {
    setSelectedFuncionario(funcionario);
    setShowModal(true);
  };

  // Eliminar
  const [deleteLoadingId, setDeleteLoadingId] = useState(null);

  const handleDelete = async (p) => {
    const businessEntityID =
      p.businessEntityID ?? p.employee?.businessEntityID ?? "";

    const confirm = window.confirm("Deseja mesmo eliminar este Funcionário?");
    if (!confirm) return;

    const url = `http://localhost:5136/api/v1/employee/${encodeURIComponent(
      businessEntityID
    )}`;

    try {
      setDeleteLoadingId(String(businessEntityID));

      const resp = await fetch(url, {
        method: "DELETE",
        headers: {
          Accept: "application/json",
        },
      });

      await fetchFuncionarios();
    } catch (e) {
      console.error(e);
      alert(e?.message || "Erro ao eliminar colaborador.");
    } finally {
      setDeleteLoadingId(null);
    }
  };

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
      setFuncionarios(
        funcionarios.map((f) =>
          f.id === selectedFuncionario.id ? selectedFuncionario : f
        )
      );
      closeModal();
    } catch (error) {
      console.error(error);
      alert("Erro ao atualizar funcionário");
    }
  };

  const isCurrent = (f) => {
    return f?.currentFlag === true;
  };

  const filteredFuncionarios = funcionarios
    .filter(isCurrent)
    .filter(
      (f) =>
        getNomeCompleto(f).toLowerCase().includes(searchTerm.toLowerCase()) ||
        f.jobTitle.toLowerCase().includes(searchTerm.toLowerCase())
    );

  function getNomeCompleto(f) {
    if (f?.nome) return f.nome;
    const p = f?.person ?? {};
    const partes = [p.firstName, p.middleName, p.lastName].filter(Boolean);
    return partes.join(" ") || "Sem nome";
  }

  function getDepartamentoAtualNome(funcionario) {
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

  // Paginação
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const currentFuncionarios = filteredFuncionarios.slice(
    indexOfFirst,
    indexOfLast
  );
  const totalPages = Math.ceil(filteredFuncionarios.length / itemsPerPage);

  return (
    <>
      <div className="container mt-4">
        <BackButton />

        {/* Header */}
        <div className="mb-4">
          <h1 className="h3 mb-1">Gestão de Funcionários</h1>
        </div>

        {/* Search */}
        <div className="card mb-3 border-0 shadow-sm">
          <div className="card-body">
            <input
              type="text"
              className="form-control"
              placeholder="Procurar funcionários..."
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
              }}
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
                  <table className="table table-hover mb-0">
                    <thead className="table-light">
                      <tr>
                        <th className="px-4 py-3">Nome</th>
                        <th className="px-4 py-3">Cargo</th>
                        <th className="px-4 py-3">Departamento</th>
                        <th className="px-4 py-3 text-center">Ações</th>
                      </tr>
                    </thead>
                    <tbody>
                      {currentFuncionarios.map((f) => {
                        const id = f.businessEntityID ?? f.id; // usa o ID que estiver disponível
                        const isDeleting =
                          String(deleteLoadingId) === String(id);

                        return (
                          <tr key={id}>
                            <td className="px-4 py-3">{getNomeCompleto(f)}</td>
                            <td className="px-4 py-3 text-muted">
                              {f.jobTitle}
                            </td>
                            <td className="px-4 py-3 text-muted">
                              {getDepartamentoAtualNome(f)}
                            </td>

                            <td className="px-4 py-3 text-end">
                              <div className="btn-group btn-group-sm">
                                <button
                                  className="btn btn-sm btn-outline-secondary flex-fill"
                                  onClick={() =>
                                    viewProfile(f.businessEntityID)
                                  }
                                >
                                  Ver Perfil
                                </button>
                                <button
                                  className="btn btn-outline-danger"
                                  onClick={() => handleDelete(f)}
                                >
                                  Eliminar
                                </button>
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>

                {/* Mobile Cards */}
                <div className="d-md-none">
                  {currentFuncionarios.map((f) => (
                    <div key={f.id} className="border-bottom p-3">
                      <h6 className="mb-1">{getNomeCompleto(f)}</h6>
                      <p className="text-muted small mb-1">{f.jobTitle}</p>
                      <p className="text-muted small mb-3">
                        {getDepartamentoAtualNome(f)}
                      </p>
                      <div className="d-flex gap-2">
                        <button
                          className="btn btn-sm btn-outline-secondary flex-fill"
                          onClick={() => viewProfile(f.businessEntityID)}
                        >
                          Ver Perfil
                        </button>

                        <button
                          className="btn btn-outline-danger"
                          onClick={() => handleDelete(f)}
                        >
                          Eliminar
                        </button>
                      </div>
                    </div>
                  ))}
                </div>

                {/* Pagination */}
                <div className="border-top p-3 ">
                  <div className="d-flex justify-content-between align-items-center">
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === 1}
                      onClick={() => setCurrentPage(currentPage - 1)}
                    >
                      ← Anterior
                    </button>
                    <span className="text-muted small">
                      Página {currentPage} de {totalPages}
                    </span>
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === totalPages}
                      onClick={() => setCurrentPage(currentPage + 1)}
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

      {/* Modal */}
      {showModal && selectedFuncionario && (
        <>
          <div
            className="modal fade show d-block"
            tabIndex="-1"
            style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
            onClick={closeModal}
          >
            <div
              className="modal-dialog modal-dialog-centered"
              onClick={(e) => e.stopPropagation()}
            >
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">Editar Funcionário</h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={closeModal}
                  ></button>
                </div>
                <div className="modal-body">
                  <div className="mb-3">
                    <label className="form-label fw-medium">Nome</label>
                    <input
                      disabled
                      type="text"
                      className="form-control"
                      name="nome"
                      value={getNomeCompleto(selectedFuncionario)}
                      readOnly
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label fw-medium">Cargo</label>
                    <input
                      type="text"
                      className="form-control"
                      name="jobTitle"
                      value={selectedFuncionario.jobTitle}
                      onChange={handleChange}
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label fw-medium">Departamento</label>
                    <input
                      type="text"
                      className="form-control"
                      name="departamento"
                      value={getDepartamentoAtualNome(selectedFuncionario)}
                      readOnly
                    />
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={closeModal}
                  >
                    Cancelar
                  </button>
                  <button
                    type="button"
                    className="btn btn-outline-dark"
                    onClick={handleSave}
                  >
                    Guardar Alterações
                  </button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </>
  );
}

export default Funcionarios;
``;
