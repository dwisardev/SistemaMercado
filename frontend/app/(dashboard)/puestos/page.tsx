'use client';

import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { puestosApi, usuariosApi } from '@/lib/api';
import type { Puesto, Usuario } from '@/lib/types';
import { formatCurrency, getAxiosErrorMessage } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';
import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import Select from '@/components/ui/Select';
import { useToast } from '@/components/ui/Toast';
import { useAuth } from '@/context/AuthContext';
import Pagination from '@/components/ui/Pagination';
import { usePagination } from '@/lib/usePagination';

const estadoColor: Record<string, 'green' | 'blue' | 'yellow' | 'gray'> = {
  Disponible: 'green', Ocupado: 'blue', Mantenimiento: 'yellow',
};

interface EditarForm {
  descripcion: string;
  sector: string;
  estado: string;
}

export default function PuestosPage() {
  const { hasRole } = useAuth();
  const { toast } = useToast();
  const [puestos, setPuestos] = useState<Puesto[]>([]);
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [modalAsignar, setModalAsignar] = useState<Puesto | null>(null);
  const [modalLiberar, setModalLiberar] = useState<Puesto | null>(null);
  const [modalEditar, setModalEditar] = useState<Puesto | null>(null);
  const [modalNuevo, setModalNuevo] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const asignarForm = useForm<{ duenoId: string }>();
  const nuevoForm = useForm<{ numeroPuesto: string; descripcion?: string; sector?: string; tarifaMensual?: string }>();
  const editarForm = useForm<EditarForm>();

  useEffect(() => {
    Promise.all([puestosApi.getAll(), usuariosApi.getAll()])
      .then(([p, u]) => {
        setPuestos(p);
        setUsuarios(u.filter((u) => u.rol === 'Dueno'));
      })
      .catch(() => toast('Error al cargar datos', 'error'))
      .finally(() => setLoading(false));
  }, [toast]);

  const filtered = puestos.filter((p) =>
    p.numeroPuesto.toLowerCase().includes(search.toLowerCase()) ||
    (p.duenoNombre ?? '').toLowerCase().includes(search.toLowerCase()) ||
    (p.sector ?? '').toLowerCase().includes(search.toLowerCase())
  );
  const { page, setPage, totalPages, paged, total } = usePagination(filtered, 20);

  const handleAsignar = async (values: { duenoId: string }) => {
    if (!modalAsignar) return;
    setSubmitting(true);
    try {
      const updated = await puestosApi.asignarDueno(modalAsignar.id, values);
      setPuestos((prev) => prev.map((p) => (p.id === updated.id ? updated : p)));
      setModalAsignar(null);
      toast('Dueño asignado correctamente', 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleLiberar = async () => {
    if (!modalLiberar) return;
    try {
      const updated = await puestosApi.liberar(modalLiberar.id);
      setPuestos((prev) => prev.map((p) => (p.id === updated.id ? updated : p)));
      setModalLiberar(null);
      toast('Puesto liberado', 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    }
  };

  const abrirEditar = (p: Puesto) => {
    editarForm.reset({
      descripcion: p.descripcion ?? '',
      sector: p.sector ?? '',
      estado: p.estado === 'Ocupado' ? 'Disponible' : p.estado,
    });
    setModalEditar(p);
  };

  const handleEditar = async (values: EditarForm) => {
    if (!modalEditar) return;
    setSubmitting(true);
    try {
      const updated = await puestosApi.update(modalEditar.id, {
        descripcion: values.descripcion,
        sector: values.sector,
        estado: values.estado,
      });
      setPuestos((prev) => prev.map((p) => (p.id === updated.id ? updated : p)));
      setModalEditar(null);
      toast('Puesto actualizado', 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleCrear = async (values: { numeroPuesto: string; descripcion?: string; sector?: string; tarifaMensual?: string }) => {
    setSubmitting(true);
    try {
      const nuevo = await puestosApi.create({
        ...values,
        tarifaMensual: values.tarifaMensual ? parseFloat(values.tarifaMensual) : undefined,
      });
      setPuestos((prev) => [nuevo, ...prev]);
      setModalNuevo(false);
      nuevoForm.reset();
      toast('Puesto creado correctamente', 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <Navbar title="Puestos" />
      <div className="p-6">
        <Card>
          {/* Toolbar */}
          <div className="flex flex-col sm:flex-row gap-3 mb-6">
            <input
              type="text"
              placeholder="Buscar por número, dueño o sector..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="flex-1 rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            {hasRole('Admin') && (
              <Button onClick={() => setModalNuevo(true)}>+ Nuevo Puesto</Button>
            )}
          </div>

          {/* Table */}
          {loading ? (
            <div className="space-y-3">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="h-12 bg-gray-100 rounded animate-pulse" />
              ))}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-gray-500 border-b border-gray-200">
                    <th className="pb-3 font-medium">Puesto</th>
                    <th className="pb-3 font-medium">Sector</th>
                    <th className="pb-3 font-medium">Estado</th>
                    <th className="pb-3 font-medium">Dueño</th>
                    <th className="pb-3 font-medium text-right">Tarifa</th>
                    {hasRole('Admin', 'Cajero') && (
                      <th className="pb-3 font-medium text-right">Acciones</th>
                    )}
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {filtered.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="py-8 text-center text-gray-400">
                        No se encontraron puestos
                      </td>
                    </tr>
                  ) : (
                    paged.map((p) => (
                      <tr key={p.id} className="hover:bg-gray-50 transition-colors">
                        <td className="py-3">
                          <p className="font-semibold text-gray-800">{p.numeroPuesto}</p>
                          {p.descripcion && (
                            <p className="text-xs text-gray-400 mt-0.5">{p.descripcion}</p>
                          )}
                        </td>
                        <td className="py-3 text-gray-600">{p.sector ?? '—'}</td>
                        <td className="py-3">
                          <Badge color={estadoColor[p.estado] ?? 'gray'}>{p.estado}</Badge>
                        </td>
                        <td className="py-3 text-gray-600">{p.duenoNombre ?? '—'}</td>
                        <td className="py-3 text-right">
                          {p.tarifaMensual ? formatCurrency(p.tarifaMensual) : '—'}
                        </td>
                        {hasRole('Admin', 'Cajero') && (
                          <td className="py-3 text-right">
                            <div className="flex gap-2 justify-end">
                              {hasRole('Admin') && (
                                <Button size="sm" variant="secondary" onClick={() => abrirEditar(p)}>
                                  Editar
                                </Button>
                              )}
                              {p.estado !== 'Ocupado' ? (
                                <Button
                                  size="sm"
                                  variant="secondary"
                                  onClick={() => {
                                    asignarForm.reset();
                                    setModalAsignar(p);
                                  }}
                                >
                                  Asignar dueño
                                </Button>
                              ) : (
                                <Button
                                  size="sm"
                                  variant="danger"
                                  onClick={() => setModalLiberar(p)}
                                >
                                  Liberar
                                </Button>
                              )}
                            </div>
                          </td>
                        )}
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
              <Pagination page={page} totalPages={totalPages} total={total} pageSize={20} onPage={setPage} />
            </div>
          )}
        </Card>
      </div>

      {/* Modal: Editar puesto */}
      <Modal
        open={!!modalEditar}
        onClose={() => setModalEditar(null)}
        title={`Editar Puesto ${modalEditar?.numeroPuesto}`}
      >
        <form onSubmit={editarForm.handleSubmit(handleEditar)} className="space-y-4">
          <Input
            id="descripcionEdit"
            label="Descripción (opcional)"
            placeholder="Ej: Local esquinero"
            {...editarForm.register('descripcion')}
          />
          <Input
            id="sectorEdit"
            label="Sector (opcional)"
            placeholder="Ej: Zona A"
            {...editarForm.register('sector')}
          />
          <Select id="estadoEdit" label="Estado" {...editarForm.register('estado')}>
            <option value="Disponible">Disponible</option>
            <option value="Mantenimiento">En Mantenimiento</option>
          </Select>
          <div className="flex gap-3 justify-end pt-2">
            <Button variant="secondary" type="button" onClick={() => setModalEditar(null)}>Cancelar</Button>
            <Button type="submit" loading={submitting}>Guardar cambios</Button>
          </div>
        </form>
      </Modal>

      {/* Modal: Asignar dueño */}
      <Modal
        open={!!modalAsignar}
        onClose={() => setModalAsignar(null)}
        title={`Asignar dueño — Puesto ${modalAsignar?.numeroPuesto}`}
      >
        <form onSubmit={asignarForm.handleSubmit(handleAsignar)} className="space-y-4">
          <Select
            id="duenoId"
            label="Seleccionar dueño"
            error={asignarForm.formState.errors.duenoId?.message}
            {...asignarForm.register('duenoId', { required: 'Selecciona un dueño' })}
          >
            <option value="">— Seleccionar —</option>
            {usuarios.map((u) => (
              <option key={u.id} value={u.id}>{u.nombreCompleto} ({u.email})</option>
            ))}
          </Select>
          <div className="flex gap-3 justify-end pt-2">
            <Button variant="secondary" type="button" onClick={() => setModalAsignar(null)}>Cancelar</Button>
            <Button type="submit" loading={submitting}>Asignar</Button>
          </div>
        </form>
      </Modal>

      {/* Modal: Confirmar liberación */}
      <Modal
        open={!!modalLiberar}
        onClose={() => setModalLiberar(null)}
        title={`Liberar puesto ${modalLiberar?.numeroPuesto}`}
      >
        <p className="text-sm text-gray-600 mb-6">
          ¿Estás seguro de que deseas liberar el puesto <strong>{modalLiberar?.numeroPuesto}</strong>?
          Se eliminará la asignación del dueño <strong>{modalLiberar?.duenoNombre}</strong>.
        </p>
        <div className="flex gap-3 justify-end">
          <Button variant="secondary" onClick={() => setModalLiberar(null)}>Cancelar</Button>
          <Button variant="danger" onClick={handleLiberar}>Liberar</Button>
        </div>
      </Modal>

      {/* Modal: Nuevo puesto */}
      <Modal open={modalNuevo} onClose={() => setModalNuevo(false)} title="Nuevo Puesto">
        <form onSubmit={nuevoForm.handleSubmit(handleCrear)} className="space-y-4">
          <Input
            id="numeroPuesto"
            label="Número de puesto"
            placeholder="Ej: A-01"
            error={nuevoForm.formState.errors.numeroPuesto?.message}
            {...nuevoForm.register('numeroPuesto', { required: 'Campo obligatorio' })}
          />
          <Input id="sector" label="Sector (opcional)" placeholder="Ej: Zona A" {...nuevoForm.register('sector')} />
          <Input id="descripcion" label="Descripción (opcional)" {...nuevoForm.register('descripcion')} />
          <div className="flex gap-3 justify-end pt-2">
            <Button variant="secondary" type="button" onClick={() => setModalNuevo(false)}>Cancelar</Button>
            <Button type="submit" loading={submitting}>Crear</Button>
          </div>
        </form>
      </Modal>
    </>
  );
}
