
// src/components/BackButton.jsx
import { useNavigate } from 'react-router-dom';

export default function BackButton({ label = 'Voltar', className = '' }) {
  const navigate = useNavigate();

  const goBack = () => {
    navigate(-1);
  };

  return (
    <button
      type="button"
      onClick={goBack}
      aria-label={label}
      className={className}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 8,
        padding: '8px 12px',
        border: '1px solid #ddd',
        borderRadius: 8,
        background: '#fff',
        color: '#222',
        cursor: 'pointer',
        marginBottom: '1rem',
      }}
    >
      {/* Ícone de seta (SVG) */}
      <svg
        xmlns="http://www.w3.org/2000/svg"
        width="18"
        height="18"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
        focusable="false"
      >
        <polyline points="15 18 9 12 15 6" />
      </svg>
      {label}
    </button>
  );
}

