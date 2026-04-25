'use client';

import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { puestosApi, deudasApi, pagosApi } from '@/lib/api';
import type { Puesto, Deuda, MetodoPago } from '@/lib/types';
import { formatCurrency, formatDate, getAxiosErrorMessage } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';
import Select from '@/components/ui/Select';
import Input from '@/components/ui/Input';
import Modal from '@/components/ui/Modal';
import { useToast } from '@/components/ui/Toast';

interface PagoFormValues {
  montoPagado: string;
  metodo: MetodoPago;
  referenciaPago?: string;
  observaciones?: string;
}

export default function PagosPage() {
  const { toast } = useToast();
  const [puestos, setPuestos] = useState<Puesto[]>([]);
  const [puestoId, setPuestoId] = useState('');
  const [deudas, setDeudas] = useState<Deuda[]>([]);
  const [deudaSeleccionada, setDeudaSeleccionada] = useState<Deuda | null>(null);
  const [loadingDeudas, setLoadingDeudas] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [modalAnular, setModalAnular] = useState<string | null>(null);
  const [motivoAnulacion, setMotivoAnulacion] = useState('');

  const { register, handleSubmit, reset, formState: { errors } } = useForm<PagoFormValues>({
    defaultValues: { metodo: 'Efectivo' },
  });

  useEffect(() => {
    puestosApi.getAll()
      .then((ps) => setPuestos(ps.filter((p) => p.estado === 'Ocupado')))
      .catch(() => toast('Error cargando puestos', 'error'));
  }, [toast]);

  const buscarDeudas = async (id: string) => {
    if (!id) { setDeudas([]); return; }
    setLoadingDeudas(true);
    setDeudaSeleccionada(null);
    try {
      const data = await deudasApi.getPorPuesto(id);
      setDeudas(data.filter((d) => d.estado === 'Pendiente' || d.estado === 'Vencida'));
    } catch {
      toast('Error al cargar deudas', 'error');
    } finally {
      setLoadingDeudas(false);
    }
  };

  const onSubmit = async (values: PagoFormValues) => {
    if (!deudaSeleccionada) return;
    setSubmitting(true);
    try {
      await pagosApi.registrar({
        deudaId: deudaSeleccionada.id,
        montoPagado: parseFloat(values.montoPagado),
        metodo: values.metodo,
        referenciaPago: values.referenciaPago,
        observaciones: values.observaciones,
      });
      toast('Pago registrado correctamente', 'success');
      reset();
      setDeudaSeleccionada(null);
      await buscarDeudas(puestoId);
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleAnular = async () => {
    if (!modalAnular || !motivoAnulacion.trim()) return;
    try {
      await pagosApi.anular(modalAnular, { motivoAnulacion });
      toast('Pago anulado', 'success');
      setModalAnular(null);
      setMotivoAnulacion('');
      await buscarDeudas(puestoId);
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    }
  };

  const estadoColor: Record<string, 'red' | 'yellow' | 'gray'> = {
    Vencida: 'red', Pendiente: 'yellow',
  };

  return (
    <>
      <Navbar title="Registrar Pago" />
      <div className="p-6 grid grid-cols-1 lg:grid-cols-2 gap-6">

        {/* Columna izquierda: buscar deuda */}
        <Card title="1. Seleccionar puesto y deuda">
          <div className="space-y-4">
            <Select
              id="puesto"
              label="Puesto"
              value={puestoId}
              onChange={(e) => {
                setPuestoId(e.target.value);
                buscarDeudas(e.target.value);
              }}
            >
              <option value="">— Seleccionar puesto —</option>
              {puestos.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.numeroPuesto} — {p.duenoNombre}
                </option>
              ))}
            </Select>

            {loadingDeudas ? (
              <div className="space-y-2">
                {[...Array(3)].map((_, i) => (
                  <div key={i} className="h-16 bg-gray-100 rounded animate-pulse" />
                ))}
              </div>
            ) : deudas.length > 0 ? (
              <div className="space-y-2 max-h-80 overflow-y-auto">
                {deudas.map((d) => (
                  <button
                    key={d.id}
                    onClick={() => {
                      setDeudaSeleccionada(d);
                      reset({ montoPagado: String(d.saldoPendiente), metodo: 'Efectivo' });
                    }}
                    className={`w-full text-left p-3 rounded-lg border-2 transition-colors ${
                      deudaSeleccionada?.id === d.id
                        ? 'border-blue-500 bg-blue-50'
                        : 'border-gray-200 hover:border-blue-300'
                    }`}
                  >
                    <div className="flex justify-between items-start">
                      <div>
                        <p className="font-medium text-sm">{d.conceptoNombre}</p>
                        <p className="text-xs text-gray-500">Período: {d.periodo}</p>
                        <p className="text-xs text-gray-500">Vence: {formatDate(d.fechaVencimiento)}</p>
                      </div>
                      <div className="text-right">
                        <Badge color={estadoColor[d.estado] ?? 'gray'}>{d.estado}</Badge>
                        <p className="text-sm font-bold mt-1">{formatCurrency(d.saldoPendiente)}</p>
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            ) : puestoId ? (
              <p className="text-sm text-gray-400 text-center py-4">Sin deudas pendientes para este puesto</p>
            ) : null}
          </div>
        </Card>

        {/* Columna derecha: formulario de pago */}
        <Card title="2. Registrar pago">
          {!deudaSeleccionada ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <span className="text-4xl mb-3">💳</span>
              <p className="text-gray-500 text-sm">Selecciona una deuda de la lista para registrar el pago</p>
            </div>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              {/* Resumen de deuda */}
              <div className="bg-blue-50 rounded-lg p-4 text-sm space-y-1">
                <p><span className="font-medium">Concepto:</span> {deudaSeleccionada.conceptoNombre}</p>
                <p><span className="font-medium">Período:</span> {deudaSeleccionada.periodo}</p>
                <p><span className="font-medium">Saldo:</span> <span className="font-bold">{formatCurrency(deudaSeleccionada.saldoPendiente)}</span></p>
              </div>

              <Input
                id="montoPagado"
                label="Monto a pagar (S/.)"
                type="number"
                step="0.01"
                error={errors.montoPagado?.message}
                {...register('montoPagado', {
                  required: 'Campo obligatorio',
                  min: { value: 0.01, message: 'Monto inválido' },
                })}
              />

              <Select id="metodo" label="Método de pago" {...register('metodo')}>
                <option value="Efectivo">Efectivo</option>
                <option value="Transferencia">Transferencia</option>
                <option value="Cheque">Cheque</option>
                <option value="Otro">Otro</option>
              </Select>

              <Input
                id="referenciaPago"
                label="Referencia / N° operación (opcional)"
                placeholder="Ej: OPE-12345"
                {...register('referenciaPago')}
              />

              <Input
                id="observaciones"
                label="Observaciones (opcional)"
                {...register('observaciones')}
              />

              <div className="flex gap-3 pt-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setDeudaSeleccionada(null)}
                >
                  Cancelar
                </Button>
                <Button type="submit" loading={submitting} className="flex-1">
                  Registrar Pago
                </Button>
              </div>
            </form>
          )}
        </Card>
      </div>

      {/* Modal Anular */}
      <Modal open={!!modalAnular} onClose={() => setModalAnular(null)} title="Anular Pago">
        <div className="space-y-4">
          <Input
            label="Motivo de anulación"
            value={motivoAnulacion}
            onChange={(e) => setMotivoAnulacion(e.target.value)}
            placeholder="Describe el motivo..."
          />
          <div className="flex gap-3 justify-end">
            <Button variant="secondary" onClick={() => setModalAnular(null)}>Cancelar</Button>
            <Button variant="danger" onClick={handleAnular}>Anular</Button>
          </div>
        </div>
      </Modal>
    </>
  );
}
