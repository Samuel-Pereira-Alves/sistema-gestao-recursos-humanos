
import React from "react";
import ReadOnlyField from "../ReadOnlyField/ReadOnlyField";
import { formatDate } from "../../utils/Utils";

export default function EditPaymentModal({
  open,
  onClose,
  onSubmit,
  loading,
  error,
  keys,
  form,
  setForm,
}) {
  if (!open) return null;

  return (
    <div className="modal fade show d-block" style={{ background: "rgba(0,0,0,0.5)" }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">Editar registo</h5>
            <button type="button" className="btn-close" onClick={onClose} />
          </div>
          <div className="modal-body">
            {error && <div className="alert alert-danger">{error}</div>}
            <ReadOnlyField label="ID Funcionário" value={keys.businessEntityID} />
            <ReadOnlyField label="Data Pagamento" value={formatDate(keys.rateChangeDate)} />
            <div className="row g-3 mt-2">
              <div className="col-6">
                <label className="form-label">Valor</label>
                <input
                  type="number"
                  step="0.01"
                  className="form-control"
                  value={form.rate}
                  onChange={(e) => setForm((f) => ({ ...f, rate: e.target.value }))}
                />
              </div>
              <div className="col-6">
                <label className="form-label">Frequência</label>
                <select
                  className="form-select"
                  value={form.payFrequency}
                  onChange={(e) => setForm((f) => ({ ...f, payFrequency: e.target.value }))}
                >
                  <option value="1">Mensal</option>
                  <option value="2">Quinzenal</option>
                </select>
              </div>
            </div>
          </div>
          <div className="modal-footer">
            <button className="btn btn-outline-secondary" onClick={onClose}>Cancelar</button>
            <button className="btn btn-primary" onClick={onSubmit} disabled={loading}>
              {loading ? "A guardar..." : "Guardar alterações"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
