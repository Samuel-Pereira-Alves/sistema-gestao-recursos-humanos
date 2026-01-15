import React, { useEffect, useMemo, useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";
import { addNotification } from "../../utils/notificationBus";
import axios from 'axios';
import { approveCandidate, deleteCandidate, getCandidatos, openPdf, sendFeedbackEmail } from "../../Service/candidatosService";
import Pagination from "../../components/Pagination/Pagination"

export default function Candidatos() {
  const [isLoading, setIsLoading] = useState(false);
  const [candidatos, setCandidatos] = useState([]);
  const [error, setError] = useState("");

  const fetchCandidatos = async () => {
    try {
      const token = localStorage.getItem("authToken");
      setIsLoading(true);
      setError("");
      const data = await getCandidatos(token);

      const mapped = (data || []).map((d) => ({
        id: d.jobCandidateId,
        numero: d.jobCandidateId,
        nome: d.firstName + " " + d.lastName,
        email: d.email
      }));

      setCandidatos(mapped);
    } catch (e) {
      console.error(e);
      setError("Não foi possível carregar os candidatos.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchCandidatos();
  }, []);

  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  const filtered = useMemo(() => {
    const term = (searchTerm || "").toLowerCase().trim();
    return candidatos.filter((c) =>
      String(c.nome || "")
        .toLowerCase()
        .includes(term)
    );
  }, [candidatos, searchTerm]);

  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const current = filtered.slice(indexOfFirst, indexOfLast);
  const totalPages = Math.max(1, Math.ceil(filtered.length / itemsPerPage));

  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm]);

  // Abrir PDF numa nova aba
  const downloadCvPdf = (id) => {
    openPdf(id);
  };  
  
  const aprovarCandidato = async (id, nome) => {
    try {
      setIsLoading(true);
      await approveCandidate(id);
      
      alert("Candidato aprovado com sucesso.");
      fetchCandidatos();
      addNotification(
        `O candidato ${nome} foi aprovado como funcionário.`,
        "admin",
        { type: "EMPLOYEES" }
      );
    } catch (err) {
      console.error(err);
      alert("Falha ao aprovar candidato.");
    } finally {
      setIsLoading(false);
    }
  };
  
  const eliminarCandidato = async (id, nome) => {
    try {
      if (!confirm("Tens a certeza que queres eliminar este candidato?")) return;
      
      setIsLoading(true);
      setError("");
      
      await deleteCandidate(id);
      
      setCandidatos((prev) => prev.filter((c) => c.id !== id));
      alert("Candidato eliminado com sucesso.");
      addNotification(
        `O candidato ${nome} foi recusado.`,
        "admin",
        { type: "CANDIDATE" }
      );
    } catch (err) {
      console.error(err);
      setError("Erro ao eliminar candidato. Tenta novamente.");
      alert("Erro ao eliminar candidato.");
    } finally {
      setIsLoading(false);
    }
  };
  
  //enviar email
  async function sendEmail(email, condition) {
    var frase = condition === false
      ? "Infelizmente, a sua candidatura não foi aprovada nesta fase do processo. Agradecemos o seu interesse e o tempo dedicado à candidatura. Continuaremos a considerar o seu perfil para futuras oportunidades compatíveis."
      : "Parabéns! A sua candidatura foi aprovada nesta fase do processo. Em breve entraremos em contacto para lhe fornecer mais detalhes sobre os próximos passos. Obrigado pelo seu interesse e confiança.";

    console.log(email)
    try {

      await axios.post('http://localhost:5136/api/email/send', {
        to: email,
        subject: 'Feedback Candidatura',
        text: frase
      }, {
        headers: { "Content-Type": "application/json" }
      }
      );
    } catch (e) {
      if (e.response) {
        console.error('Erro API:', e.response.data);
      } else {
        console.error('Erro rede/CORS:', e.message);
      }
    }
  }
  
  return (
    <div className="container mt-4">
      <BackButton />
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
              <div className="table-responsive d-none d-sm-block">
                <table className="table table-hover mb-0">
                  <thead className="table-light">
                    <tr>
                      <th className="px-4 py-3 text-center">Nº Candidato</th>
                      <th className="px-4 py-3 text-center">Nome</th>
                      <th className="px-4 py-3 text-center">CV</th>
                      <th className="px-4 py-3 text-center">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {current.map((c) => (
                      <tr key={c.id}>
                        <td className="px-4 py-3 text-center">
                          <span className="fw-semibold">{c.numero}</span>
                        </td>
                        <td className="px-4 py-3 text-center">
                          <span className="fw-semibold">{c.nome}</span>
                        </td>
                        <td className="px-4 py-3 text-center">
                          <button
                            className="btn btn-sm btn-outline-primary text-center"
                            onClick={() => downloadCvPdf(c.id)}
                            type="button"
                          >
                            Ver CV (PDF)
                          </button>
                        </td>
                        <td className="px-4 py-3 text-center">
                          <div className="btn-group btn-group-sm" role="group">
                            <button
                              className="btn btn-outline-success"
                              onClick={() => {
                                aprovarCandidato(c.id, c.nome);
                                sendEmail(c.email, true)
                              }}
                              disabled={isLoading}
                              type="button"
                            >
                              Aprovar
                            </button>
                            <button
                              className="btn btn-outline-danger"
                              onClick={() => {
                                eliminarCandidato(c.id, c.nome);
                                sendEmail(c.email, false);
                              }}
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
                    <div className="d-flex align-items-center mb-2">
                      <div className="flex-grow-1">
                        <div className="fw-semibold">{c.nome}</div>
                      </div>
                    </div>

                    <div className="d-flex gap-2">
                      <button
                        className="btn btn-sm btn-outline-primary flex-fill"
                        onClick={() => downloadCvPdf(c.id)}
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
