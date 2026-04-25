'use client';

import { useEffect, useState } from 'react';
import { reportesApi } from '@/lib/api';
import type { CajaDiariaDto, MorosidadDto } from '@/lib/types';
import { formatCurrency, formatDate } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';

export default function DashboardPage() {
  const [caja, setCaja] = useState<CajaDiariaDto | null>(null);
  const [morosidad, setMorosidad] = useState<MorosidadDto[]>([]);
  const [loadingCaja, setLoadingCaja] = useState(true);
  const [loadingMor, setLoadingMor] = useState(true);

  const today = new Date().toISOString().slice(0, 10);

  useEffect(() => {
    reportesApi.cajaDiaria(today)
      .then(setCaja)
      .catch(() => {})
      .finally(() => setLoadingCaja(false));

    reportesApi.morosidad()
      .then(setMorosidad)
      .catch(() => {})
      .finally(() => setLoadingMor(false));
  }, [today]);

  const totalMorosidad = morosidad.reduce((s, m) => s + m.totalPendiente, 0);

  return (
    <>
      <Navbar title="Dashboard" />
      <div className="p-6 space-y-6">
        {/* KPI Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiCard
            label="Recaudado hoy"
            value={loadingCaja ? '...' : formatCurrency(caja?.totalRecaudado ?? 0)}
            icon="💰"
            color="blue"
          />
          <KpiCard
            label="Pagos hoy"
            value={loadingCaja ? '...' : String(caja?.cantidadPagos ?? 0)}
            icon="✅"
            color="green"
          />
          <KpiCard
            label="Puestos morosos"
            value={loadingMor ? '...' : String(morosidad.length)}
            icon="⚠️"
            color="yellow"
          />
          <KpiCard
            label="Total morosidad"
            value={loadingMor ? '...' : formatCurrency(totalMorosidad)}
            icon="📋"
            color="red"
          />
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Últimos pagos del día */}
          <Card title={`Pagos de hoy — ${formatDate(today)}`}>
            {loadingCaja ? (
              <Skeleton rows={4} />
            ) : !caja || caja.pagos.length === 0 ? (
              <p className="text-sm text-gray-400 text-center py-4">Sin pagos registrados hoy</p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-gray-500 border-b border-gray-100">
                    <th className="pb-2 font-medium">Puesto</th>
                    <th className="pb-2 font-medium">Dueño</th>
                    <th className="pb-2 font-medium text-right">Monto</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {caja.pagos.slice(0, 8).map((p) => (
                    <tr key={p.id}>
                      <td className="py-2 font-medium">{p.puestoNumero}</td>
                      <td className="py-2 text-gray-600">{p.duenoNombre}</td>
                      <td className="py-2 text-right font-semibold text-emerald-600">
                        {formatCurrency(p.montoPagado)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Card>

          {/* Top morosos */}
          <Card title="Puestos con deudas pendientes">
            {loadingMor ? (
              <Skeleton rows={4} />
            ) : morosidad.length === 0 ? (
              <p className="text-sm text-gray-400 text-center py-4">Sin puestos morosos 🎉</p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-gray-500 border-b border-gray-100">
                    <th className="pb-2 font-medium">Puesto</th>
                    <th className="pb-2 font-medium">Dueño</th>
                    <th className="pb-2 font-medium text-right">Pendiente</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {morosidad.slice(0, 8).map((m) => (
                    <tr key={m.puestoId}>
                      <td className="py-2 font-medium">{m.puestoNumero}</td>
                      <td className="py-2 text-gray-600">{m.duenoNombre}</td>
                      <td className="py-2 text-right">
                        <Badge color="red">{formatCurrency(m.totalPendiente)}</Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Card>
        </div>
      </div>
    </>
  );
}

function KpiCard({ label, value, icon, color }: {
  label: string; value: string; icon: string;
  color: 'blue' | 'green' | 'yellow' | 'red';
}) {
  const bg = { blue: 'bg-blue-50', green: 'bg-emerald-50', yellow: 'bg-yellow-50', red: 'bg-red-50' }[color];
  const text = { blue: 'text-blue-700', green: 'text-emerald-700', yellow: 'text-yellow-700', red: 'text-red-700' }[color];
  return (
    <div className={`${bg} rounded-xl p-4 flex items-center gap-4`}>
      <span className="text-3xl">{icon}</span>
      <div>
        <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">{label}</p>
        <p className={`text-xl font-bold ${text}`}>{value}</p>
      </div>
    </div>
  );
}

function Skeleton({ rows }: { rows: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="h-4 bg-gray-100 rounded animate-pulse" />
      ))}
    </div>
  );
}
