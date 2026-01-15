import ReadOnlyField from "../ReadOnlyField/ReadOnlyField";

export default function EmployeeDetails({ employee }) {
  if (!employee?.person) return null;

  return (
    <div className="text-muted small">
      <ReadOnlyField
        label="Funcionário"
        value={`${employee.person.firstName} ${employee.person.lastName}`}
      />
      <ReadOnlyField label="ID" value={employee.businessEntityID} />
    </div>
  );
}
