import React, { useMemo } from "react";
import AssignmentForm from "./AssignmentForm";

export default function AssignmentModal({
  action,
  setAction,
  closeAction,
  submitAction,
  employees = [],
  departments = [],
  resolveDepartmentName,
  resolveShiftLabel,
  formatDate,
  dateInputToIsoMidnight,
}) {
  if (!action.open) return null;

  const title =
    action.mode === "create" ? "Criar novo registo" : "Editar registo";

  const primaryLabel = action.loading
    ? action.mode === "create"
      ? "A criar..."
      : "A guardar..."
    : action.mode === "create"
      ? "Criar registo"
      : "Guardar alterações";

  const initialEndDate = action?.keys?.endDate ?? "";
  const currentEndDate = action?.form?.endDate ?? "";

  const normalizeDate = (d) => (d ? String(d).substring(0, 10) : "");
  const endChanged = normalizeDate(currentEndDate) !== normalizeDate(initialEndDate);

  const isValidDateOrEmpty = (val) => {
    const v = normalizeDate(val);
    return v === "" || /^\d{4}-\d{2}-\d{2}$/.test(v);
  };

  return (
    <div
      className="modal fade show d-block"
      tabIndex="-1"
      role="dialog"
      style={{ background: "rgba(0,0,0,0.5)" }}
      aria-modal="true"
    >
      <div className="modal-dialog modal-lg">
        <div className="modal-content">

          {/* Header */}
          <div className="modal-header">
            <h5 className="modal-title">{title}</h5>
            <button
              type="button"
              className="btn-close"
              onClick={closeAction}
              aria-label="Fechar"
            />
          </div>

          {/* Body */}
          <div className="modal-body">
            {action.error && (
              <div className="alert alert-danger">{action.error}</div>
            )}

            <AssignmentForm
              mode={action.mode}
              action={action}
              setAction={setAction}
              employees={employees}
              departments={departments}
              resolveDepartmentName={resolveDepartmentName}
              resolveShiftLabel={resolveShiftLabel}
              formatDate={formatDate}
              dateInputToIsoMidnight={dateInputToIsoMidnight}
              endChanged={endChanged}
              isValidDateOrEmpty={isValidDateOrEmpty}
            />
          </div>

          {/* Footer */}
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
            >
              {primaryLabel}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
