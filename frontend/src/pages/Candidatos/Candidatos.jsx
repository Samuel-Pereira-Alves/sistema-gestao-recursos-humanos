import React, { useEffect, useState, useCallback } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import axios from "axios";
import {
  approveCandidate,
  deleteCandidate,
  getCandidatos,
  openPdf,
} from "../../Service/candidatosService";
import { addNotification } from "../../utils/notificationBus";
import Loading from "../../components/Loading/Loading";

export default function Candidatos() {
  const [rows, setRows] = useState([]);
  const [isLoading, setIsLoading] = useState(false);

  // Paginação
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;

  const [meta, setMeta] = useState({
    totalCount: 0,
    totalPages: 1,
    pageNumber: 1,
  });

  const [searchTerm, setSearchTerm] = useState("");
  const [search, setSearch] = useState("");

  useEffect(() => {
    const t = setTimeout(() => {
      setSearch(searchTerm.trim());
    }, 300);
    return () => clearTimeout(t);
  }, [searchTerm]);

  useEffect(() => {
    setCurrentPage(1);
  }, [search]);

  const mapItem = (d) => {
    const id = d.jobCandidateId ?? d.jobCandidateID ?? d.id ?? d.JobCandidateId;
    const first = d.firstName ?? d.person?.firstName ?? "";
    const last = d.lastName ?? d.person?.lastName ?? "";
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
      const token = localStorage.getItem("authToken");

      setIsLoading(true);

      try {
        const { items, meta } = await getCandidatos(token, {
          pageNumber: page,
          pageSize: itemsPerPage,
          search,
        });

        const mapped = (items ?? []).map(mapItem);
        setRows(mapped);
        setMeta(meta);

        return { items: mapped, meta };
      } catch (e) {
        console.error("Erro ao carregar candidatos:", e);
        setRows([]);
        setMeta((m) => ({ ...m, totalPages: 1, pageNumber: 1 }));
        return { items: [], meta: { totalPages: 1, pageNumber: 1 } };
      } finally {
        setIsLoading(false);
      }
    },
    [currentPage, search]
  );

  useEffect(() => {
    fetchCandidatos(currentPage);
  }, [fetchCandidatos, currentPage]);

  const downloadCvPdf = (id) => openPdf(id);

  const aprovarCandidato = async (id, nome, email) => {
    try {
      await approveCandidate(id);
      await fetchCandidatos(currentPage);
      try {
        alert("Candidato aprovado.");
        await axios.post("http://localhost:5136/api/email/send", {
          to: email,
          subject: "Candidatura Feedback",
          text: "Temos o prazer de informar que a sua candidatura foi aprovada. Agradecemos o interesse demonstrado e valorizamos o tempo dedicado ao processo",
        });
      } catch (e) {
        if (e.response) {
          console.error("Erro API:", e.response.data);
        } else {
          console.error("Erro rede/CORS:", e.message);
        }
      }
      addNotification(`O candidato ${nome} foi aprovado.`, "admin", { type: "EMPLOYEES" });
    } catch (e) {
      //alert("Erro ao aprovar candidato.");
      console.error("Erro ao aprovar candidato:", e);
    }
  };

  const eliminarCandidato = async (id, nome, email) => {
    try {
      if (!confirm("Deseja eliminar este candidato?")) return;

      await deleteCandidate(id);
      alert("Candidato eliminado.");
      
      const result = await fetchCandidatos(currentPage);
      if ((result.items?.length ?? 0) === 0 && currentPage > 1) {
        setCurrentPage(Math.max(1, currentPage - 1));
      }
      
      try {
        await axios.post("http://localhost:5136/api/email/send", {
          to: email,
          subject: "Candidatura Feedback",
          text: "Infelizmente não vamos dar seguimento à sua candidatura. Agradecemos o interesse demonstrado e valorizamos o tempo dedicado ao processo",
        });
      } catch (e) {
        if (e.response) {
          console.error("Erro API:", e.response.data);
        } else {
          console.error("Erro rede/CORS:", e.message);
        }
      }
      addNotification(`Candidato ${nome} eliminado.`, "admin", { type: "CANDIDATE" });
    } catch (e) {
      alert("Erro ao eliminar.");
    }
  };

  return (
    
    <div className="container mt-4">
      <BackButton />
      {/* Header */}
      <div className="mb-4 d-flex justify-content-between align-items-center">
        <h1 className="h4 mb-0">Gestão de Candidatos</h1>
        <span className="text-muted small">
          Total:&nbsp;
          <span className="badge bg-secondary">{meta.totalCount}</span>
        </span>
      </div>

      {/* Search */}
      <div className="card mb-3 border-0 shadow-sm">
        <div className="card-body position-relative">
          <input
            type="text"
            className="form-control"
            placeholder="Procurar por nome..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            aria-label="Pesquisar candidatos"
          />
          {isLoading && (
            <div
              className="position-absolute top-50 end-0 translate-middle-y me-3 text-muted small"
              aria-hidden="true"
            >
              <span className="spinner-border spinner-border-sm" /> A carregar...
            </div>
          )}
        </div>
      </div>

      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          {isLoading && rows.length === 0 ? (
            <div className="py-4">
              <Loading text="Carregando candidatos..." />
            </div>
          ) : rows.length === 0 ? (
            <div className="text-center py-5">
              <div className="alert alert-light text-muted d-inline-block">
                Não existem candidatos.
              </div>
            </div>
          ) : (
            <>
              {/* Desktop Table */}
              <div className="table-responsive d-none d-sm-block">
                <table className="table table-hover mb-0">
                  <thead className="table-light">
                    <tr>
                      <th className="text-center">ID</th>
                      <th className="text-center">Nome</th>
                      <th className="text-center">CV</th>
                      <th className="text-center">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rows.map((c) => (
                      <tr key={c.id}>
                        <td className="text-center">{c.numero}</td>
                        <td className="text-center text-muted">{c.nome}</td>
                        <td className="text-center">
                          <button
                            className="btn btn-sm btn-outline-primary"
                            onClick={() => downloadCvPdf(c.id)}
                          >
                            Ver PDF
                          </button>
                        </td>

                        <td className="text-center">
                          <div className="d-flex justify-content-center gap-2">
                            <button
                              className="btn btn-sm btn-outline-success"
                              onClick={() => aprovarCandidato(c.id, c.nome, c.email)}
                            >
                              Aprovar
                            </button>

                            <button
                              className="btn btn-sm btn-outline-danger"
                              onClick={() => eliminarCandidato(c.id, c.nome, c.email)}
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
              <div className="d-sm-none">
                {rows.map((c) => (
                  <div key={c.id} className="border-bottom p-3">
                    <h6>{c.nome}</h6>
                    <p className="text-muted small mb-2">ID: {c.numero}</p>

                    <div className="d-flex gap-2 mb-2">
                      <button
                        className="btn btn-sm btn-outline-primary"
                        onClick={() => downloadCvPdf(c.id)}
                      >
                        Ver PDF
                      </button>
                    </div>

                    <div className="d-flex gap-2">
                      <button
                        className="btn btn-sm btn-outline-success"
                        onClick={() => aprovarCandidato(c.id, c.nome, c.email)}
                      >
                        Aprovar
                      </button>
                      <button
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => eliminarCandidato(c.id, c.nome, c.email)}
                      >
                        Eliminar
                      </button>
                    </div>
                  </div>
                ))}
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
  );
}