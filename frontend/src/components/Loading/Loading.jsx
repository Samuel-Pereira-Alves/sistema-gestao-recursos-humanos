export default function Loading({ text = "Carregando..." }) {
  return (
    <div className="container mt-5 text-center text-muted">
      <div className="spinner-border text-secondary mb-3" role="status" />
      <p className="mb-0">{text}</p>
    </div>
  );
}
