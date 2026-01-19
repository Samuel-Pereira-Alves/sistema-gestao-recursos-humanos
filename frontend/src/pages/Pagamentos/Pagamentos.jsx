
import React, { useEffect, useMemo, useState, useCallback } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import BackButton from "../../components/Button/BackButton";
import Pagination from "../../components/Pagination/Pagination";
import Loading from "../../components/Loading/Loading";
import ReadOnlyField from "../../components/ReadOnlyField/ReadOnlyField";
import EditPaymentModal from "../../components/Modals/EditPaymentModal";
import CreatePaymentModal from "../../components/Modals/CreatePaymentModal";
import {
  freqLabel,
  formatCurrencyEUR,
  formatDate,
  dateInputToIsoMidnight,
  filterPagamentos,
  normalizeApiError,
} from "../../utils/Utils";
import {
  listPagamentosFlattened,
  patchPayHistory,
  deletePayHistory,
  createPayHistory
} from "../../Service/pagamentosService";
import { getAllEmployees } from "../../Service/employeeService";
import { addNotificationForUser } from "../../utils/notificationBus";

export default function Pagamentos() {
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [serverTotalPages, setServerTotalPages] = useState(1);
  const [employees, setEmployees] = useState([]);
  const [employeesDrop, setEmployeesDrop] = useState([]);
  const [pagamentos, setPagamentos] = useState([]);
  const itemsPerPage = 5;

  const reload = useCallback(async () => {
    setLoading(true);
    setFetchError(null);
    try {
      const token = localStorage.getItem("authToken");
      const { employees: emps, pagamentos: pays, meta } =
        await listPagamentosFlattened(currentPage, itemsPerPage);

      const myId = Number(localStorage.getItem("businessEntityId"));
      const employeesExceptActual = (emps ?? [])
        .filter((e) => e.businessEntityID !== myId)
        .sort((a, b) =>
          (a.person?.firstName || "").localeCompare(b.person?.firstName || "")
        );
      const employeedrop = await getAllEmployees(token);
      setEmployeesDrop(employeedrop);
      setEmployees(employeesExceptActual);
      setPagamentos(pays ?? []);
      setServerTotalPages(meta?.totalPages ?? 1);
    } catch (error) {
      console.error(error);
      setFetchError(error.message || "Erro a carregar dados.");
    } finally {
      setLoading(false);
    }
  }, [currentPage, itemsPerPage]);

  useEffect(() => {
    reload();
  }, [reload]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm]);

  const pagamentosFiltrados = useMemo(
    () => filterPagamentos(pagamentos, searchTerm),
    [pagamentos, searchTerm]
  );

  const currentPagamentos = pagamentosFiltrados;

  // Estados para edição
  const [editOpen, setEditOpen] = useState(false);
  const [editLoading, setEditLoading] = useState(false);
  const [editError, setEditError] = useState(null);
  const [editKeys, setEditKeys] = useState({
    businessEntityID: "",
    rateChangeDate: "",
  });
  const [editForm, setEditForm] = useState({ rate: "", payFrequency: "1" });

  const openEdit = (p) => {
    setEditKeys({
      businessEntityID: String(
        p.businessEntityID ?? p.employee?.businessEntityID ?? ""
      ),
      rateChangeDate: p.rateChangeDate ?? "",
    });
    setEditForm({
      rate: p.rate ?? "",
      payFrequency: String(p.payFrequency ?? "1"),
    });
    setEditError(null);
    setEditOpen(true);
  };

  const submitEdit = async () => {
    try {
      setEditLoading(true);
      setEditError(null);
      await patchPayHistory(editKeys.businessEntityID, editKeys.rateChangeDate, {
        rate: Number(editForm.rate),
        payFrequency: Number(editForm.payFrequency),
      });
      await reload();
      addNotificationForUser(
        "O seu registo de Pagamento foi atualizado.",
        editKeys.businessEntityID,
        { type: "PAYMENT" }
      );
      setEditOpen(false);
    } catch (e) {
      setEditError(e.message || "Erro ao editar registo.");
    } finally {
      setEditLoading(false);
    }
  };

  // Eliminar registo
  const [deleteLoadingId, setDeleteLoadingId] = useState(null);
  const handleDelete = async (p) => {
    const businessEntityID =
      p.businessEntityID ?? p.employee?.businessEntityID ?? "";
    const rateChangeDate = p.rateChangeDate ?? "";
    if (!businessEntityID || !rateChangeDate)
      return alert("Chaves do registo em falta.");
    if (
      !window.confirm(
        `Eliminar registo do colaborador ${p.employee?.person?.firstName} ${p.employee?.person?.lastName}?`
      )
    )
      return;
    try {
      setDeleteLoadingId(`${businessEntityID}|${rateChangeDate}`);
      await deletePayHistory(businessEntityID, rateChangeDate);
      await reload();
      addNotificationForUser(
        "O seu registo de Pagamento foi eliminado.",
        businessEntityID,
        { type: "PAYMENT" }
      );
    } catch (e) {
      alert(e.message || "Erro ao eliminar registo.");
    } finally {
      setDeleteLoadingId(null);
    }
  };

  // Criar registo
  const [createOpen, setCreateOpen] = useState(false);
  const [createLoading, setCreateLoading] = useState(false);
  const [createError, setCreateError] = useState(null);
  const [createForm, setCreateForm] = useState({
    businessEntityID: "",
    rateChangeDate: "",
    rate: "",
    payFrequency: "1",
  });

  const submitCreate = async () => {
    try {
      setCreateLoading(true);
      setCreateError(null);
      if (
        !createForm.businessEntityID ||
        !createForm.rateChangeDate ||
        !createForm.rate
      )
        throw new Error("Preenche todos os campos.");
      const body = {
        businessEntityID: Number(createForm.businessEntityID),
        rateChangeDate: dateInputToIsoMidnight(createForm.rateChangeDate),
        rate: Number(createForm.rate),
        payFrequency: Number(createForm.payFrequency),
      };
      await createPayHistory(body);
      await reload();
      addNotificationForUser(
        "Foi criado um novo registo de Pagamento.",
        body.businessEntityID,
        { type: "PAYMENT" }
      );
      setCreateOpen(false);
      setCreateForm({
        businessEntityID: "",
        rateChangeDate: "",
        rate: "",
        payFrequency: "1",
      });
    } catch (err) {
      setCreateError(normalizeApiError(err));
    } finally {
      setCreateLoading(false);
    }
  };

  return (
    <div className="container mt-4">
      <BackButton />
      <div className="mb-4 d-flex justify-content-between align-items-center">
        <h1 className="h3 mb-1">Gestão de Pagamentos</h1>
        {!loading && (
          <button
            className="btn btn-sm btn-primary"
            onClick={() => setCreateOpen(true)}
          >
            Criar registo
          </button>
        )}
      </div>

      {/* Search */}
      <div className="card mb-3 border-0 shadow-sm">
        <div className="card-body">
          {loading ? (
            <Loading text="Carregando pagamentos..." />
          ) : (
            <input
              type="text"
              className="form-control"
              placeholder="Procurar por ID, colaborador, frequência, valor ou data..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          )}
        </div>
      </div>

      {/* Content */}
      <div className="card shadow-sm">
        <div className="card-body p-0">
          {loading ? (
            <Loading text="Carregando lista..." />
          ) : (
            <>
              {/* Desktop Table */}
              <div className="table-responsive d-none d-md-block">
                <table className="table table-hover mb-0">
                  <thead className="table-light">
                    <tr>
                      <th>Colaborador</th>
                      <th>Valor</th>
                      <th>Data</th>
                      <th className="text-center">Frequência</th>
                      <th className="text-end">Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {currentPagamentos.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="text-center text-muted">
                          Sem registos
                        </td>
                      </tr>
                    ) : (
                      currentPagamentos.map((p) => {
                        const key = `${
                          p.businessEntityID ?? p.employee?.businessEntityID
                        }|${p.rateChangeDate}`;
                        const deleting = deleteLoadingId === key;
                        return (
                          <tr key={p.payHistoryId ?? key}>
                            <td>
                              {p.employee?.person?.firstName}{" "}
                              {p.employee?.person?.lastName}
                              <div className="small text-muted">
                                ID: {p.employee?.businessEntityID ?? "—"}
                              </div>
                            </td>
                            <td>{formatCurrencyEUR(p.rate)}</td>
                            <td>{formatDate(p.rateChangeDate)}</td>
                            <td className="text-center">
                              {freqLabel(p.payFrequency)}
                            </td>
                            <td className="text-end">
                              <button
                                className="btn btn-outline-primary btn-sm"
                                onClick={() => openEdit(p)}
                              >
                                Editar
                              </button>
                              <button
                                className="btn btn-outline-danger btn-sm"
                                disabled={deleting}
                                onClick={() => handleDelete(p)}
                              >
                                {deleting ? "A eliminar..." : "Eliminar"}
                              </button>
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>

              {/* Mobile Cards */}
              <div className="d-md-none">
                {currentPagamentos.length === 0 ? (
                  <div className="text-center p-3 text-muted">Sem registos</div>
                ) : (
                  currentPagamentos.map((p) => {
                    const key = `${
                      p.businessEntityID ?? p.employee?.businessEntityID
                    }|${p.rateChangeDate}`;
                    const deleting = deleteLoadingId === key;
                    return (
                      <div key={p.payHistoryId ?? key} className="border-bottom p-3">
                        <h6>
                          <strong>
                            {p.employee?.person?.firstName}{" "}
                            {p.employee?.person?.lastName}
                          </strong>
                        </h6>
                        <ReadOnlyField
                          label="ID"
                          value={p.employee?.businessEntityID ?? "—"}
                        />
                        <ReadOnlyField
                          label="Valor"
                          value={formatCurrencyEUR(p.rate)}
                        />
                        <ReadOnlyField
                          label="Data"
                          value={formatDate(p.rateChangeDate)}
                        />
                        <ReadOnlyField
                          label="Frequência"
                          value={freqLabel(p.payFrequency)}
                        />
                        <div className="d-flex gap-2 mt-2">
                          <button
                            className="btn btn-sm btn-outline-primary"
                            onClick={() => openEdit(p)}
                          >
                            Editar
                          </button>
                          <button
                            className="btn btn-sm btn-outline-danger"
                            disabled={deleting}
                            onClick={() => handleDelete(p)}
                          >
                            {deleting ? "A eliminar..." : "Eliminar"}
                          </button>
                        </div>
                      </div>
                    );
                  })
                )}
              </div>

              {/* Pagination (usa o total do back) */}
              <Pagination
                currentPage={currentPage}
                totalPages={serverTotalPages}
                setPage={setCurrentPage}
              />
            </>
          )}
        </div>
      </div>

      {fetchError && (
        <div className="alert alert-danger mt-3">{fetchError}</div>
      )}

      <EditPaymentModal
        open={editOpen}
        onClose={() => setEditOpen(false)}
        onSubmit={submitEdit}
        loading={editLoading}
        error={editError}
        keys={editKeys}
        form={editForm}
        setForm={setEditForm}
      />

      <CreatePaymentModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onSubmit={submitCreate}
        loading={createLoading}
        error={createError}
        form={createForm}
        setForm={setCreateForm}
        employees={employeesDrop}
      />
    </div>
  );
}
