
import React from "react";
import Select from "react-select";

export default function CreatePaymentModal({
  open,
  onClose,
  onSubmit,
  loading,
  error,
  form,
  setForm,
  employees = [],
}) {
  if (!open) return null;

  // Preparar opções para React-Select
  const options = employees.map((emp) => {
    const id = emp.businessEntityID ?? emp.id;
    const fullName = [
      emp.person?.firstName,
      emp.person?.middleName,
      emp.person?.lastName,
    ]
      .filter(Boolean)
      .join(" ") || "Sem nome";

    return { value: id, label: fullName };
  });

  return (
    <div
      className="modal fade show d-block"
      style={{ background: "rgba(0,0,0,0.5)" }}
    >
      <div className="modal-dialog">
        <div className="modal-content">
          {/* Header */}
          <div className="modal-header">
            <h5 className="modal-title">Criar novo registo</h5>
            <button type="button" className="btn-close" onClick={onClose} />
          </div>

          {/* Body */}
          <div className="modal-body">
            {error && <div className="alert alert-danger">{error}</div>}

            <div className="row g-3">
              {/* Funcionário */}
              <div className="col-6">
                <label className="form-label">Funcionário</label>
                <Select
                  options={options}
                  value={
                    options.find((opt) => opt.value === form.businessEntityID) ||
                    null
                  }
                  onChange={(selected) =>
                    setForm((f) => ({
                      ...f,
                      businessEntityID: selected?.value || "",
                    }))
                  }
                  placeholder="Funcionário..."
                  isClearable
                  classNamePrefix="react-select"
                />
              </div>

              {/* Data */}
              <div className="col-6">
                <label className="form-label">Data</label>
                <input
                  type="date"
                  className="form-control"
                  value={form.rateChangeDate}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, rateChangeDate: e.target.value }))
                  }
                />
              </div>

              {/* Valor */}
              <div className="col-6">
                <label className="form-label">Valor</label>
                <input
                  type="number"
                  step="0.01"
                  className="form-control"
                  value={form.rate}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, rate: e.target.value }))
                  }
                />
              </div>

              {/* Frequência */}
              <div className="col-6">
                <label className="form-label">Frequência</label>
                <select
                  className="form-select"
                  value={form.payFrequency}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, payFrequency: e.target.value }))
                  }
                >
                  <option value="1">Mensal</option>
                  <option value="2">Quinzenal</option>
                </select>
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="modal-footer">
            <button
              className="btn btn-outline-secondary"
              onClick={onClose}
            >
              Cancelar
            </button>
            <button
              className="btn btn-primary"
              onClick={onSubmit}
              disabled={loading}
            >
              {loading ? "A criar..." : "Criar registo"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
