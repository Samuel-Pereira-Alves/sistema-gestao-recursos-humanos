import React, { useEffect, useMemo, useState, useCallback } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

/* =========================
 * Utils
 * ========================= */
function formatDate(dateStr) {
  if (!dateStr) return "—";
  const d = new Date(dateStr);
  if (isNaN(d)) return "—";
  return d.toLocaleDateString("pt-PT");
}
function normalize(t) {
  if (!t) return "";
  return String(t)
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}
function idToString(id) {
  if (id == null) return "";
  return String(id).trim();
}
function dateInputToIsoMidnight(dateStr) {
  return dateStr ? `${dateStr}T00:00:00` : "";
}

/* Helpers (suportam camelCase/PascalCase) */
const getBusinessEntityID = (h) =>
  h?.businessEntityID ??
  h?.BusinessEntityID ??
  h?.employee?.businessEntityID ??
  "";
const getDepartmentID = (h) =>
  h?.departmentID ??
  h?.DepartmentID ??
  h?.department?.departmentID ??
  h?.department?.DepartmentID ??
  "";
const getShiftID = (h) =>
  h?.shiftID ?? h?.ShiftID ?? h?.shift?.shiftID ?? h?.shift?.ShiftID ?? "";
const getStartDate = (h) => h?.startDate ?? h?.StartDate ?? "";
const getEndDate = (h) => h?.endDate ?? h?.EndDate ?? "";
const getDepartmentName = (h) =>
  h?.department?.name ??
  h?.department?.Name ??
  h?.departmentName ??
  h?.DepartmentName ??
  "—";
const getGroupName = (h) =>
  h?.department?.groupName ??
  h?.department?.GroupName ??
  h?.groupName ??
  h?.GroupName ??
  "—";

/* API */
const API_BASE = "http://localhost:5136/api/v1";
const EMPLOYEE_BASE = `${API_BASE}/employee`;
const DEPT_HISTORY_BASE = `${API_BASE}/departmenthistory`;

const deptHistoryUrl = (businessEntityID, departmentID, shiftID, startDate) =>
  `${DEPT_HISTORY_BASE}/${encodeURIComponent(
    businessEntityID
  )}/${encodeURIComponent(departmentID)}/${encodeURIComponent(
    shiftID
  )}/${encodeURIComponent(startDate)}`;

const SHIFT_LABELS = { 1: "Manhã", 2: "Tarde", 3: "Noite" };
const resolveShiftLabel = (id) => SHIFT_LABELS[Number(id)] ?? "—";

export default function GestaoDepartmentHistories() {
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState(null);
  const [items, setItems] = useState([]);

  /* Departamentos: derivados dos próprios histories */
  const [departments, setDepartments] = useState([]);

  // 🔎 pesquisa
  const [searchTerm, setSearchTerm] = useState("");
  // 📄 paginação
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 7;

  /* ---------- Fetch & flatten ---------- */
  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setFetchError(null);

      const response = await fetch(EMPLOYEE_BASE, {
        headers: { Accept: "application/json" },
      });
      if (!response.ok)
        throw new Error("Erro ao carregar colaboradores/departamentos");

      const data = await response.json();
      const employees = Array.isArray(data) ? data : data?.items ?? [];

      const flattened = employees.flatMap((emp) =>
        (emp.departmentHistories ?? emp.DepartmentHistories ?? []).map(
          (dh) => ({
            ...dh,
            employee: emp, // referência ao colaborador (Nome/ID)
          })
        )
      );

      // Ordena por Data Início desc
      flattened.sort(
        (a, b) => new Date(getStartDate(b)) - new Date(getStartDate(a))
      );
      setItems(flattened);

      // Derivar departamentos (ID + Nome) a partir dos histories carregados
      const derivedDeps = buildDerivedDepartments(flattened);
      setDepartments(derivedDeps);
    } catch (err) {
      setFetchError(err.message || "Erro desconhecido ao obter dados.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  /* ---------- Reset página ao mudar pesquisa ---------- */
  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm]);

  /* ---------- Filtro ---------- */
  const rawSearch = searchTerm.trim();
  const isNumericSearch = /^\d+$/.test(rawSearch);
  const termo = normalize(searchTerm);

  const filtered = useMemo(() => {
    if (!rawSearch) return items;

    return items.filter((h) => {
      const employee = h.employee ?? {};
      const person = employee.person ?? {};

      if (isNumericSearch) {
        const beid = idToString(employee.businessEntityID);
        return beid === rawSearch; // filtra por businessEntityID (igualdade exata)
      }

      const fullName = `${normalize(person.firstName)} ${normalize(
        person.lastName
      )}`.trim();
      const deptName = normalize(getDepartmentName(h));
      const groupName = normalize(getGroupName(h));
      const start = normalize(formatDate(getStartDate(h)));
      const end = normalize(formatDate(getEndDate(h)));

      return (
        (fullName && fullName.includes(termo)) ||
        (deptName && deptName.includes(termo)) ||
        (groupName && groupName.includes(termo)) ||
        (start && start.includes(termo)) ||
        (end && end.includes(termo))
      );
    });
  }, [items, rawSearch, termo, isNumericSearch]);

  /* ---------- Paginação ---------- */
  const totalPages = Math.max(1, Math.ceil(filtered.length / itemsPerPage));
  const indexOfLast = currentPage * itemsPerPage;
  const indexOfFirst = indexOfLast - itemsPerPage;
  const pageItems = filtered.slice(indexOfFirst, indexOfLast);

  /* =========================
   * CRUD (modal único Create/Edit)
   * ========================= */
  const [action, setAction] = useState({
    open: false,
    mode: "create", // 'create' | 'edit'
    loading: false,
    error: null,
    keys: {
      businessEntityID: "",
      departmentID: "",
      shiftID: "",
      startDate: "",
    },
    form: {
      businessEntityID: "",
      departmentID: "",
      shiftID: "",
      startDate: "", // create: 'yyyy-MM-dd'
      endDate: "", // create: 'yyyy-MM-dd' (opcional) | edit: ISO (mostrado como yyyy-MM-dd)
    },
  });

  const openCreate = () => {
    setAction({
      open: true,
      mode: "create",
      loading: false,
      error: null,
      keys: {
        businessEntityID: "",
        departmentID: "",
        shiftID: "",
        startDate: "",
      },
      form: {
        businessEntityID: "",
        departmentID: "",
        shiftID: "",
        startDate: "",
        endDate: "",
      },
    });
  };

  const openEdit = (h) => {
    const beid = getBusinessEntityID(h);
    const depId = getDepartmentID(h);
    const shId = getShiftID(h);
    const start = getStartDate(h);
    const end = getEndDate(h);

    setAction({
      open: true,
      mode: "edit",
      loading: false,
      error: null,
      keys: {
        businessEntityID: String(beid),
        departmentID: String(depId),
        shiftID: String(shId),
        startDate: start,
      },
      form: {
        businessEntityID: String(beid),
        departmentID: String(depId),
        shiftID: String(shId),
        startDate: start,
        endDate: end ?? "",
      },
    });
  };

  const closeAction = () =>
    setAction((s) => ({ ...s, open: false, error: null }));

  const submitAction = async () => {
    try {
      setAction((s) => ({ ...s, loading: true, error: null }));

      const { mode, form, keys } = action;

      if (mode === "create") {
        // validações mínimas
        const businessEntityID = Number(form.businessEntityID);
        const departmentID = Number(form.departmentID);
        const shiftID = Number(form.shiftID);
        if (!businessEntityID) throw new Error("BusinessEntityID em falta.");
        if (!departmentID) throw new Error("DepartmentID em falta.");
        if (!shiftID) throw new Error("ShiftID em falta.");
        if (!form.startDate) throw new Error("Data Início em falta.");

        const body = {
          businessEntityID,
          departmentID,
          shiftID,
          startDate: dateInputToIsoMidnight(form.startDate),
          endDate: form.endDate ? dateInputToIsoMidnight(form.endDate) : null,
        };

        const resp = await fetch(`${DEPT_HISTORY_BASE}`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Accept: "application/json",
          },
          body: JSON.stringify(body),
        });
        if (!resp.ok)
          throw new Error((await resp.text()) || "Falha ao criar registo.");
      } else {
        // EDIT: PATCH /departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        // Atualizamos apenas campos não-chave (ex.: endDate).
        const { businessEntityID, departmentID, shiftID, startDate } = keys;
        if (!businessEntityID || !departmentID || !shiftID || !startDate)
          throw new Error("Chaves do registo em falta.");

        const patchBody = {
          endDate: form.endDate ? form.endDate : null,
        };

        const resp = await fetch(
          deptHistoryUrl(businessEntityID, departmentID, shiftID, startDate),
          {
            method: "PATCH",
            headers: {
              "Content-Type": "application/json",
              Accept: "application/json",
            },
            body: JSON.stringify(patchBody),
          }
        );
        if (!resp.ok)
          throw new Error((await resp.text()) || "Falha ao editar registo.");
      }

      await fetchData();
      closeAction();
    } catch (e) {
      setAction((s) => ({ ...s, error: e.message || "Erro na operação." }));
    } finally {
      setAction((s) => ({ ...s, loading: false }));
    }
  };

  /* ---------- Util: derivar departamentos a partir dos histories ---------- */
  function buildDerivedDepartments(list) {
    const map = new Map();
    list.forEach((h) => {
      const id = getDepartmentID(h);
      const name = getDepartmentName(h);
      if (id) {
        const key = String(id);
        if (!map.has(key)) {
          map.set(key, { departmentID: key, name });
        } else {
          // Se o nome vier vazio "—" numa entrada e noutra vier preenchido, atualiza
          const existing = map.get(key);
          if (
            (!existing.name || existing.name === "—") &&
            name &&
            name !== "—"
          ) {
            map.set(key, { departmentID: key, name });
          }
        }
      }
    });
    const derived = Array.from(map.values());
    derived.sort((a, b) =>
      String(a.name).localeCompare(String(b.name), "pt-PT", {
        sensitivity: "base",
      })
    );
    return derived;
  }

  const resolveDepartmentName = (id) => {
    const dep = departments.find((d) => String(d.departmentID) === String(id));
    return dep?.name ?? "—";
  };

  /* =========================
   * UI
   * ========================= */
  return (
    <div className="container mt-4">
      <div className="mb-4 d-flex align-items-center justify-content-between">
        <h1 className="h3 mb-1">Histórico de Departamentos</h1>
        {!loading && (
          <button className="btn btn-sm btn-primary" onClick={openCreate}>
            Criar registo
          </button>
        )}
      </div>

      {/* Search */}
      <div className="card mb-3 border-0 shadow-sm">
        <div className="card-body">
          {loading ? (
            <div className="text-center py-3">
              <div className="spinner-border text-secondary" role="status">
                <span className="visually-hidden">Carregando...</span>
              </div>
            </div>
          ) : (
            <input
              type="text"
              className="form-control"
              placeholder="Procurar por ID, colaborador, departamento, grupo ou data..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              aria-label="Pesquisar histórico de departamentos"
            />
          )}
        </div>
      </div>

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
                      <th className="px-4 py-3">Colaborador</th>
                      <th className="px-4 py-3">Departamento</th>
                      <th className="px-4 py-3">Grupo</th>
                      <th className="px-4 py-3">Data Início</th>
                      <th className="px-4 py-3">Data Fim</th>
                      <th className="px-4 py-3 text-end">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pageItems.map((h) => {
                      const employee = h.employee ?? {};
                      const person = employee.person ?? {};
                      const deptName = getDepartmentName(h);
                      const groupName = getGroupName(h);
                      const start = getStartDate(h);
                      const end = getEndDate(h);

                      // chave estável por linha
                      const key = `${getBusinessEntityID(h)}|${getDepartmentID(
                        h
                      )}|${getShiftID(h)}|${start}`;

                      return (
                        <tr key={key}>
                          <td className="px-4 py-3">
                            {person.firstName} {person.lastName}
                            <div className="small text-muted">
                              ID: {employee.businessEntityID ?? "—"}
                            </div>
                          </td>
                          <td className="px-4 py-3">{deptName}</td>
                          <td className="px-4 py-3">{groupName}</td>
                          <td className="px-4 py-3 text-muted">
                            {formatDate(start)}
                          </td>
                          <td className="px-4 py-3 text-muted">
                            {formatDate(end)}
                          </td>
                          <td className="px-4 py-3 text-end">
                            <button
                              className="btn btn-sm btn-outline-primary"
                              onClick={() => openEdit(h)}
                            >
                              Editar
                            </button>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              {/* Mobile Cards */}
              <div className="d-md-none">
                {pageItems.map((h) => {
                  const employee = h.employee ?? {};
                  const person = employee.person ?? {};
                  const start = getStartDate(h);
                  const end = getEndDate(h);
                  const key = `${getBusinessEntityID(h)}|${getDepartmentID(
                    h
                  )}|${getShiftID(h)}|${start}`;

                  return (
                    <div key={key} className="border-bottom p-3">
                      <h6 className="mb-1">
                        <strong>
                          {person.firstName} {person.lastName}
                        </strong>
                      </h6>
                      <p className="text-muted small mb-1">
                        <strong>ID:</strong> {employee.businessEntityID ?? "—"}
                      </p>
                      <p className="text-muted small mb-1">
                        <strong>Departamento:</strong> {getDepartmentName(h)}
                      </p>
                      <p className="text-muted small mb-1">
                        <strong>Grupo:</strong> {getGroupName(h)}
                      </p>
                      <p className="text-muted small mb-1">
                        <strong>Início:</strong> {formatDate(start)}
                      </p>
                      <p className="text-muted small mb-2">
                        <strong>Fim:</strong> {formatDate(end)}
                      </p>
                      <div className="d-flex gap-2">
                        <button
                          className="btn btn-sm btn-outline-primary"
                          onClick={() => openEdit(h)}
                        >
                          Editar
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Empty state */}
              {!pageItems.length && (
                <div className="p-4">
                  <p className="text-muted mb-0">
                    Sem resultados para <strong>{rawSearch || "…"}</strong>.
                    Tenta outro termo.
                  </p>
                </div>
              )}

              {/* Pagination */}
              {!!pageItems.length && (
                <div className="border-top p-3 d-flex flex-wrap gap-2 justify-content-between align-items-center">
                  <div className="d-flex align-items-center gap-2">
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === 1}
                      onClick={() => setCurrentPage((p) => p - 1)}
                    >
                      ← Anterior
                    </button>
                    <span className="text-muted small">
                      Página {currentPage} de {totalPages}
                    </span>
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      disabled={currentPage === totalPages}
                      onClick={() => setCurrentPage((p) => p + 1)}
                    >
                      Próxima →
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {fetchError && (
        <div className="alert alert-danger mt-3">{fetchError}</div>
      )}

      {/* Modal Create/Edit */}
      {action.open && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          role="dialog"
          style={{ background: "rgba(0,0,0,0.5)" }}
          aria-modal="true"
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">
                  {action.mode === "create"
                    ? "Criar novo registo"
                    : "Editar registo"}
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={closeAction}
                  aria-label="Fechar"
                />
              </div>
              <div className="modal-body">
                {action.error && (
                  <div className="alert alert-danger">{action.error}</div>
                )}

                <div className="row g-3">
                  {action.mode === "create" ? (
                    <>
                      <div className="col-6">
                        <label className="form-label">ID - Funcionário</label>
                        <input
                          type="number"
                          className="form-control"
                          value={action.form.businessEntityID}
                          onChange={(e) =>
                            setAction((s) => ({
                              ...s,
                              form: {
                                ...s.form,
                                businessEntityID: e.target.value,
                              },
                            }))
                          }
                        />
                      </div>

                      {/* DepartmentID como dropdown (derivado dos histories) */}
                      <div className="col-6">
                        <label className="form-label">Departamento</label>
                        <select
                          className="form-select"
                          value={action.form.departmentID}
                          onChange={(e) =>
                            setAction((s) => ({
                              ...s,
                              form: { ...s.form, departmentID: e.target.value },
                            }))
                          }
                        >
                          <option value="">— Seleciona departamento —</option>
                          {departments.map((d) => (
                            <option
                              key={String(d.departmentID)}
                              value={String(d.departmentID)}
                            >
                              {d.name}
                            </option>
                          ))}
                        </select>
                      </div>

                      {/* ShiftID como dropdown fixo */}
                      <div className="col-6">
                        <label className="form-label">Turno</label>
                        <select
                          className="form-select"
                          value={action.form.shiftID}
                          onChange={(e) =>
                            setAction((s) => ({
                              ...s,
                              form: { ...s.form, shiftID: e.target.value },
                            }))
                          }
                        >
                          <option value="">— Seleciona turno —</option>
                          <option value="1">Manhã</option>
                          <option value="2">Tarde</option>
                          <option value="3">Noite</option>
                        </select>
                      </div>

                      <div className="col-6">
                        <label className="form-label">Data Início</label>
                        <input
                          type="date"
                          className="form-control"
                          value={action.form.startDate}
                          onChange={(e) =>
                            setAction((s) => ({
                              ...s,
                              form: { ...s.form, startDate: e.target.value },
                            }))
                          }
                        />
                      </div>

                      <div className="col-6">
                        <label className="form-label">
                          Data Fim (opcional)
                        </label>
                        <input
                          type="date"
                          className="form-control"
                          value={action.form.endDate}
                          onChange={(e) =>
                            setAction((s) => ({
                              ...s,
                              form: { ...s.form, endDate: e.target.value },
                            }))
                          }
                        />
                      </div>
                    </>
                  ) : (
                    <>
                      {/* Chaves (read-only) */}
                      <div className="col-6">
                        <label className="form-label">BusinessEntityID</label>
                        <input
                          className="form-control"
                          value={action.keys.businessEntityID}
                          disabled
                          readOnly
                        />
                      </div>
                      

                      {/* Mostrar nome do Departamento resolvido por ID */}
                      <div className="col-6">
                        <label className="form-label">Departamento</label>
                        <input
                          className="form-control"
                          value={resolveDepartmentName(
                            action.keys.departmentID
                          )}
                          disabled
                          readOnly
                        />
                      </div>

                      

                      {/* Mostrar label do Shift */}
                      <div className="col-6">
                        <label className="form-label">Turno</label>
                        <input
                          className="form-control"
                          value={resolveShiftLabel(action.keys.shiftID)}
                          disabled
                          readOnly
                        />
                      </div>

                      <div className="col-6">
                        <label className="form-label">
                          Data Início
                        </label>
                        <input
                          className="form-control"
                          value={formatDate(action.keys.startDate)}
                          disabled
                          readOnly
                        />
                      </div>

                      {/* Campo editável */}
                      <div className="col-6">
                        <label className="form-label">Data Fim</label>
                        <input
                          type="date"
                          className="form-control"
                          value={
                            action.form.endDate
                              ? action.form.endDate.substring(0, 10)
                              : ""
                          }
                          onChange={(e) =>
                            setAction((s) => ({
                              ...s,
                              form: {
                                ...s.form,
                                endDate: dateInputToIsoMidnight(e.target.value),
                              },
                            }))
                          }
                        />
                      </div>
                    </>
                  )}
                </div>
              </div>
              <div className="modal-footer">
                <button
                  className="btn btn-outline-secondary"
                  onClick={closeAction}
                >
                  Cancelar
                </button>
                <button
                  className="btn btn-primary"
                  onClick={submitAction}
                  disabled={
                    action.loading ||
                    (action.mode === "create" &&
                      (!action.form.departmentID || !action.form.shiftID))
                  }
                >
                  {action.loading
                    ? action.mode === "create"
                      ? "A criar..."
                      : "A guardar..."
                    : action.mode === "create"
                    ? "Criar registo"
                    : "Guardar alterações"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
