interface PaginationProps {
  page: number;
  totalPages: number;
  total: number;
  pageSize: number;
  onPage: (p: number) => void;
}

export default function Pagination({ page, totalPages, total, pageSize, onPage }: PaginationProps) {
  if (totalPages <= 1) return null;

  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  return (
    <div className="flex items-center justify-between mt-4 text-sm text-gray-500">
      <span>{from}–{to} de {total}</span>
      <div className="flex gap-1">
        <button
          onClick={() => onPage(1)}
          disabled={page === 1}
          className="px-2 py-1 rounded border border-gray-200 disabled:opacity-30 hover:bg-gray-50 transition-colors"
        >
          «
        </button>
        <button
          onClick={() => onPage(page - 1)}
          disabled={page === 1}
          className="px-2 py-1 rounded border border-gray-200 disabled:opacity-30 hover:bg-gray-50 transition-colors"
        >
          ‹
        </button>
        <span className="px-3 py-1 rounded border border-blue-500 bg-blue-50 text-blue-700 font-medium">
          {page} / {totalPages}
        </span>
        <button
          onClick={() => onPage(page + 1)}
          disabled={page === totalPages}
          className="px-2 py-1 rounded border border-gray-200 disabled:opacity-30 hover:bg-gray-50 transition-colors"
        >
          ›
        </button>
        <button
          onClick={() => onPage(totalPages)}
          disabled={page === totalPages}
          className="px-2 py-1 rounded border border-gray-200 disabled:opacity-30 hover:bg-gray-50 transition-colors"
        >
          »
        </button>
      </div>
    </div>
  );
}
