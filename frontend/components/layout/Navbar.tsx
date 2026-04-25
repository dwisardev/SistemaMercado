'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { notificacionesApi } from '@/lib/api';
import type { Notificacion } from '@/lib/types';
import { formatDateTime } from '@/lib/utils';

export default function Navbar({ title }: { title?: string }) {
  const [notifs, setNotifs] = useState<Notificacion[]>([]);
  const [open, setOpen] = useState(false);

  const unread = notifs.filter((n) => !n.leida).length;

  useEffect(() => {
    notificacionesApi.getAll().then(setNotifs).catch(() => {});
  }, []);

  const handleMarkAll = async () => {
    try {
      await notificacionesApi.marcarTodasLeidas();
      setNotifs((prev) => prev.map((n) => ({ ...n, leida: true })));
    } catch { /* silencioso */ }
  };

  return (
    <header className="bg-white border-b border-gray-200 px-6 py-3 flex items-center justify-between sticky top-0 z-30">
      <h1 className="text-lg font-semibold text-gray-800">{title}</h1>

      {/* Campana de notificaciones */}
      <div className="relative">
        <button
          onClick={() => setOpen((p) => !p)}
          className="relative p-2 rounded-lg hover:bg-gray-100 transition-colors"
        >
          <span className="text-xl">🔔</span>
          {unread > 0 && (
            <span className="absolute top-1 right-1 bg-red-500 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center font-bold">
              {unread > 9 ? '9+' : unread}
            </span>
          )}
        </button>

        {open && (
          <>
            <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />
            <div className="absolute right-0 top-12 w-80 bg-white rounded-xl shadow-xl border border-gray-200 z-20">
              <div className="flex items-center justify-between px-4 py-3 border-b border-gray-100">
                <p className="font-semibold text-sm text-gray-800">Notificaciones</p>
                {unread > 0 && (
                  <button onClick={handleMarkAll} className="text-xs text-blue-600 hover:underline">
                    Marcar todas como leídas
                  </button>
                )}
              </div>
              <div className="max-h-72 overflow-y-auto">
                {notifs.length === 0 ? (
                  <p className="text-sm text-gray-400 text-center py-6">Sin notificaciones</p>
                ) : (
                  notifs.slice(0, 10).map((n) => (
                    <div
                      key={n.id}
                      className={`px-4 py-3 border-b border-gray-50 last:border-0 ${!n.leida ? 'bg-blue-50' : ''}`}
                    >
                      <p className="text-sm font-medium text-gray-800">{n.titulo}</p>
                      <p className="text-xs text-gray-500 mt-0.5">{n.mensaje}</p>
                      <p className="text-xs text-gray-400 mt-1">{formatDateTime(n.fechaCreacion)}</p>
                    </div>
                  ))
                )}
              </div>
              <div className="px-4 py-2 border-t border-gray-100">
                <Link
                  href="/notificaciones"
                  onClick={() => setOpen(false)}
                  className="text-xs text-blue-600 hover:underline"
                >
                  Ver todas →
                </Link>
              </div>
            </div>
          </>
        )}
      </div>
    </header>
  );
}
