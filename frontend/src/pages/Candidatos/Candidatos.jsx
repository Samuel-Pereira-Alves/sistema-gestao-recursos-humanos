
import React, { useEffect, useState, useCallback, useRef } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import {
  approveCandidate,
  deleteCandidate,
  getCandidatos,
  openPdf,
} from "../../Service/candidatosService";
import { addNotification } from "../../utils/notificationBus";

export default function Candidatos() {
  const [rows, setRows] = useState([]);
  const [isLoading, setIsLoading] = useState(false);

  // Paginação
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 4;

  const [meta, setMeta] = useState({
    totalCount: 0,
    totalPages: 1,
    pageNumber: 1,
  });

  // Pesquisa + debounce
  const [searchTerm, setSearchTerm] = useState("");
  const [search, setSearch] = useState("");

  // Debounce 300ms
  useEffect(() => {
    const t = setTimeout(() => {
      setSearch(searchTerm.trim());
    }, 300);
    return () => clearTimeout(t);
  }, [searchTerm]);

  // Quando mudar a pesquisa → voltar à página 1
  useEffect(() => {
    setCurrentPage(1);
  }, [search]);

  // Map DTO -> UI-table row
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

  // Fetch principal
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
        console.error(e);
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
      alert("Candidato aprovado.");
      await fetchCandidatos(currentPage);
      addNotification(`O candidato ${nome} foi aprovado.`, "admin", { type: "EMPLOYEES" });
    } catch (e) {
      alert("Erro ao aprovar candidato.");
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

      addNotification(`Candidato ${nome} eliminado.`, "admin", { type: "CANDIDATE" });
    } catch (e) {
      alert("Erro ao eliminar.");
    }
  };

  return (
    <div className="container mt-3">
      <BackButton />

      <div className="mb-3 d-flex justify-content-between">
        <h1 className="h4 mb-0">Gestão de Candidatos</h1>
        <span className="text-muted small">
          Total:&nbsp;
          <span className="badge bg-secondary">{meta.totalCount}</span>
        </span>
      </div>

      {/* SEARCHBAR */}
      <input
        type="text"
        className="form-control mb-3"
        placeholder="Pesquisar por nome ou email…"
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
      />

      <div className="card shadow-sm">
        <div className="card-body p-0">
          {isLoading ? (
            <div className="text-center py-3">
              <div className="spinner-border"></div>
            </div>
          ) : rows.length === 0 ? (
            <div className="text-center py-3 text-muted">Candidatos não encontrados.</div>
          ) : (
            <>
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
                        <td className="text-center">{c.nome}</td>
                        <td className="text-center">
                          <button
                            className="btn btn-sm btn-outline-primary"
                            onClick={() => downloadCvPdf(c.id)}
                          >
                            Ver PDF
                          </button>
                        </td>
                        <td className="text-center">
                          <div className="btn-group btn-group-sm">
                            <button
                              className="btn btn-outline-success"
                              onClick={() => aprovarCandidato(c.id, c.nome, c.email)}
                            >
                              Aprovar
                            </button>
                            <button
                              className="btn btn-outline-danger"
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

              {/* Pagination */}
              {rows.length > 0 ? (
                <Pagination
                  currentPage={currentPage}
                  totalPages={meta.totalPages}
                  setPage={setCurrentPage}
                />
              ) : (<> </>)
              }
            </>
          )}
        </div>
      </div>
    </div>
  );
}
