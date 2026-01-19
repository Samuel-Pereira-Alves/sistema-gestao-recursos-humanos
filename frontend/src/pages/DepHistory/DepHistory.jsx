import React, { useEffect, useMemo, useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import Loading from "../../components/Loading/Loading"; 
import ReadOnlyField from "../../components/ReadOnlyField/ReadOnlyField";
import EmployeeDetails from "../../components/EmployeeDetails/EmployeeDetails";
import {mapDepartmentHistories,normalize,paginate,formatDate,} from "../../utils/Utils";
import { getEmployee } from "../../Service/employeeService";
import { getDepHistoriesById } from "../../Service/departmentHistoryService";

export default function DepartmentHistoryList() {
  const [loading, setLoading] = useState(false);
  const [fetchError, setFetchError] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;

  const [departamentos, setDepartamentos] = useState([]);
  const [employee, setEmployee] = useState(null);

  useEffect(() => {
    const id = localStorage.getItem("businessEntityId");
    const token = localStorage.getItem("authToken");

    if (!id) {
      setFetchError("ID do funcionário não encontrado no localStorage.");
      return;
    }

    async function load() {
      setLoading(true);
      setFetchError(null);
      try {
        const data = await getDepHistoriesById(token, id);
        console.log(data.items)
        setEmployee(data);
        setDepartamentos(data.items);
      } catch (err) {
        console.error(err);
        setFetchError(err.message || "Erro desconhecido ao obter dados.");
      } finally {
        setLoading(false);
      }
    }

    load();
  }, []);

  useEffect(() => setCurrentPage(1), [searchTerm]);

  const termo = normalize(searchTerm);
  const filteredDepartamentos = useMemo(() => {
    return departamentos.filter((d) => {
      return (
        normalize(d.name).includes(termo) ||
        normalize(d.groupName).includes(termo)
      );
    });
  }, [departamentos, termo]);

  const { slice: currentDepartamentos, totalPages } = paginate(
    filteredDepartamentos,
    currentPage,
    itemsPerPage
  );

  return (
    <div className="container mt-4">
      <BackButton />

      <div className="mb-4 d-flex justify-content-between align-items-center">
        <h1 className="h3 mb-3">Histórico de Departamentos</h1>
      </div>

      {/* Search */}
      <div className="card mb-3 border-0 shadow-sm">
        <div className="card-body">
          {loading ? (
            <Loading text="Carregando dados..." />
          ) : (
            <input
              type="text"
              className="form-control"
              placeholder="Procurar por departamento ou grupo..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          )}
        </div>
      </div>

      {/* Content */}
      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          {fetchError ? (
            <div className="text-center py-5">
              <div className="alert alert-light border text-muted">
                {fetchError}
              </div>
            </div>
          ) : loading ? (
            <Loading text="Carregando histórico..." /> 
          ) : (
            <>
              {/* Desktop Table */}
              <div className="table-responsive d-none d-md-block">
                <table className="table table-hover mb-0">
                  <thead className="table-light">
                    <tr>
                      <th>Departamento</th>
                      <th>Grupo</th>
                      <th>Data Início</th>
                      <th>Data Fim</th>
                    </tr>
                  </thead>
                  <tbody>
                    {currentDepartamentos.length === 0 ? (
                      <tr>
                        <td colSpan={4} className="text-center text-muted">
                          Sem registos
                        </td>
                      </tr>
                    ) : (
                      currentDepartamentos.map((d) => (
                        <tr key={d.department.departmentID}>
                          <td>{d.department.name}</td>
                          <td className="text-muted">{d.department.groupName || "—"}</td>
                          <td className="text-muted">
                            {formatDate(d.startDate)}
                          </td>
                          <td className="text-muted">
                            {d.endDate == null
                              ? "Atual"
                              : formatDate(d.endDate)}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>

              {/* Mobile Cards */}
              <div className="d-md-none">
                {currentDepartamentos.length === 0 ? (
                  <div className="text-center p-3 text-muted">Sem registos</div>
                ) : (
                  currentDepartamentos.map((d) => (
                    <div key={d.department.departmentID} className="border-bottom p-3">
                      <h6>{d.department.name}</h6>
                      {d.department.groupName && (
                        <p className="text-muted small">{d.department.groupName}</p>
                      )}
                      <ReadOnlyField
                        label="Início"
                        value={formatDate(d.startDate)}
                      />
                      <ReadOnlyField
                        label="Fim"
                        value={
                          d.endDate == null ? "Atual" : formatDate(d.endDate)
                        }
                      />
                    </div>
                  ))
                )}
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
