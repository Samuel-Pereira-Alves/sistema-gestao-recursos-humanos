import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import { addNotification } from "../../utils/notificationBus";
import {getNomeCompleto,getDepartamentoAtualNome,isCurrent,} from "../../utils/Utils";
import { deleteEmployee, getEmployees } from "../../Service/employeeService";

function Funcionarios() {
  const navigate = useNavigate();

  const [funcionarios, setFuncionarios] = useState([]);
  const [loading, setLoading] = useState(false);
  const [selectedFuncionario, setSelectedFuncionario] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;

  const viewProfile = (funcionarioId) => {
    const id = funcionarioId;
    navigate(`/profile/${id}`);
  };

  const fetchFuncionarios = async () => {
    try {
      const token = localStorage.getItem("authToken");
      setLoading(true);
      const data = await getEmployees(token)
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

  const [deleteLoadingId, setDeleteLoadingId] = useState(null);

  const handleDelete = async (p) => {
    const businessEntityID = p.businessEntityID ?? p.employee?.businessEntityID ?? "";

    const confirm = window.confirm("Deseja mesmo eliminar este Funcionário?");
    if (!confirm) return;
    const token = localStorage.getItem("authToken");

    try {
      setDeleteLoadingId(String(businessEntityID));
      await deleteEmployee(token, businessEntityID)

      await fetchFuncionarios();

      addNotification(
        `O funcionário ${getNomeCompleto(p)} saiu dos quadros das empresa.`,
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
      setFuncionarios((prev) =>
        prev.map((f) => (f.id === selectedFuncionario.id ? selectedFuncionario : f))
      );
      closeModal();
    } catch (error) {
      console.error(error);
      alert("Erro ao atualizar funcionário");
    }
  };

  const filteredFuncionarios = funcionarios
    .filter(isCurrent)
    .filter(
      (f) =>
        getNomeCompleto(f).toLowerCase().includes(searchTerm.toLowerCase()) ||
        f.jobTitle?.toLowerCase().includes(searchTerm.toLowerCase())
    );

  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const currentFuncionarios = filteredFuncionarios.slice(indexOfFirst, indexOfLast);
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
                        const id = f.businessEntityID ?? f.id;
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
                              <div className="d-flex justify-content-end gap-2">
                                <button
                                  className="btn btn-sm btn-outline-secondary flex-fill"
                                  onClick={() =>
                                    viewProfile(f.businessEntityID)
                                  }
                                >
                                  Ver Perfil
                                </button>
                                <button
                                  disabled={
                                    localStorage.getItem("businessEntityId") ==
                                    id
                                  }
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
                    <div key={f.businessEntityID} className="border-bottom p-3">
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
