'use client';

import { useEffect, useState } from 'react';
import { puestosApi, deudasApi } from '@/lib/api';
import type { Puesto, Deuda } from '@/lib/types';
import { formatCurrency, formatDate } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import { useToast } from '@/components/ui/Toast';

const estadoDeudaColor: Record<string, 'red' | 'yellow' | 'green' | 'gray'> = {
  Vencida: 'red', Pendiente: 'yellow', Pagada: 'green', Anulada: 'gray',
};

const estadoPuestoColor: Record<string, 'green' | 'blue' | 'yellow' | 'gray'> = {
  Disponible: 'green', Ocupado: 'blue', Mantenimiento: 'yellow',
};

export default function MiCuentaPage() {
  const { toast } = useToast();
  const [puestos, setPuestos] = useState<Puesto[]>([]);
  const [deudasPorPuesto, setDeudasPorPuesto] = useState<Record<string, Deuda[]>>({});
  const [loading, setLoading] = useState(true);
  const [puestoAbierto, setPuestoAbierto] = useState<string | null>(null);
  const [loadingDeudas, setLoadingDeudas] = useState<string | null>(null);

  useEffect(() => {
    puestosApi.getMisPuestos()
      .then(setPuestos)
      .catch(() => toast('Error al cargar tus puestos', 'error'))
      .finally(() => setLoading(false));
  }, [toast]);

  const togglePuesto = async (puestoId: string) => {
    if (puestoAbierto === puestoId) {
      setPuestoAbierto(null);
      return;
    }
    setPuestoAbierto(puestoId);
    if (deudasPorPuesto[puestoId]) return;

    setLoadingDeudas(puestoId);
    try {
      const deudas = await deudasApi.getPorPuesto(puestoId);
      setDeudasPorPuesto((prev) => ({ ...prev, [puestoId]: deudas }));
    } catch {
      toast('Error al cargar deudas', 'error');
    } finally {
      setLoadingDeudas(null);
    }
  };

  const totalPendiente = Object.values(deudasPorPuesto)
    .flat()
    .filter((d) => d.estado === 'Pendiente' || d.estado === 'Vencida')
    .reduce((s, d) => s + d.saldoPendiente, 0);

  return (
    <>
      <Navbar title="Mi Cuenta" />
      <div className="p-6 space-y-6">

        {/* KPIs */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="bg-white rounded-xl border border-gray-100 p-4 shadow-sm">
            <p className="text-xs text-gray-400 mb-1">Mis puestos</p>
            <p className="text-2xl font-bold text-gray-800">{puestos.length}</p>
          </div>
          <div className="bg-white rounded-xl border border-gray-100 p-4 shadow-sm">
            <p className="text-xs text-gray-400 mb-1">Puestos activos</p>
            <p className="text-2xl font-bold text-blue-600">
              {puestos.filter((p) => p.estado === 'Ocupado').length}
            </p>
          </div>
          <div className="bg-white rounded-xl border border-gray-100 p-4 shadow-sm">
            <p className="text-xs text-gray-400 mb-1">Saldo pendiente</p>
            <p className="text-2xl font-bold text-red-500">
              {Object.keys(deudasPorPuesto).length > 0 ? formatCurrency(totalPendiente) : '—'}
            </p>
          </div>
        </div>

        {/* Lista de puestos */}
        <Card title="Mis Puestos y Deudas">
          {loading ? (
            <div className="space-y-3">
              {[...Array(3)].map((_, i) => (
                <div key={i} className="h-16 bg-gray-100 rounded animate-pulse" />
              ))}
            </div>
          ) : puestos.length === 0 ? (
            <p className="text-center text-gray-400 py-12">No tienes puestos asignados</p>
          ) : (
            <div className="space-y-3">
              {puestos.map((p) => (
                <div key={p.id} className="border border-gray-100 rounded-xl overflow-hidden">
                  {/* Cabecera del puesto */}
                  <button
                    onClick={() => togglePuesto(p.id)}
                    className="w-full text-left p-4 flex items-center justify-between hover:bg-gray-50 transition-colors"
                  >
                    <div className="flex items-center gap-4">
                      <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center font-bold text-blue-600 text-sm">
                        {p.numeroPuesto}
                      </div>
                      <div>
                        <p className="font-semibold text-gray-800">Puesto {p.numeroPuesto}</p>
                        {p.sector && <p className="text-xs text-gray-400">Sector: {p.sector}</p>}
                      </div>
                    </div>
                    <div className="flex items-center gap-3">
                      {p.tarifaMensual && (
                        <span className="text-sm text-gray-500">{formatCurrency(p.tarifaMensual)}/mes</span>
                      )}
                      <Badge color={estadoPuestoColor[p.estado] ?? 'gray'}>{p.estado}</Badge>
                      <span className="text-gray-400 text-xs">{puestoAbierto === p.id ? '▲' : '▼'}</span>
                    </div>
                  </button>

                  {/* Deudas del puesto */}
                  {puestoAbierto === p.id && (
                    <div className="border-t border-gray-100 bg-gray-50 p-4">
                      {loadingDeudas === p.id ? (
                        <div className="space-y-2">
                          {[...Array(3)].map((_, i) => (
                            <div key={i} className="h-10 bg-gray-200 rounded animate-pulse" />
                          ))}
                        </div>
                      ) : !deudasPorPuesto[p.id] || deudasPorPuesto[p.id].length === 0 ? (
                        <p className="text-sm text-gray-400 text-center py-4">Sin deudas registradas</p>
                      ) : (
                        <table className="w-full text-sm">
                          <thead>
                            <tr className="text-left text-gray-400 border-b border-gray-200">
                              <th className="pb-2 font-medium">Concepto</th>
                              <th className="pb-2 font-medium">Período</th>
                              <th className="pb-2 font-medium">Vencimiento</th>
                              <th className="pb-2 font-medium text-right">Saldo</th>
                              <th className="pb-2 font-medium">Estado</th>
                            </tr>
                          </thead>
                          <tbody className="divide-y divide-gray-100">
                            {deudasPorPuesto[p.id].map((d) => (
                              <tr key={d.id} className="hover:bg-white transition-colors">
                                <td className="py-2 font-medium text-gray-700">{d.conceptoNombre}</td>
                                <td className="py-2 text-gray-500">{d.periodo}</td>
                                <td className="py-2 text-gray-500 text-xs">
                                  {d.fechaVencimiento ? formatDate(d.fechaVencimiento) : '—'}
                                </td>
                                <td className="py-2 text-right font-semibold">
                                  {formatCurrency(d.saldoPendiente)}
                                </td>
                                <td className="py-2">
                                  <Badge color={estadoDeudaColor[d.estado] ?? 'gray'}>
                                    {d.estado}
                                  </Badge>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                          <tfoot>
                            <tr className="border-t border-gray-200">
                              <td colSpan={3} className="pt-3 text-right text-xs text-gray-400 font-medium">
                                Total pendiente:
                              </td>
                              <td className="pt-3 text-right font-bold text-red-500">
                                {formatCurrency(
                                  deudasPorPuesto[p.id]
                                    .filter((d) => d.estado === 'Pendiente' || d.estado === 'Vencida')
                                    .reduce((s, d) => s + d.saldoPendiente, 0)
                                )}
                              </td>
                              <td />
                            </tr>
                          </tfoot>
                        </table>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>
    </>
  );
}
