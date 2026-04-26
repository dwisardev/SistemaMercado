'use client';

import { useEffect, useState, useCallback } from 'react';
import { useForm } from 'react-hook-form';
import { puestosApi, conceptosApi, deudasApi } from '@/lib/api';
import type { Puesto, ConceptoCobro, CargaMasivaResult, Deuda } from '@/lib/types';
import { formatCurrency, formatDate, getAxiosErrorMessage } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import Select from '@/components/ui/Select';
import { useToast } from '@/components/ui/Toast';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';

interface IndividualForm {
  puestoId: string;
  conceptoId: string;
  monto: string;
  fechaVencimiento: string;
  periodo: string;
}

interface MasivaForm {
  conceptoId: string;
  monto: string;
  fechaVencimiento: string;
  periodo: string;
}

const estadoDeudaColor: Record<string, 'red' | 'yellow' | 'green' | 'gray'> = {
  Vencida: 'red', Pendiente: 'yellow', Pagada: 'green', Anulada: 'gray',
};

export default function DeudasPage() {
  const { hasRole } = useAuth();
  const router = useRouter();
  const { toast } = useToast();

  const [puestos, setPuestos] = useState<Puesto[]>([]);
  const [conceptos, setConceptos] = useState<ConceptoCobro[]>([]);
  const [tab, setTab] = useState<'individual' | 'masiva' | 'consultar'>('individual');
  const [submitting, setSubmitting] = useState(false);
  const [resultadoMasivo, setResultadoMasivo] = useState<CargaMasivaResult | null>(null);

  // Consultar
  const [filtroPuesto, setFiltroPuesto] = useState('');
  const [filtroEstado, setFiltroEstado] = useState('');
  const [filtroPeriodo, setFiltroPeriodo] = useState('');
  const [deudas, setDeudas] = useState<Deuda[]>([]);
  const [loadingDeudas, setLoadingDeudas] = useState(false);

  const indForm = useForm<IndividualForm>();
  const masForm = useForm<MasivaForm>();

  useEffect(() => {
    if (!hasRole('Admin')) { router.push('/dashboard'); return; }
    Promise.all([puestosApi.getAll(), conceptosApi.getAll()])
      .then(([ps, cs]) => {
        setPuestos(ps.data.filter((p) => p.estado === 'Ocupado'));
        setConceptos(cs.filter((c) => c.activo));
      })
      .catch(() => toast('Error cargando datos', 'error'));
  }, [hasRole, router, toast]);

  const handleIndividual = async (values: IndividualForm) => {
    setSubmitting(true);
    try {
      await deudasApi.cargarIndividual({
        ...values,
        monto: parseFloat(values.monto),
      });
      toast('Deuda registrada correctamente', 'success');
      indForm.reset();
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleMasiva = async (values: MasivaForm) => {
    setSubmitting(true);
    setResultadoMasivo(null);
    try {
      const res = await deudasApi.cargarMasiva({
        ...values,
        monto: parseFloat(values.monto),
      });
      setResultadoMasivo(res);
      toast(`Carga masiva: ${res.exitosos}/${res.total} exitosos`, res.fallidos > 0 ? 'info' : 'success');
      masForm.reset();
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const buscarDeudas = useCallback(async () => {
    if (!filtroPuesto && !filtroEstado && !filtroPeriodo) {
      toast('Selecciona al menos un filtro', 'error');
      return;
    }
    setLoadingDeudas(true);
    try {
      const data = await deudasApi.getFiltradas({
        puestoId: filtroPuesto || undefined,
        estado: filtroEstado || undefined,
        periodo: filtroPeriodo || undefined,
      });
      setDeudas(data);
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setLoadingDeudas(false);
    }
  }, [filtroPuesto, filtroEstado, filtroPeriodo, toast]);

  const today = new Date().toISOString().slice(0, 10);

  const tabLabels: Record<string, string> = {
    individual: '📋 Carga Individual',
    masiva: '📦 Carga Masiva',
    consultar: '🔍 Consultar',
  };

  return (
    <>
      <Navbar title="Carga de Deudas" />
      <div className="p-6 max-w-3xl mx-auto">
        {/* Tabs */}
        <div className="flex gap-1 bg-gray-100 rounded-lg p-1 mb-6">
          {(['individual', 'masiva', 'consultar'] as const).map((t) => (
            <button
              key={t}
              onClick={() => setTab(t)}
              className={`flex-1 py-2 text-sm font-medium rounded-md transition-colors ${
                tab === t ? 'bg-white shadow text-gray-900' : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              {tabLabels[t]}
            </button>
          ))}
        </div>

        {tab === 'individual' && (
          <Card title="Registrar deuda individual">
            <form onSubmit={indForm.handleSubmit(handleIndividual)} className="space-y-4">
              <Select
                id="puestoId"
                label="Puesto"
                error={indForm.formState.errors.puestoId?.message}
                {...indForm.register('puestoId', { required: 'Selecciona un puesto' })}
              >
                <option value="">— Seleccionar puesto —</option>
                {puestos.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.numeroPuesto} — {p.duenoNombre}
                  </option>
                ))}
              </Select>

              <ConceptoConMonto
                conceptos={conceptos}
                register={indForm.register}
                errors={indForm.formState.errors}
                setValue={(field, val) => indForm.setValue(field as keyof IndividualForm, val)}
              />

              <div className="grid grid-cols-2 gap-4">
                <Input
                  id="periodo"
                  label="Período"
                  placeholder="Ej: Enero 2025"
                  error={indForm.formState.errors.periodo?.message}
                  {...indForm.register('periodo', { required: 'Campo obligatorio' })}
                />
                <Input
                  id="fechaVencimiento"
                  label="Fecha de vencimiento"
                  type="date"
                  min={today}
                  error={indForm.formState.errors.fechaVencimiento?.message}
                  {...indForm.register('fechaVencimiento', { required: 'Campo obligatorio' })}
                />
              </div>

              <Button type="submit" loading={submitting} className="w-full">
                Registrar Deuda
              </Button>
            </form>
          </Card>
        )}

        {tab === 'consultar' && (
          <Card title="Consultar Deudas">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-4">
              <div>
                <label className="block text-xs font-medium text-gray-500 mb-1">Puesto</label>
                <select
                  value={filtroPuesto}
                  onChange={(e) => setFiltroPuesto(e.target.value)}
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
                  value={filtroEstado}
                  onChange={(e) => setFiltroEstado(e.target.value)}
                  className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">— Todos —</option>
                  <option value="Pendiente">Pendiente</option>
                  <option value="Vencida">Vencida</option>
                  <option value="Pagada">Pagada</option>
                  <option value="Anulada">Anulada</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-500 mb-1">Período</label>
                <input
                  type="text"
                  value={filtroPeriodo}
                  onChange={(e) => setFiltroPeriodo(e.target.value)}
                  placeholder="Ej: Enero 2025"
                  className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>
            <Button onClick={buscarDeudas} loading={loadingDeudas} className="mb-6">
              Buscar
            </Button>

            {deudas.length > 0 && (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-gray-500 border-b border-gray-200">
                      <th className="pb-3 font-medium">Puesto</th>
                      <th className="pb-3 font-medium">Dueño</th>
                      <th className="pb-3 font-medium">Concepto</th>
                      <th className="pb-3 font-medium">Período</th>
                      <th className="pb-3 font-medium">Vencimiento</th>
                      <th className="pb-3 font-medium text-right">Monto</th>
                      <th className="pb-3 font-medium">Estado</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {deudas.map((d) => (
                      <tr key={d.id} className="hover:bg-gray-50">
                        <td className="py-2 font-semibold text-gray-800">{d.puestoNumero ?? '—'}</td>
                        <td className="py-2 text-gray-500 text-xs">{d.duenoNombre ?? '—'}</td>
                        <td className="py-2 text-gray-600">{d.conceptoNombre}</td>
                        <td className="py-2 text-gray-500">{d.periodo}</td>
                        <td className="py-2 text-gray-400 text-xs">
                          {d.fechaVencimiento ? formatDate(d.fechaVencimiento) : '—'}
                        </td>
                        <td className="py-2 text-right font-semibold">{formatCurrency(d.monto)}</td>
                        <td className="py-2">
                          <Badge color={estadoDeudaColor[d.estado] ?? 'gray'}>{d.estado}</Badge>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <p className="text-xs text-gray-400 mt-3 text-right">{deudas.length} resultado(s)</p>
              </div>
            )}
            {!loadingDeudas && deudas.length === 0 && (
              <p className="text-center text-gray-400 py-6 text-sm">
                Usa los filtros arriba y presiona Buscar
              </p>
            )}
          </Card>
        )}

        {tab === 'masiva' && (
          <Card title="Carga masiva (todos los puestos activos)">
            <p className="text-sm text-gray-500 mb-4">
              Genera la misma deuda para los <strong>{puestos.length}</strong> puestos ocupados.
            </p>
            <form onSubmit={masForm.handleSubmit(handleMasiva)} className="space-y-4">
              <ConceptoConMonto
                conceptos={conceptos}
                register={masForm.register}
                errors={masForm.formState.errors}
                setValue={(field, val) => masForm.setValue(field as keyof MasivaForm, val)}
              />

              <div className="grid grid-cols-2 gap-4">
                <Input
                  id="periodoMas"
                  label="Período"
                  placeholder="Ej: Enero 2025"
                  error={masForm.formState.errors.periodo?.message}
                  {...masForm.register('periodo', { required: 'Campo obligatorio' })}
                />
                <Input
                  id="fechaVencimientoMas"
                  label="Fecha de vencimiento"
                  type="date"
                  min={today}
                  error={masForm.formState.errors.fechaVencimiento?.message}
                  {...masForm.register('fechaVencimiento', { required: 'Campo obligatorio' })}
                />
              </div>

              <Button type="submit" loading={submitting} className="w-full">
                Generar deudas para todos los puestos
              </Button>
            </form>

            {/* Resultado */}
            {resultadoMasivo && (
              <div className={`mt-4 rounded-lg p-4 text-sm ${resultadoMasivo.fallidos > 0 ? 'bg-yellow-50 border border-yellow-200' : 'bg-green-50 border border-green-200'}`}>
                <p className="font-semibold mb-2">Resultado de la carga masiva</p>
                <div className="grid grid-cols-3 gap-2 text-center mb-3">
                  <div>
                    <p className="text-2xl font-bold text-gray-800">{resultadoMasivo.total}</p>
                    <p className="text-xs text-gray-500">Total</p>
                  </div>
                  <div>
                    <p className="text-2xl font-bold text-emerald-600">{resultadoMasivo.exitosos}</p>
                    <p className="text-xs text-gray-500">Exitosos</p>
                  </div>
                  <div>
                    <p className="text-2xl font-bold text-red-600">{resultadoMasivo.fallidos}</p>
                    <p className="text-xs text-gray-500">Fallidos</p>
                  </div>
                </div>
                {resultadoMasivo.errores.length > 0 && (
                  <ul className="text-xs text-red-700 space-y-1">
                    {resultadoMasivo.errores.map((e, i) => <li key={i}>• {e}</li>)}
                  </ul>
                )}
              </div>
            )}
          </Card>
        )}
      </div>
    </>
  );
}

// Subcomponente: selects de concepto + monto con autocompletado de tarifa
function ConceptoConMonto({ conceptos, register, errors, setValue }: {
  conceptos: ConceptoCobro[];
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  register: any; errors: any; setValue: (f: string, v: string) => void;
}) {
  return (
    <>
      <Select
        id="conceptoId"
        label="Concepto de cobro"
        error={errors.conceptoId?.message}
        {...register('conceptoId', { required: 'Selecciona un concepto' })}
        onChange={(e: React.ChangeEvent<HTMLSelectElement>) => {
          const found = conceptos.find((c) => c.id === e.target.value);
          if (found) setValue('monto', String(found.monto));
          register('conceptoId').onChange(e);
        }}
      >
        <option value="">— Seleccionar —</option>
        {conceptos.map((c) => (
          <option key={c.id} value={c.id}>
            {c.nombre} — {formatCurrency(c.monto)}
          </option>
        ))}
      </Select>

      <Input
        id="monto"
        label="Monto (S/.)"
        type="number"
        step="0.01"
        error={errors.monto?.message}
        {...register('monto', { required: 'Campo obligatorio', min: { value: 0.01, message: 'Inválido' } })}
      />
    </>
  );
}
