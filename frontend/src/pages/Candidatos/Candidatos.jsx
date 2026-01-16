
import React, { useEffect, useState, useCallback } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";
import { addNotification } from "../../utils/notificationBus";
import axios from "axios";
import {
  approveCandidate,
  deleteCandidate,
  getCandidatos,
  openPdf,
  // sendFeedbackEmail // se passares a usar o service
} from "../../Service/candidatosService";
import Pagination from "../../components/Pagination/Pagination";

export default function Candidatos() {
  const [isLoading, setIsLoading] = useState(false);
  const [rows, setRows] = useState([]);
  const [error, setError] = useState("");

  // Paginação no servidor
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  // Metadados vindos da API
  const [meta, setMeta] = useState({
    totalCount: 0,
    pageNumber: 1,
    pageSize: itemsPerPage,
    totalPages: 1,
    hasPrevious: false,
    hasNext: false,
  });

  // (Opcional) se quiseres manter pesquisa no UI, podemos ligar ao backend depois
  // Por enquanto não afeta o fetch (sem search no backend)
  const [searchTerm, setSearchTerm] = useState("");

  const mapItem = (d) => {
    const id =
      d.jobCandidateId ?? d.jobCandidateID ?? d.id ?? d.numero ?? d.JobCandidateId;
    const first =
      d.firstName ?? d.person?.firstName ?? d.nome?.split(" ")[0] ?? "";
    const last =
      d.lastName ?? d.person?.lastName ?? d.nome?.split(" ").slice(1).join(" ") ?? "";
    const email = d.email ?? d.person?.email ?? "";

    return {
      id,
      numero: id,
      nome: `${first} ${last}`.trim(),
      email,
    };
  };

  const fetchCandidatos = useCallback(
    async (page = currentPage) => {
      try {
        const token = localStorage.getItem("authToken");
        setIsLoading(true);
        setError("");

        // 🔴 Apenas paginação no servidor (sem sort/search)
        const { items, meta } = await getCandidatos(token, {
          pageNumber: page,
          pageSize: itemsPerPage,
        });

        const mapped = (items || []).map(mapItem);

        setRows(mapped);
        setMeta(meta);
        return { items: mapped, meta };
      } catch (e) {
        console.error(e);
        setError("Não foi possível carregar os candidatos.");
        setRows([]);
        setMeta((m) => ({ ...m, totalCount: 0, totalPages: 1, pageNumber: 1 }));
        return { items: [], meta: { totalPages: 1, pageNumber: 1 } };
      } finally {
        setIsLoading(false);
      }
    },
    [currentPage, itemsPerPage]
  );

  useEffect(() => {
    fetchCandidatos(currentPage);
  }, [fetchCandidatos, currentPage]);

  // Abrir PDF numa nova aba
  const downloadCvPdf = (id) => {
    openPdf(id);
  };

  const aprovarCandidato = async (id, nome, email) => {
    try {
      setIsLoading(true);
      await approveCandidate(id);
      alert("Candidato aprovado com sucesso.");

      const res = await fetchCandidatos(currentPage);
      // Se a página ficou vazia após mudanças, recua uma página
      if ((res.items?.length ?? 0) === 0 && currentPage > 1) {
        const newPage = Math.max(1, Math.min(currentPage - 1, res.meta?.totalPages || 1));
        if (newPage !== currentPage) setCurrentPage(newPage);
      }

      addNotification(
        `O candidato ${nome} foi aprovado como funcionário.`,
        "admin",
        { type: "EMPLOYEES" }
      );

      // (Opcional) enviar email por aqui
      // await sendFeedbackEmail(email, true);
    } catch (err) {
      console.error(err);
      alert("Falha ao aprovar candidato.");
    } finally {
      setIsLoading(false);
    }
  };

  const eliminarCandidato = async (id, nome, email) => {
    try {
      if (!confirm("Tens a certeza que queres eliminar este candidato?")) return;

      setIsLoading(true);
      setError("");

      await deleteCandidate(id);

      const res = await fetchCandidatos(currentPage);
      if ((res.items?.length ?? 0) === 0 && currentPage > 1) {
        const newPage = Math.max(1, Math.min(currentPage - 1, res.meta?.totalPages || 1));
        if (newPage !== currentPage) setCurrentPage(newPage);
      }

      alert("Candidato eliminado com sucesso.");
      addNotification(`O candidato ${nome} foi recusado.`, "admin", { type: "CANDIDATE" });

      // (Opcional) enviar email por aqui
      // await sendFeedbackEmail(email, false);
    } catch (err) {
      console.error(err);
      setError("Erro ao eliminar candidato. Tenta novamente.");
      alert("Erro ao eliminar candidato.");
    } finally {
      setIsLoading(false);
    }
  };

  // Enviar email (mantive tua função, mas idealmente usa um service)
  async function sendEmail(email, condition) {
    const frase =
      condition === false
        ? "Infelizmente, a sua candidatura não foi aprovada nesta fase do processo. Agradecemos o seu interesse e o tempo dedicado à candidatura. Continuaremos a considerar o seu perfil para futuras oportunidades compatíveis."
        : "Parabéns! A sua candidatura foi aprovada nesta fase do processo. Em breve entraremos em contacto para lhe fornecer mais detalhes sobre os próximos passos. Obrigado pelo seu interesse e confiança.";

    try {
      await axios.post(
        "http://localhost:5136/api/email/send",
        { to: email, subject: "Feedback Candidatura", text: frase },
        { headers: { "Content-Type": "application/json" } }
      );
    } catch (e) {
      if (e.response) console.error("Erro API:", e.response.data);
      else console.error("Erro rede/CORS:", e.message);
    }
  }

  return (
    <div className="container mt-3">
      <BackButton />

      {/* Header */}
      <div className="mb-2 d-flex justify-content-between align-items-center">
        <h1 className="mb-0 h4">Gestão de Candidatos</h1>
        <span className="text-muted small">
          Total:&nbsp;
          <span className="badge bg-secondary bg-opacity-50 text-dark">
            {meta.totalCount}
          </span>
        </span>
      </div>

      {/* Content */}
      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          {isLoading ? (
            <div className="text-center py-3">
              <div className="spinner-border text-secondary" role="status">
                <span className="visually-hidden">Carregando...</span>
              </div>
            </div>
          ) : rows.length === 0 ? (
            <div className="text-center py-3 text-muted">Sem candidatos</div>
          ) : (
            <>
              {/* Desktop Table */}
              <div className="table-responsive d-none d-sm-block">
                <table className="table table-hover mb-0 table-sm">
                  <thead className="table-light">
                    <tr>
                      <th className="px-3 py-2 text-center">Nº Candidato</th>
                      <th className="px-3 py-2 text-center">Nome</th>
                      <th className="px-3 py-2 text-center">CV</th>
                      <th className="px-3 py-2 text-center">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rows.map((c) => (
                      <tr key={c.id}>
                        <td className="px-3 py-2 text-center">
                          <span className="fw-semibold">{c.numero}</span>
                        </td>
                        <td className="px-3 py-2 text-center">
                          <span className="fw-semibold">{c.nome}</span>
                        </td>
                        <td className="px-3 py-2 text-center">
                          <button
                            className="btn btn-sm btn-outline-primary text-center"
                            onClick={() => downloadCvPdf(c.id)}
                            type="button"
                          >
                            Ver CV (PDF)
                          </button>
                        </td>
                        <td className="px-3 py-2 text-center">
                          <div className="btn-group btn-group-sm" role="group">
                            <button
                              className="btn btn-outline-success"
                              onClick={() => {
                                aprovarCandidato(c.id, c.nome, c.email);
                                sendEmail(c.email, true);
                              }}
                              disabled={isLoading}
                              type="button"
                            >
                              Aprovar
                            </button>
                            <button
                              className="btn btn-outline-danger"
                              onClick={() => {
                                eliminarCandidato(c.id, c.nome, c.email);
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
                {rows.map((c) => (
                  <div key={c.id} className="border-bottom p-2">
                    <div className="d-flex align-items-center mb-1">
                      <div className="flex-grow-1">
                        <div className="fw-semibold">Nº {c.numero}</div>
                      </div>
                    </div>
                    <div className="d-flex align-items-center mb-2">
                      <div className="flex-grow-1">
                        <div className="fw-semibold">{c.nome}</div>
                      </div>
                    </div>

                    <div className="d-flex gap-1">
                      <button
                        className="btn btn-sm btn-outline-primary flex-fill"
                        onClick={() => downloadCvPdf(c.id)}
                        type="button"
                      >
                        Ver CV (PDF)
                      </button>
                      <button
                        className="btn btn-sm btn-outline-success flex-fill"
                        onClick={() => aprovarCandidato(c.id, c.nome, c.email)}
                        disabled={isLoading}
                        type="button"
                      >
                        Aprovar
                      </button>
                      <button
                        className="btn btn-sm btn-outline-danger flex-fill"
                        onClick={() => eliminarCandidato(c.id, c.nome, c.email)}
                        disabled={isLoading}
                        type="button"
                      >
                        Eliminar
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              {/* Pagination (meta do servidor) */}
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
  );
}
