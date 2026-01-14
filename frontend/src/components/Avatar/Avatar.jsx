export default function Avatar({ name }) {
  const initial = name?.charAt(0) ?? "?";
  return (
    <div
      className="rounded-circle bg-secondary bg-opacity-25 d-flex align-items-center justify-content-center"
      style={{ width: 56, height: 56 }}
    >
      <span className="text-muted fw-bold">{initial}</span>
    </div>
  );
}
