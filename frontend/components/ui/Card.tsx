import { cn } from '@/lib/utils';

interface CardProps {
  children: React.ReactNode;
  className?: string;
  title?: string;
}

export default function Card({ children, className, title }: CardProps) {
  return (
    <div className={cn('bg-white rounded-xl border border-gray-200 shadow-sm', className)}>
      {title && (
        <div className="px-6 py-4 border-b border-gray-100">
          <h2 className="text-base font-semibold text-gray-800">{title}</h2>
        </div>
      )}
      <div className="p-6">{children}</div>
    </div>
  );
}
