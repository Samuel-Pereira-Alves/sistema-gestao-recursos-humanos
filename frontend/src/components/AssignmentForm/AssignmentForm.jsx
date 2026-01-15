
// src/components/AssignmentForm.jsx
import React from "react";

export default function AssignmentForm({
  mode,                 // "create" | "edit"
  action,               // objeto com keys, form, error...
  setAction,
  employees = [],
  departments = [],
  resolveDepartmentName,
  resolveShiftLabel,
  formatDate,
  dateInputToIsoMidnight,
}) {
  return (
    <div className="row g-3">
      {mode === "create" ? (
        <>
          <div className="col-6">
            <label className="form-label">Funcionário</label>
            <select
              className="form-select"
              value={action.form.businessEntityID ?? ""}
              onChange={(e) =>
                setAction((s) => ({
                  ...s,
                  form: { ...s.form, businessEntityID: e.target.value },
                }))
              }
            >
              {employees.map((emp) => {
                const id = emp.businessEntityID ?? emp.id;
                const first = emp.person?.firstName ?? "";
                const middle = emp.person?.middleName ?? "";
                const last = emp.person?.lastName ?? "";
                const fullName =
                  [first, middle, last].filter(Boolean).join(" ") || "Sem nome";

                return (
                  <option key={id} value={id}>
                    {fullName} {emp.jobTitle ? `— ${emp.jobTitle}` : ""}
                  </option>
                );
              })}
            </select>
          </div>

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
                <option key={String(d.departmentID)} value={String(d.departmentID)}>
                  {d.name}
                </option>
              ))}
            </select>
          </div>

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
            <label className="form-label">Data Fim (opcional)</label>
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
          <div className="col-6">
            <label className="form-label">BusinessEntityID</label>
            <input
              className="form-control"
              value={action.keys.businessEntityID}
              disabled
              readOnly
            />
          </div>

          <div className="col-6">
            <label className="form-label">Departamento</label>
            <input
              className="form-control"
              value={resolveDepartmentName(action.keys.departmentID)}
              disabled
              readOnly
            />
          </div>

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
            <label className="form-label">Data Início</label>
            <input
              className="form-control"
              value={formatDate(action.keys.startDate)}
              disabled
              readOnly
            />
          </div>

          <div className="col-6">
            <label className="form-label">Data Fim</label>
            <input
              type="date"
              className="form-control"
              value={
                action.form.endDate ? action.form.endDate.substring(0, 10) : ""
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
  );
}
