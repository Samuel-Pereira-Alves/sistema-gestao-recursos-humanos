
import { useEffect, useMemo, useState } from "react";

export function usePagination(items = [], itemsPerPage = 5) {
  const [currentPage, setCurrentPage] = useState(1);

  // Total de páginas calculado sempre que items ou itemsPerPage mudam
  const totalPages = useMemo(
    () => Math.max(1, Math.ceil((items?.length ?? 0) / itemsPerPage)),
    [items, itemsPerPage]
  );

  // Itens da página atual 
  const currentItems = useMemo(() => {
    const start = (currentPage - 1) * itemsPerPage;
    const end = start + itemsPerPage;
    return (items ?? []).slice(start, end);
  }, [items, currentPage, itemsPerPage]);

  // Se a lista mudar e ficar menor, assegura que a página atual não fica fora do intervalo
  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(totalPages);
    }
  }, [totalPages, currentPage]);

  return { currentPage, setCurrentPage, currentItems, totalPages };
}
