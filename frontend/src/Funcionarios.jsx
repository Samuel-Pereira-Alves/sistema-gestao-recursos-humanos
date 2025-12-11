import React, { useEffect, useState } from "react";

function Funcionarios() {
  const [funcionarios, setFuncionarios] = useState([]);
  const [loading, setLoading] = useState(false);
  const [selectedFuncionario, setSelectedFuncionario] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  useEffect(() => {
    const fetchFuncionarios = async () => {
      try {
        setLoading(true);
        const response = await fetch("http://localhost:5136/api/v1/employee/");
        if (!response.ok) throw new Error("Erro ao carregar funcionários");
        const data = await response.json();
        setFuncionarios(data);
      } catch (error) {
        console.error(error);
      }finally {
        setLoading(false);
      }
    };
    fetchFuncionarios();
  }, []);

  const handleEditClick = (funcionario) => {
    setSelectedFuncionario(funcionario);
    setShowModal(true);
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
      // Para usar com API real, descomente:
      // const response = await fetch(
      //   `http://localhost:5136/api/v1/employee/${selectedFuncionario.id}`,
      //   {
      //     method: "PUT",
      //     headers: { "Content-Type": "application/json" },
      //     body: JSON.stringify(selectedFuncionario),
      //   }
      // );
      // if (!response.ok) throw new Error("Erro ao atualizar funcionário");
     
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

  const filteredFuncionarios = funcionarios.filter(f =>
   // f.nome.toLowerCase().includes(searchTerm.toLowerCase()) ||
    f.jobTitle.toLowerCase().includes(searchTerm.toLowerCase()) 
  );

  // Paginação
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const currentFuncionarios = filteredFuncionarios.slice(indexOfFirst, indexOfLast);
  const totalPages = Math.ceil(filteredFuncionarios.length / itemsPerPage);

  return (
    <>
      <div className="container mt-4">
       
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
                      {currentFuncionarios.map((f) => (
                        <tr key={f.id}>
                          <td className="px-4 py-3">{f.nome}</td>
                          <td className="px-4 py-3 text-muted">{f.jobTitle}</td>
                          <td className="px-4 py-3 text-muted">{f.departamento}</td>
                          <td className="px-4 py-3 text-center">
                            <button className="btn btn-sm btn-outline-secondary me-2">
                              Ver Perfil
                            </button>
                            <button
                              className="btn btn-sm btn-outline-primary"
                              onClick={() => handleEditClick(f)}
                            >
                              Editar
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Mobile Cards */}
                <div className="d-md-none">
                  {currentFuncionarios.map((f) => (
                    <div key={f.id} className="border-bottom p-3">
                      <h6 className="mb-1">{f.nome}</h6>
                      <p className="text-muted small mb-1">{f.jobTitle}</p>
                      <p className="text-muted small mb-3">{f.departamento}</p>
                      <div className="d-flex gap-2">
                        <button className="btn btn-sm btn-outline-secondary flex-fill">
                          Ver Perfil
                        </button>
                        <button
                          className="btn btn-sm btn-outline-primary flex-fill"
                          onClick={() => handleEditClick(f)}
                        >
                          Editar
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
                      type="text"
                      className="form-control"
                      name="nome"
                      value={selectedFuncionario.nome}
                      onChange={handleChange}
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
                      value={selectedFuncionario.departamento}
                      onChange={handleChange}
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
                    className="btn btn-primary"
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