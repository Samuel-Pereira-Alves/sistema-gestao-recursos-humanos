export default function ReadOnlyField({ label, value }) {
  return (
    <p className="mb-1">
      <strong>{label}:</strong> {value}
    </p>
  );
}