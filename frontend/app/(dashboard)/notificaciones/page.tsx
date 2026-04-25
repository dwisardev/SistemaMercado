'use client';

import { useEffect, useState } from 'react';
import { notificacionesApi } from '@/lib/api';
import type { Notificacion } from '@/lib/types';
import { formatDateTime, getAxiosErrorMessage } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Badge from '@/components/ui/Badge';
import { useToast } from '@/components/ui/Toast';

export default function NotificacionesPage() {
  const { toast } = useToast();
  const [notifs, setNotifs] = useState<Notificacion[]>([]);
  const [loading, setLoading] = useState(true);
  const [filtro, setFiltro] = useState<'todas' | 'no-leidas'>('todas');

  useEffect(() => {
    notificacionesApi.getAll()
      .then(setNotifs)
      .catch(() => toast('Error cargando notificaciones', 'error'))
      .finally(() => setLoading(false));
  }, [toast]);

  const handleMarcarLeida = async (id: string) => {
    try {
      await notificacionesApi.marcarLeida(id);
      setNotifs((prev) => prev.map((n) => n.id === id ? { ...n, leida: true } : n));
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    }
  };

  const handleMarcarTodas = async () => {
    try {
      await notificacionesApi.marcarTodasLeidas();
      setNotifs((prev) => prev.map((n) => ({ ...n, leida: true })));
      toast('Todas marcadas como leídas', 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    }
  };

  const visible = filtro === 'no-leidas' ? notifs.filter((n) => !n.leida) : notifs;
  const unread = notifs.filter((n) => !n.leida).length;

  return (
    <>
      <Navbar title="Notificaciones" />
      <div className="p-6 max-w-3xl mx-auto">
        {/* Header acciones */}
        <div className="flex items-center justify-between mb-4">
          <div className="flex gap-1 bg-gray-100 rounded-lg p-1">
            <TabBtn active={filtro === 'todas'} onClick={() => setFiltro('todas')}>
              Todas ({notifs.length})
            </TabBtn>
            <TabBtn active={filtro === 'no-leidas'} onClick={() => setFiltro('no-leidas')}>
              No leídas ({unread})
            </TabBtn>
          </div>
          {unread > 0 && (
            <Button variant="ghost" size="sm" onClick={handleMarcarTodas}>
              ✓ Marcar todas como leídas
            </Button>
          )}
        </div>

        <Card>
          {loading ? (
            <div className="space-y-3">
              {[...Array(5)].map((_, i) => <div key={i} className="h-16 bg-gray-100 rounded animate-pulse" />)}
            </div>
          ) : visible.length === 0 ? (
            <div className="text-center py-12">
              <span className="text-5xl">🔔</span>
              <p className="text-gray-400 mt-3">Sin notificaciones</p>
            </div>
          ) : (
            <div className="divide-y divide-gray-100">
              {visible.map((n) => (
                <div
                  key={n.id}
                  className={`py-4 flex items-start gap-4 ${!n.leida ? 'bg-blue-50 -mx-6 px-6' : ''}`}
                >
                  <div className={`mt-1 w-2 h-2 rounded-full flex-shrink-0 ${!n.leida ? 'bg-blue-500' : 'bg-gray-200'}`} />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <p className="text-sm font-semibold text-gray-800">{n.titulo}</p>
                        <p className="text-sm text-gray-600 mt-0.5">{n.mensaje}</p>
                        <div className="flex items-center gap-2 mt-1">
                          <p className="text-xs text-gray-400">{formatDateTime(n.fechaCreacion)}</p>
                          {n.tipo && <Badge color="blue">{n.tipo}</Badge>}
                        </div>
                      </div>
                      {!n.leida && (
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => handleMarcarLeida(n.id)}
                          className="flex-shrink-0 text-xs"
                        >
                          Marcar leída
                        </Button>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>
    </>
  );
}

function TabBtn({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      className={`px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${
        active ? 'bg-white shadow text-gray-900' : 'text-gray-500 hover:text-gray-700'
      }`}
    >
      {children}
    </button>
  );
}
