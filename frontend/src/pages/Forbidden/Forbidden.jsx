
import { Link } from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";

export default function ForbiddenPage() {
  return (
    <div className="d-flex align-items-start justify-content-center min-vh-100 bg-light pt-5">
      <div className="bg-white shadow-lg rounded-4 p-5 text-center" style={{ maxWidth: "400px" }}>
        <h2 className="display-5 fw-bold text-black mb-3">Acesso Negado</h2>
        <p className="text-muted mb-4">
          Você não tem permissão para acessar esta página.
        </p>
        <Link
          to="/"
          className="btn btn-outline-black btn-lg px-4 py-2 rounded-pill shadow-sm"
        >
          Voltar para Home
        </Link>
      </div>
    </div>
  );
}