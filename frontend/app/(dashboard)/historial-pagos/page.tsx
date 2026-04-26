'use client';

import { useEffect, useState, useCallback } from 'react';
import { pagosApi, puestosApi } from '@/lib/api';
import type { Pago, Puesto } from '@/lib/types';
import { formatCurrency, formatDate, getAxiosErrorMessage, downloadBlob } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';
import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import { useToast } from '@/components/ui/Toast';
import { useAuth } from '@/context/AuthContext';
import Pagination from '@/components/ui/Pagination';

const estadoColor: Record<string, 'green' | 'red' | 'gray'> = {
  Activo: 'green', Anulado: 'red',
};

const PAGE_SIZE = 25;
const today = new Date().toISOString().slice(0, 10);
const monthStart = new Date(new Date().getFullYear(), new Date().getMonth(), 1)
  .toISOString().slice(0, 10);

export default function HistorialPagosPage() {
  const { hasRole } = useAuth();
  const { toast } = useToast();
  const [pagos, setPagos] = useState<Pago[]>([]);
  const [puestos, setPuestos] = useState<Puesto[]>([]);
  const [loading, setLoading] = useState(true);
  const [fechaInicio, setFechaInicio] = useState(monthStart);
  const [fechaFin, setFechaFin] = useState(today);
  const [puestoId, setPuestoId] = useState('');
  const [estadoFiltro, setEstadoFiltro] = useState('');
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [modalAnular, setModalAnular] = useState<Pago | null>(null);
  const [motivoAnulacion, setMotivoAnulacion] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const cargarPagos = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const result = await pagosApi.getAll({
        fechaInicio,
        fechaFin,
        puestoId: puestoId || undefined,
        estado: estadoFiltro || undefined,
        page: p,
        pageSize: PAGE_SIZE,
      });
      setPagos(result.data);
      setTotal(result.total);
      setTotalPages(result.totalPages);
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setLoading(false);
    }
  }, [fechaInicio, fechaFin, puestoId, estadoFiltro, toast]);

  useEffect(() => {
    puestosApi.getAll({ pageSize: 500 }).then((r) => setPuestos(r.data)).catch(() => {});
  }, []);

  useEffect(() => {
    setPage(1);
    cargarPagos(1);
  }, [cargarPagos]);

  const handlePage = (p: number) => {
    setPage(p);
    cargarPagos(p);
  };

  const handleDescargar = async (p: Pago) => {
    try {
      const blob = await pagosApi.getComprobante(p.id);
      downloadBlob(blob, `comprobante-${p.numeroComprobante ?? p.id}.pdf`);
    } catch {
      toast('Error al descargar el comprobante', 'error');
    }
  };

  const handleAnular = async () => {
    if (!modalAnular || !motivoAnulacion.trim()) return;
    setSubmitting(true);
    try {
      await pagosApi.anular(modalAnular.id, { motivoAnulacion });
      toast('Pago anulado', 'success');
      setModalAnular(null);
      setMotivoAnulacion('');
      cargarPagos(page);
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <Navbar title="Historial de Pagos" />
      <div className="p-6 space-y-6">

        {/* Filtros */}
        <Card title="Filtros">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Desde</label>
              <input
                type="date"
                value={fechaInicio}
                onChange={(e) => setFechaInicio(e.target.value)}
                className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Hasta</label>
              <input
                type="date"
                value={fechaFin}
                onChange={(e) => setFechaFin(e.target.value)}
                className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Puesto</label>
              <select
                value={puestoId}
                onChange={(e) => setPuestoId(e.target.value)}
                className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">— Todos —</option>
                {puestos.map((p) => (
                  <option key={p.id} value={p.id}>{p.numeroPuesto}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Estado</label>
              <select
                value={estadoFiltro}
                onChange={(e) => setEstadoFiltro(e.target.value)}
                className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">— Todos —</option>
                <option value="Activo">Activo</option>
                <option value="Anulado">Anulado</option>
              </select>
            </div>
          </div>
        </Card>

        {/* Resumen */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="bg-white rounded-xl border border-gray-100 p-4 shadow-sm">
            <p className="text-xs text-gray-400 mb-1">Total pagos (filtro actual)</p>
            <p className="text-2xl font-bold text-gray-800">{total}</p>
          </div>
          <div className="bg-white rounded-xl border border-gray-100 p-4 shadow-sm">
            <p className="text-xs text-gray-400 mb-1">Página</p>
            <p className="text-2xl font-bold text-blue-600">{page} / {totalPages || 1}</p>
          </div>
        </div>

        {/* Tabla */}
        <Card>
          {loading ? (
            <div className="space-y-3">
              {[...Array(8)].map((_, i) => (
                <div key={i} className="h-12 bg-gray-100 rounded animate-pulse" />
              ))}
            </div>
          ) : pagos.length === 0 ? (
            <p className="text-center text-gray-400 py-12">No se encontraron pagos con los filtros seleccionados</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-gray-500 border-b border-gray-200">
                    <th className="pb-3 font-medium">Comprobante</th>
                    <th className="pb-3 font-medium">Fecha</th>
                    <th className="pb-3 font-medium">Puesto</th>
                    <th className="pb-3 font-medium">Dueño</th>
                    <th className="pb-3 font-medium">Cajero</th>
                    <th className="pb-3 font-medium">Método</th>
                    <th className="pb-3 font-medium text-right">Monto</th>
                    <th className="pb-3 font-medium">Estado</th>
                    <th className="pb-3 font-medium">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {pagos.map((p) => (
                    <tr key={p.id} className="hover:bg-gray-50 transition-colors">
                      <td className="py-3 font-mono text-xs text-gray-600">{p.numeroComprobante}</td>
                      <td className="py-3 text-gray-600 whitespace-nowrap">{formatDate(p.fechaPago)}</td>
                      <td className="py-3 font-semibold text-gray-800">{p.puestoNumero ?? '—'}</td>
                      <td className="py-3 text-gray-600">{p.duenoNombre ?? '—'}</td>
                      <td className="py-3 text-gray-500 text-xs">{p.cajeroNombre ?? '—'}</td>
                      <td className="py-3 text-gray-500">{p.metodo}</td>
                      <td className="py-3 text-right font-semibold">
                        <span className={p.estado === 'Anulado' ? 'line-through text-gray-400' : ''}>
                          {formatCurrency(p.montoPagado)}
                        </span>
                      </td>
                      <td className="py-3">
                        <Badge color={estadoColor[p.estado] ?? 'gray'}>{p.estado}</Badge>
                      </td>
                      <td className="py-3">
                        <div className="flex gap-2 items-center">
                          <Button size="sm" variant="secondary" onClick={() => handleDescargar(p)}>
                            PDF
                          </Button>
                          {hasRole('Admin') && p.estado === 'Activo' && (
                            <Button
                              size="sm"
                              variant="danger"
                              onClick={() => { setModalAnular(p); setMotivoAnulacion(''); }}
                            >
                              Anular
                            </Button>
                          )}
                          {hasRole('Admin') && p.estado === 'Anulado' && p.motivoAnulacion && (
                            <span className="text-xs text-gray-400 italic">{p.motivoAnulacion}</span>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <Pagination page={page} totalPages={totalPages} total={total} pageSize={PAGE_SIZE} onPage={handlePage} />
            </div>
          )}
        </Card>
      </div>

      <Modal open={!!modalAnular} onClose={() => setModalAnular(null)} title="Anular Pago">
        <p className="text-sm text-gray-500 mb-4">
          Comprobante: <strong>{modalAnular?.numeroComprobante}</strong> —{' '}
          {formatCurrency(modalAnular?.montoPagado ?? 0)}
        </p>
        <Input
          label="Motivo de anulación"
          value={motivoAnulacion}
          onChange={(e) => setMotivoAnulacion(e.target.value)}
          placeholder="Describe el motivo..."
        />
        <div className="flex gap-3 justify-end mt-4">
          <Button variant="secondary" onClick={() => setModalAnular(null)}>Cancelar</Button>
          <Button variant="danger" loading={submitting} onClick={handleAnular}>Anular</Button>
        </div>
      </Modal>
    </>
  );
}
