
import React, { useEffect, useMemo, useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

export default function Candidatos() {
  const [isLoading, setIsLoading] = useState(false);
  const [candidatos, setCandidatos] = useState([]);
  const [error, setError] = useState("");

  // Ajusta conforme o teu ambiente
  const API_BASE = "http://localhost:5136";

  // Carregar candidatos da API
  useEffect(() => {
    const fetchCandidatos = async () => {
      try {
        setIsLoading(true);
        setError("");
        const res = await fetch(`${API_BASE}/api/v1/jobcandidate`);
        if (!res.ok) throw new Error(`Falha ao obter candidatos (${res.status})`);
        const data = await res.json();

        // Mapeia também o URL do PDF
        const mapped = (data || []).map((d) => ({
          id: d.jobCandidateId,
          numero: d.jobCandidateId,
          cvXml: d.resume || "",
          cvPdfUrl: d.cvFileUrl ? `${API_BASE}${d.cvFileUrl}` : "" // constrói URL absoluto
        }));

        setCandidatos(mapped);
      } catch (e) {
        console.error(e);
        setError("Não foi possível carregar os candidatos.");
      } finally {
        setIsLoading(false);
      }
    };
    fetchCandidatos();
  }, []);

  // Pesquisa
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  const filtered = useMemo(() => {
    const term = (searchTerm || "").toLowerCase().trim();
    return candidatos.filter(
      (c) =>
        String(c.numero || "").toLowerCase().includes(term) ||
        String(c.cvXml || "").toLowerCase().includes(term)
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

  // Abrir PDF numa nova aba (com fallback se o popup for bloqueado)
  const abrirCvPdf = (url) => {
    if (!url) return alert("CV PDF não disponível.");
    const win = window.open(url, "_blank", "noopener,noreferrer");
    if (!win) {
      // Fallback: criar link temporário caso o browser bloqueie window.open
      const a = document.createElement("a");
      a.href = url;
      a.target = "_blank";
      a.rel = "noopener noreferrer";
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
    }
  };

  // Aprovar candidato (simulação)
  const aprovarCandidato = async (id) => {
    try {
      setIsLoading(true);
      // TODO: integrar com endpoint real quando existir
      alert("Candidato aprovado!");
    } catch (err) {
      console.error(err);
      alert("Erro ao aprovar candidato");
    } finally {
      setIsLoading(false);
    }
  };

  // Eliminar candidato (simulação)
  const eliminarCandidato = async (id) => {
    try {
      if (!confirm("Tens a certeza que queres eliminar este candidato?")) return;
      setIsLoading(true);
      // TODO: integrar com endpoint real quando existir
      setCandidatos((prev) => prev.filter((c) => c.id !== id));
      alert("Candidato eliminado.");
    } catch (err) {
      console.error(err);
      alert("Erro ao eliminar candidato");
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
              placeholder="Procurar por Nº de candidato ou conteúdo do CV..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          )}
          {error && <div className="text-danger small mt-2">{error}</div>}
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
                      <th className="px-4 py-3" style={{ width: 140 }}>Nº Candidato</th>
                      <th className="px-4 py-3">CV</th>
                      <th className="px-4 py-3 text-center" style={{ width: 220 }}>Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {current.map((c) => (
                      <tr key={c.id}>
                        <td className="px-4 py-3">
                          <span className="fw-semibold">{c.numero}</span>
                        </td>
                        <td className="px-4 py-3">
                          <button
                            className="btn btn-sm btn-outline-primary"
                            onClick={() => abrirCvPdf(c.cvPdfUrl)}
                            disabled={!c.cvPdfUrl}
                            type="button"
                          >
                            Ver CV (PDF)
                          </button>
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
                              onClick={() => eliminarCandidato(c.id)}
                              disabled={isLoading}
                              type="button"
                            >
                              Eliminar
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
                      <div className="flex-grow-1">
                        <div className="fw-semibold">Nº {c.numero}</div>
                      </div>
                    </div>

                    <div className="d-flex gap-2">
                      <button
                        className="btn btn-sm btn-outline-primary flex-fill"
                        onClick={() => abrirCvPdf(c.cvPdfUrl)}
                        disabled={!c.cvPdfUrl}
                        type="button"
                      >
                        Ver CV (PDF)
                      </button>
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
                        onClick={() => eliminarCandidato(c.id)}
                        disabled={isLoading}
                        type="button"
                      >
                        Eliminar
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
       
  );}
