'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { cn } from '@/lib/utils';
import { 
  HomeIcon, ChartBarIcon, BuildingStorefrontIcon, 
  CreditCardIcon, DocumentTextIcon, ClipboardDocumentListIcon,
  TagIcon, ChartBarSquareIcon, UsersIcon, BellIcon, UserIcon
} from '@heroicons/react/24/outline';

const navItems = [
  { href: '/mi-cuenta', label: 'Mi Cuenta', icon: HomeIcon, roles: ['Dueno'] },
  { href: '/dashboard', label: 'Dashboard', icon: ChartBarIcon, roles: ['Admin', 'Cajero'] },
  { href: '/puestos', label: 'Puestos', icon: BuildingStorefrontIcon, roles: ['Admin', 'Cajero'] },
  { href: '/pagos', label: 'Registrar Pago', icon: CreditCardIcon, roles: ['Admin', 'Cajero'] },
  { href: '/historial-pagos', label: 'Historial Pagos', icon: DocumentTextIcon, roles: ['Admin', 'Cajero'] },
  { href: '/deudas', label: 'Carga de Deudas', icon: ClipboardDocumentListIcon, roles: ['Admin'] },
  { href: '/conceptos', label: 'Conceptos de Cobro', icon: TagIcon, roles: ['Admin'] },
  { href: '/reportes', label: 'Reportes', icon: ChartBarSquareIcon, roles: ['Admin', 'Cajero'] },
  { href: '/usuarios', label: 'Usuarios', icon: UsersIcon, roles: ['Admin'] },
  { href: '/notificaciones', label: 'Notificaciones', icon: BellIcon, roles: ['Admin', 'Cajero', 'Dueno'] },
  { href: '/perfil',        label: 'Mi Perfil',       icon: UserIcon, roles: ['Admin', 'Cajero', 'Dueno'] },
] as const;

export default function Sidebar() {
  const pathname = usePathname();
  const { user, hasRole, logout } = useAuth();

  const visibleItems = navItems.filter((item) =>
    hasRole(...(item.roles as unknown as ('Admin' | 'Cajero' | 'Dueno')[]))
  );

  return (
    <aside className="w-64 bg-gray-900 text-white flex flex-col h-screen sticky top-0">
      {/* Logo */}
      <div className="px-6 py-5 border-b border-gray-700">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 bg-blue-500 rounded-lg flex items-center justify-center text-sm font-bold">M</div>
          <div>
            <p className="font-semibold text-sm">MercaGest</p>
            <p className="text-xs text-gray-400">Sistema de Mercado</p>
          </div>
        </div>
      </div>

      {/* Nav */}
      <nav className="flex-1 px-3 py-4 overflow-y-auto">
        <ul className="space-y-1">
          {visibleItems.map((item) => {
            const active = pathname === item.href || pathname.startsWith(item.href + '/');
            const Icon = item.icon;
            return (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className={cn(
                    'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm transition-colors',
                    active
                      ? 'bg-blue-600 text-white'
                      : 'text-gray-300 hover:bg-gray-800 hover:text-white'
                  )}
                >
                  <Icon className="w-5 h-5 flex-shrink-0" />
                  {item.label}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>

      {/* User info */}
      <div className="px-4 py-4 border-t border-gray-700">
        <div className="flex items-center gap-3 mb-3">
          <div className="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center text-sm font-semibold">
            {user?.nombreCompleto?.[0]?.toUpperCase()}
          </div>
          <div className="overflow-hidden">
            <p className="text-sm font-medium truncate">{user?.nombreCompleto}</p>
            <p className="text-xs text-gray-400">{user?.rol}</p>
          </div>
        </div>
        <button
          onClick={logout}
          className="w-full text-left text-xs text-gray-400 hover:text-red-400 transition-colors px-1 py-1"
        >
          Cerrar sesión →
        </button>
      </div>
    </aside>
  );
}
