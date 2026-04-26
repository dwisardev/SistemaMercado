'use client';

import { useEffect, useState } from 'react';
import { reportesApi } from '@/lib/api';
import type { CajaDiariaDto, MorosidadDto } from '@/lib/types';
import { formatCurrency, formatDate, formatDateTime } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';

type ReporteTab = 'caja' | 'morosidad';

export default function ReportesPage() {
  const [tab, setTab] = useState<ReporteTab>('caja');
  const [fecha, setFecha] = useState(new Date().toISOString().slice(0, 10));
  const [caja, setCaja] = useState<CajaDiariaDto | null>(null);
  const [morosidad, setMorosidad] = useState<MorosidadDto[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (tab === 'caja') loadCaja();
    else loadMorosidad();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab]);

  const loadCaja = async (f?: string) => {
    setLoading(true);
    try {
      const data = await reportesApi.cajaDiaria(f ?? fecha);
      setCaja(data);
    } catch {
      setCaja(null);
    } finally {
      setLoading(false);
    }
  };

  const loadMorosidad = async () => {
    setLoading(true);
    try {
      const data = await reportesApi.morosidad();
      setMorosidad(data);
    } catch {
      setMorosidad([]);
    } finally {
      setLoading(false);
    }
  };

  const totalMorosidad = morosidad.reduce((s, m) => s + m.totalPendiente, 0);

  const metodoBadge: Record<string, 'green' | 'blue' | 'yellow' | 'gray'> = {
    Efectivo: 'green', Transferencia: 'blue', Cheque: 'yellow',
  };

  return (
    <>
      <Navbar title="Reportes" />
      <div className="p-6 space-y-6">
        {/* Tabs */}
        <div className="flex gap-1 bg-gray-100 rounded-lg p-1 w-fit">
          <TabBtn active={tab === 'caja'} onClick={() => setTab('caja')}>💰 Caja Diaria</TabBtn>
          <TabBtn active={tab === 'morosidad'} onClick={() => setTab('morosidad')}>⚠️ Morosidad</TabBtn>
        </div>

        {/* CAJA DIARIA */}
        {tab === 'caja' && (
          <div className="space-y-4">
            {/* Filtro de fecha */}
            <div className="flex items-center gap-3">
              <input
                type="date"
                value={fecha}
                max={new Date().toISOString().slice(0, 10)}
                onChange={(e) => setFecha(e.target.value)}
                className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <Button onClick={() => loadCaja(fecha)}>Buscar</Button>
            </div>

            {loading ? (
              <Skeleton />
            ) : !caja ? (
              <p className="text-gray-400 text-sm">Sin datos para esta fecha</p>
            ) : (
              <>
                {/* KPIs */}
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                  <KpiBox label="Total recaudado" value={formatCurrency(caja.totalRecaudado)} icon="💰" />
                  <KpiBox label="Cantidad de pagos" value={String(caja.cantidadPagos)} icon="✅" />
                  <KpiBox label="Fecha" value={formatDate(caja.fecha)} icon="📅" />
                </div>

                {/* Tabla pagos */}
                <Card title="Detalle de pagos">
                  {caja.pagos.length === 0 ? (
                    <p className="text-sm text-gray-400 text-center py-6">Sin pagos registrados</p>
                  ) : (
                    <div className="overflow-x-auto">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="text-left text-gray-500 border-b border-gray-200">
                            <th className="pb-3 font-medium">Comprobante</th>
                            <th className="pb-3 font-medium">Puesto</th>
                            <th className="pb-3 font-medium">Dueño</th>
                            <th className="pb-3 font-medium">Cajero</th>
                            <th className="pb-3 font-medium">Método</th>
                            <th className="pb-3 font-medium">Fecha/Hora</th>
                            <th className="pb-3 font-medium text-right">Monto</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-50">
                          {caja.pagos.map((p) => (
                            <tr key={p.id} className="hover:bg-gray-50">
                              <td className="py-2.5 font-mono text-xs text-gray-500">{p.numeroComprobante ?? '—'}</td>
                              <td className="py-2.5 font-semibold">{p.puestoNumero}</td>
                              <td className="py-2.5 text-gray-600">{p.duenoNombre}</td>
                              <td className="py-2.5 text-gray-600">{p.cajeroNombre ?? '—'}</td>
                              <td className="py-2.5">
                                <Badge color={metodoBadge[p.metodo] ?? 'gray'}>{p.metodo}</Badge>
                              </td>
                              <td className="py-2.5 text-gray-500 text-xs">{formatDateTime(p.fechaPago)}</td>
                              <td className="py-2.5 text-right font-bold text-emerald-600">
                                {formatCurrency(p.montoPagado)}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                        <tfoot className="border-t-2 border-gray-200">
                          <tr>
                            <td colSpan={6} className="pt-3 text-sm font-semibold text-right">Total:</td>
                            <td className="pt-3 text-right font-bold text-lg text-emerald-600">
                              {formatCurrency(caja.totalRecaudado)}
                            </td>
                          </tr>
                        </tfoot>
                      </table>
                    </div>
                  )}
                </Card>
              </>
            )}
          </div>
        )}

        {/* MOROSIDAD */}
        {tab === 'morosidad' && (
          <div className="space-y-4">
            {loading ? (
              <Skeleton />
            ) : (
              <>
                {/* KPIs */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <KpiBox label="Puestos morosos" value={String(morosidad.length)} icon="🏪" />
                  <KpiBox label="Total pendiente" value={formatCurrency(totalMorosidad)} icon="⚠️" />
                </div>

                {morosidad.length === 0 ? (
                  <div className="text-center py-12">
                    <span className="text-5xl">🎉</span>
                    <p className="text-gray-500 mt-2">Sin puestos morosos</p>
                  </div>
                ) : (
                  <div className="space-y-4">
                    {morosidad.map((m) => (
                      <Card key={m.puestoId}>
                        <div className="flex items-start justify-between mb-3">
                          <div>
                            <p className="font-bold text-gray-800">Puesto {m.puestoNumero}</p>
                            <p className="text-sm text-gray-500">{m.duenoNombre}</p>
                          </div>
                          <Badge color="red" className="text-sm px-3 py-1">
                            {formatCurrency(m.totalPendiente)}
                          </Badge>
                        </div>
                        <table className="w-full text-xs">
                          <thead>
                            <tr className="text-gray-400 border-b border-gray-100">
                              <th className="pb-1 text-left">Concepto</th>
                              <th className="pb-1 text-left">Período</th>
                              <th className="pb-1 text-left">Vencimiento</th>
                              <th className="pb-1 text-right">Pendiente</th>
                            </tr>
                          </thead>
                          <tbody>
                            {m.deudas.map((d) => (
                              <tr key={d.id} className="border-b border-gray-50 last:border-0">
                                <td className="py-1.5">{d.conceptoNombre}</td>
                                <td className="py-1.5">{d.periodo}</td>
                                <td className="py-1.5">
                                  <span className={new Date(d.fechaVencimiento) < new Date() ? 'text-red-600 font-medium' : ''}>
                                    {formatDate(d.fechaVencimiento)}
                                  </span>
                                </td>
                                <td className="py-1.5 text-right font-medium">{formatCurrency(d.saldoPendiente)}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </Card>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>
        )}
      </div>
    </>
  );
}

function TabBtn({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${
        active ? 'bg-white shadow text-gray-900' : 'text-gray-500 hover:text-gray-700'
      }`}
    >
      {children}
    </button>
  );
}

function KpiBox({ label, value, icon }: { label: string; value: string; icon: string }) {
  return (
    <div className="bg-gray-50 rounded-xl p-4 flex items-center gap-3">
      <span className="text-2xl">{icon}</span>
      <div>
        <p className="text-xs text-gray-500 uppercase tracking-wide">{label}</p>
        <p className="text-lg font-bold text-gray-800">{value}</p>
      </div>
    </div>
  );
}

function Skeleton() {
  return (
    <div className="space-y-3">
      {[...Array(4)].map((_, i) => <div key={i} className="h-12 bg-gray-100 rounded animate-pulse" />)}
    </div>
  );
}
