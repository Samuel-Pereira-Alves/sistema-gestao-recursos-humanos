
export default function Pagination({ currentPage, totalPages, setPage }) {
  const prev = () => setPage(p => Math.max(1, p - 1));
  const next = () => setPage(p => Math.min(totalPages, p + 1));
  const goTo = (page) =>
    setPage(() => Math.min(Math.max(1, Number(page) || 1), totalPages));

  return (
    <div className="border-top p-2">
      <div className="d-flex justify-content-between align-items-center">
        <button
          className="btn btn-sm btn-outline-secondary"
          disabled={currentPage === 1}
          onClick={prev}
          type="button"
        >
          ← Anterior
        </button>

        <span className="text-muted small">
          Página {currentPage} de {totalPages}
        </span>

        <button
          className="btn btn-sm btn-outline-secondary"
          disabled={currentPage === totalPages}
          onClick={next}
          type="button"
        >
          Próxima →
        </button>
      </div>
    </div>
  );
}
