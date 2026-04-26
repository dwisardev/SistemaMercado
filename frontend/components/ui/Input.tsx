import { forwardRef, InputHTMLAttributes, useState } from 'react';
import { EyeIcon, EyeSlashIcon } from '@heroicons/react/20/solid';
import { cn } from '@/lib/utils';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  icon?: React.ReactNode;
}

const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, icon, className, id, type, ...props }, ref) => {
    const [showPwd, setShowPwd] = useState(false);
    const isPassword = type === 'password';
    const resolvedType = isPassword ? (showPwd ? 'text' : 'password') : type;

    return (
      <div className="flex flex-col gap-1">
        {label && (
          <label htmlFor={id} className="text-sm font-medium text-gray-700">
            {label}
          </label>
        )}
        <div className="relative">
          {icon && (
            <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 w-4 h-4">
              {icon}
            </span>
          )}
          <input
            ref={ref}
            id={id}
            type={resolvedType}
            className={cn(
              'w-full rounded-lg border border-gray-300 bg-white text-gray-900',
              'px-3 py-2 text-sm shadow-sm',
              'placeholder:text-gray-400',
              'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500',
              'disabled:bg-gray-50 disabled:text-gray-500',
              !!icon && 'pl-9',
              isPassword && 'pr-10',
              error && 'border-red-500 focus:ring-red-500',
              className
            )}
            {...props}
          />
          {isPassword && (
            <button
              type="button"
              tabIndex={-1}
              onClick={() => setShowPwd((v) => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition"
            >
              {showPwd
                ? <EyeSlashIcon className="w-4 h-4" />
                : <EyeIcon className="w-4 h-4" />}
            </button>
          )}
        </div>
        {error && <p className="text-xs text-red-600">{error}</p>}
      </div>
    );
  }
);
Input.displayName = 'Input';
export default Input;
