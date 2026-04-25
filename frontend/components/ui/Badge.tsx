import { cn } from '@/lib/utils';

const colors = {
  green: 'bg-emerald-100 text-emerald-700',
  red: 'bg-red-100 text-red-700',
  yellow: 'bg-yellow-100 text-yellow-700',
  blue: 'bg-blue-100 text-blue-700',
  gray: 'bg-gray-100 text-gray-700',
  purple: 'bg-purple-100 text-purple-700',
};

interface BadgeProps {
  color?: keyof typeof colors;
  children: React.ReactNode;
  className?: string;
}

export default function Badge({ color = 'gray', children, className }: BadgeProps) {
  return (
    <span className={cn('inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium', colors[color], className)}>
      {children}
    </span>
  );
}
