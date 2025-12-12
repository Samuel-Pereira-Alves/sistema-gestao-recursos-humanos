
import React, { useEffect, useMemo, useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

// Badge por estado
function EstadoBadge({ estado }) {
  const normalized = (estado || "").toLowerCase();
  const classes =
    normalized === "aprovado"
      ? "badge bg-success"
      : normalized === "rejeitado"
      ? "badge bg-danger"
      : "badge bg-warning text-dark"; // pendente
  return <span className={classes}>{estado || "Pendente"}</span>;
}

export default function Candidatos() {
  const [isLoading, setIsLoading] = useState(false);
  const [candidatos, setCandidatos] = useState([
    {
      id: 1,
      nome: "João Silva",
      vaga: "Dev .NET",
      estado: "Pendente",
      email: "joao.silva@email.com",
      telefone: "912345678",
      cv: "/cvs/joao-silva.pdf",
    },
    {
      id: 2,
      nome: "Maria Costa",
      vaga: "Frontend React",
      estado: "Pendente",
      email: "maria.costa@email.com",
      telefone: "987654321",
      cv: "/cvs/maria-costa.pdf",
    },
  ]);

  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  // 🔍 Pesquisa (por nome, vaga, email)
  const filtered = useMemo(() => {
    const term = (searchTerm || "").toLowerCase().trim();
    return candidatos.filter(
      (c) =>
        (c.nome || "").toLowerCase().includes(term) ||
        (c.vaga || "").toLowerCase().includes(term) ||
        (c.email || "").toLowerCase().includes(term)
    );
  }, [candidatos, searchTerm]);

  // Paginação
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const current = filtered.slice(indexOfFirst, indexOfLast);
  const totalPages = Math.max(1, Math.ceil(filtered.length / itemsPerPage));

  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm]);

  // Handlers (substitui alert por chamada real à API quando tiveres endpoint)
  const aprovarCandidato = async (id) => {
    try {
      setIsLoading(true);
      // Exemplo de chamada real:
      // const res = await fetch(`http://localhost:5136/api/v1/candidates/${id}/approve`, { method: "POST" });
      // if (!res.ok) throw new Error("Falha ao aprovar");
      // const novoFuncionario = await res.json();

      setCandidatos((prev) =>
        prev.map((c) => (c.id === id ? { ...c, estado: "Aprovado" } : c))
      );
      alert("Candidato aprovado e (exemplo) convertido em funcionário!");
    } catch (err) {
      console.error(err);
      alert("Erro ao aprovar candidato");
    } finally {
      setIsLoading(false);
    }
  };

  const rejeitarCandidato = async (id) => {
    try {
      setIsLoading(true);
      // Exemplo chamada real:
      // const res = await fetch(`http://localhost:5136/api/v1/candidates/${id}/reject`, { method: "POST" });
      // if (!res.ok) throw new Error("Falha ao rejeitar");

      setCandidatos((prev) =>
        prev.map((c) => (c.id === id ? { ...c, estado: "Rejeitado" } : c))
      );
      alert("Candidato rejeitado.");
    } catch (err) {
      console.error(err);
      alert("Erro ao rejeitar candidato");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="container mt-4">
      {/* Header */}
      <div className="mb-4 d-flex justify-content-between align-items-center">
        <h1 className="mb-0 h3">Gestão de Candidatos</h1>
        
        <span className="text-muted small">
          Total:{" "}
          <span className="badge bg-secondary bg-opacity-50 text-dark">
            {candidatos.length}
          </span>
        </span>
      </div>

      {/* Search */}
      <div className="card mb-3 border-0 shadow-sm">
        <div className="card-body">
          {isLoading ? (
            <div className="text-center py-3">
              <div className="spinner-border text-secondary" role="status">
                <span className="visually-hidden">Carregando...</span>
              </div>
            </div>
          ) : (
            <input
              type="text"
              className="form-control"
              placeholder="Procurar por nome, vaga ou email..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          )}
        </div>
      </div>

      {/* Content */}
      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          {isLoading ? (
            <div className="text-center py-5">
              <div className="spinner-border text-secondary" role="status">
                <span className="visually-hidden">Carregando...</span>
              </div>
            </div>
          ) : current.length === 0 ? (
            <div className="text-center py-4 text-muted">Sem candidatos</div>
          ) : (
            <>
              {/* Desktop Table */}
              <div className="table-responsive d-none d-md-block">
                <table className="table table-hover mb-0">
                  <thead className="table-light">
                    <tr>
                      <th className="px-4 py-3">Nome</th>
                      <th className="px-4 py-3">Email</th>
                      <th className="px-4 py-3">Telefone</th>
                      <th className="px-4 py-3">Vaga</th>
                      <th className="px-4 py-3">CV</th>
                      <th className="px-4 py-3">Estado</th>
                      <th className="px-4 py-3 text-center">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {current.map((c) => (
                      <tr key={c.id}>
                        <td className="px-4 py-3">
                          <div className="d-flex align-items-center">
                            <div
                              className="rounded-circle bg-secondary bg-opacity-25 d-flex align-items-center justify-content-center me-2"
                              style={{ width: 32, height: 32 }}
                              aria-label="Avatar"
                            >
                              <span className="text-muted small fw-bold">
                                {c.nome
                                  .split(" ")
                                  .map((p) => p[0])
                                  .slice(0, 2)
                                  .join("")
                                  .toUpperCase()}
                              </span>
                            </div>
                            <div>
                              <div className="fw-semibold">{c.nome}</div>
                              <div className="text-muted small">{c.vaga}</div>
                            </div>
                          </div>
                        </td>
                        <td className="px-4 py-3 text-muted">
                          <span className="text-truncate d-inline-block" style={{ maxWidth: 220 }}>
                            {c.email}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-muted">{c.telefone}</td>
                        <td className="px-4 py-3 text-muted">{c.vaga}</td>
                        <td className="px-4 py-3">
                          <a
                            href={c.cv}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="btn btn-sm btn-outline-secondary"
                          >
                            Ver CV
                          </a>
                        </td>
                        <td className="px-4 py-3">
                          <EstadoBadge estado={c.estado} />
                        </td>
                        <td className="px-4 py-3 text-center">
                          <div className="btn-group btn-group-sm" role="group">
                            <button
                              className="btn btn-outline-success"
                              onClick={() => aprovarCandidato(c.id)}
                              disabled={isLoading}
                              type="button"
                            >
                              Aprovar
                            </button>
                            <button
                              className="btn btn-outline-danger"
                              onClick={() => rejeitarCandidato(c.id)}
                              disabled={isLoading}
                              type="button"
                            >
                              Rejeitar
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Mobile Cards */}
              <div className="d-md-none">
                {current.map((c) => (
                  <div key={c.id} className="border-bottom p-3">
                    <div className="d-flex align-items-center mb-2">
                      <div
                        className="rounded-circle bg-secondary bg-opacity-25 d-flex align-items-center justify-content-center me-2"
                        style={{ width: 32, height: 32 }}
                        aria-label="Avatar"
                      >
                        <span className="text-muted small fw-bold">
                          {c.nome
                            .split(" ")
                            .map((p) => p[0])
                            .slice(0, 2)
                            .join("")
                            .toUpperCase()}
                        </span>
                      </div>
                      <div className="flex-grow-1">
                        <div className="fw-semibold">{c.nome}</div>
                        <div className="text-muted small">{c.vaga}</div>
                      </div>
                      <EstadoBadge estado={c.estado} />
                    </div>

                    <p className="text-muted small mb-1">
                      <strong>Email:</strong>{" "}
                      <span className="text-truncate d-inline-block" style={{ maxWidth: 220 }}>
                        {c.email}
                      </span>
                    </p>
                    <p className="text-muted small mb-2">
                      <strong>Telefone:</strong> {c.telefone}
                    </p>
                    <div className="d-flex gap-2">
                      <a
                        href={c.cv}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="btn btn-sm btn-outline-secondary flex-fill"
                      >
                        Ver CV
                      </a>
                      <button
                        className="btn btn-sm btn-outline-success flex-fill"
                        onClick={() => aprovarCandidato(c.id)}
                        disabled={isLoading}
                        type="button"
                      >
                        Aprovar
                      </button>
                      <button
                        className="btn btn-sm btn-outline-danger flex-fill"
                        onClick={() => rejeitarCandidato(c.id)}
                        disabled={isLoading}
                        type="button"
                      >
                        Rejeitar
                      </button>
                    </div>
                  </div>
                ))}
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
  );
}