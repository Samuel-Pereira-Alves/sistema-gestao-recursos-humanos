import React from "react";
import Select from "react-select";
import ReadOnlyField from "../ReadOnlyField/ReadOnlyField";

export default function AssignmentForm({
  mode,                 
  action,               
  setAction,
  employees = [],
  departments = [],
  resolveDepartmentName,
  resolveShiftLabel,
  formatDate,
  dateInputToIsoMidnight,
}) {
  
  const employeeOptions = employees.map((emp) => {
    const id = emp.businessEntityID ?? emp.id;
    const fullName = [
      emp.person?.firstName,
      emp.person?.middleName,
      emp.person?.lastName,
    ]
      .filter(Boolean)
      .join(" ") || "Sem nome";

    return {
      value: id,
      label: emp.jobTitle ? `${fullName} — ${emp.jobTitle}` : fullName,
    };
  });

  return (
    <div className="row g-3">
      {mode === "create" ? (
        <>
          {/* Funcionário */}
          <div className="col-6">
            <label className="form-label">Funcionário</label>
            <Select
              options={employeeOptions}
              value={
                employeeOptions.find(
                  (opt) => String(opt.value) === String(action.form.businessEntityID)
                ) || null
              }
              onChange={(selected) =>
                setAction((s) => ({
                  ...s,
                  form: { ...s.form, businessEntityID: selected?.value || "" },
                }))
              }
              placeholder="Selecione um funcionário..."
              isClearable
            />
          </div>

          {/* Departamento */}
          <div className="col-6">
            <label className="form-label">Departamento</label>
            <select
              className="form-select"
              size={4}
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

          {/* Turno */}
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

          {/* Data Início */}
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

          {/* Data Fim */}
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
          <div className="col-12">
            <ReadOnlyField label="ID Funcionário" value={action.keys.businessEntityID} />
          </div>

          <div className="col-12">
            <ReadOnlyField
              label="Departamento"
              value={resolveDepartmentName(action.keys.departmentID)}
            />
          </div>

          <div className="col-12">
            <ReadOnlyField
              label="Turno"
              value={resolveShiftLabel(action.keys.shiftID)}
            />
          </div>

          <div className="col-12">
            <ReadOnlyField
              label="Data Início"
              value={formatDate(action.keys.startDate)}
            />
          </div>

          <div className="col-6">
            <label className="form-label">Data Fim</label>
            <input
              type="date"
              className="form-control"
              value={action.form.endDate ? action.form.endDate.substring(0, 10) : ""}
              onChange={(e) =>
                setAction((s) => ({
                  ...s,
                  form: {
                    ...s.form,
                    endDate: e.target.value
                      ? dateInputToIsoMidnight(e.target.value)
                      : "",
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
