import React from "react";

/**
 * Componente de Tabela Reutilizável
 * 
 * @param {Array} columns - Array de objetos com { key, label }
 * @param {Array} data - Array de objetos com os dados
 * @param {Function} renderCell - Função opcional para customizar células
 * @param {String} emptyMessage - Mensagem quando não há dados
 */
export default function ReusableTable({ 
  columns = [], 
  data = [], 
  renderCell,
  emptyMessage = "Sem registos" 
}) {
  
  // Função padrão para renderizar célula
  const defaultRenderCell = (item, column) => {
    return item[column.key] ?? "—";
  };

  const cellRenderer = renderCell || defaultRenderCell;

  return (
    <>
      {/* Desktop Table */}
      <div className="table-responsive d-none d-md-block">
        <table className="table table-hover mb-0">
          <thead className="table-light">
            <tr>
              {columns.map((col) => (
                <th key={col.key} className="px-4 py-3">
                  {col.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-4 text-center text-muted">
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              data.map((item, idx) => (
                <tr key={item.key || idx}>
                  {columns.map((col) => (
                    <td key={col.key} className="px-4 py-3">
                      {cellRenderer(item, col)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Mobile Cards */}
      <div className="d-md-none">
        {data.length === 0 ? (
          <div className="text-center p-3 text-muted">{emptyMessage}</div>
        ) : (
          data.map((item, idx) => (
            <div key={item.key || idx} className="border-bottom p-3">
              {columns.map((col) => (
                <div key={col.key} className="mb-2">
                  <strong className="text-muted small d-block">{col.label}:</strong>
                  <span>{cellRenderer(item, col)}</span>
                </div>
              ))}
            </div>
          ))
        )}
      </div>
    </>
  );
}