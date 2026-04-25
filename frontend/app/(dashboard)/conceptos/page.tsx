'use client';

import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { conceptosApi } from '@/lib/api';
import type { ConceptoCobro } from '@/lib/types';
import { formatCurrency, getAxiosErrorMessage } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';
import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import { useToast } from '@/components/ui/Toast';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';

interface ConceptoForm {
  nombre: string;
  descripcion?: string;
  monto: string;
  diaEmision?: string;
}

export default function ConceptosPage() {
  const { hasRole } = useAuth();
  const router = useRouter();
  const { toast } = useToast();
  const [conceptos, setConceptos] = useState<ConceptoCobro[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalNuevo, setModalNuevo] = useState(false);
  const [modalEditar, setModalEditar] = useState<ConceptoCobro | null>(null);
  const [modalEliminar, setModalEliminar] = useState<ConceptoCobro | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const nuevoForm = useForm<ConceptoForm>();
  const editarForm = useForm<ConceptoForm>();

  useEffect(() => {
    if (!hasRole('Admin')) { router.push('/dashboard'); return; }
    conceptosApi.getAll()
      .then(setConceptos)
      .catch(() => toast('Error cargando conceptos', 'error'))
      .finally(() => setLoading(false));
  }, [hasRole, router, toast]);

  const handleCrear = async (values: ConceptoForm) => {
    setSubmitting(true);
    try {
      const nuevo = await conceptosApi.create({
        nombre: values.nombre,
        descripcion: values.descripcion,
        monto: parseFloat(values.monto),
        diaEmision: values.diaEmision ? parseInt(values.diaEmision) : undefined,
      });
      setConceptos((prev) => [nuevo, ...prev]);
      setModalNuevo(false);
      nuevoForm.reset();
      toast('Concepto creado', 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleEditar = async (values: ConceptoForm) => {
    if (!modalEditar) return;
    setSubmitting(true);
    try {
      const updated = await conceptosApi.update(modalEditar.id, {
        nombre: values.nombre,
        descripcion: values.descripcion,
        monto: parseFloat(values.monto),
        diaEmision: values.diaEmision ? parseInt(values.diaEmision) : undefined,
      });
      setConceptos((prev) => prev.map((c) => (c.id === updated.id ? updated : c)));
      setModalEditar(null);
      toast('Concepto actualizado', 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleEliminar = async () => {
    if (!modalEliminar) return;
    setSubmitting(true);
    try {
      await conceptosApi.delete(modalEliminar.id);
      setConceptos((prev) => prev.map((c) =>
        c.id === modalEliminar.id ? { ...c, activo: false } : c
      ));
      setModalEliminar(null);
      toast('Concepto desactivado', 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const abrirEditar = (c: ConceptoCobro) => {
    editarForm.reset({
      nombre: c.nombre,
      descripcion: c.descripcion ?? '',
      monto: String(c.monto),
    });
    setModalEditar(c);
  };

  return (
    <>
      <Navbar title="Conceptos de Cobro" />
      <div className="p-6">
        <Card>
          <div className="flex justify-end mb-6">
            <Button onClick={() => { nuevoForm.reset(); setModalNuevo(true); }}>
              + Nuevo Concepto
            </Button>
          </div>

          {loading ? (
            <div className="space-y-3">
              {[...Array(4)].map((_, i) => (
                <div key={i} className="h-12 bg-gray-100 rounded animate-pulse" />
              ))}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-gray-500 border-b border-gray-200">
                    <th className="pb-3 font-medium">Nombre</th>
                    <th className="pb-3 font-medium">Descripción</th>
                    <th className="pb-3 font-medium text-right">Monto default</th>
                    <th className="pb-3 font-medium">Estado</th>
                    <th className="pb-3 font-medium text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {conceptos.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="py-8 text-center text-gray-400">
                        No hay conceptos de cobro registrados
                      </td>
                    </tr>
                  ) : (
                    conceptos.map((c) => (
                      <tr key={c.id} className="hover:bg-gray-50 transition-colors">
                        <td className="py-3 font-semibold text-gray-800">{c.nombre}</td>
                        <td className="py-3 text-gray-500">{c.descripcion ?? '—'}</td>
                        <td className="py-3 text-right font-medium">{formatCurrency(c.monto)}</td>
                        <td className="py-3">
                          <Badge color={c.activo ? 'green' : 'gray'}>
                            {c.activo ? 'Activo' : 'Inactivo'}
                          </Badge>
                        </td>
                        <td className="py-3">
                          <div className="flex gap-2 justify-end">
                            <Button size="sm" variant="secondary" onClick={() => abrirEditar(c)}>
                              Editar
                            </Button>
                            {c.activo && (
                              <Button size="sm" variant="danger" onClick={() => setModalEliminar(c)}>
                                Desactivar
                              </Button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </div>

      {/* Modal Nuevo */}
      <Modal open={modalNuevo} onClose={() => setModalNuevo(false)} title="Nuevo Concepto de Cobro">
        <form onSubmit={nuevoForm.handleSubmit(handleCrear)} className="space-y-4">
          <Input
            id="nombre"
            label="Nombre"
            placeholder="Ej: Alquiler mensual"
            error={nuevoForm.formState.errors.nombre?.message}
            {...nuevoForm.register('nombre', { required: 'Campo obligatorio' })}
          />
          <Input
            id="descripcion"
            label="Descripción (opcional)"
            {...nuevoForm.register('descripcion')}
          />
          <Input
            id="monto"
            label="Monto default (S/.)"
            type="number"
            step="0.01"
            error={nuevoForm.formState.errors.monto?.message}
            {...nuevoForm.register('monto', {
              required: 'Campo obligatorio',
              min: { value: 0.01, message: 'Debe ser mayor a 0' },
            })}
          />
          <Input
            id="diaEmision"
            label="Día de emisión (1-31, opcional)"
            type="number"
            min={1}
            max={31}
            {...nuevoForm.register('diaEmision')}
          />
          <div className="flex gap-3 justify-end pt-2">
            <Button variant="secondary" type="button" onClick={() => setModalNuevo(false)}>Cancelar</Button>
            <Button type="submit" loading={submitting}>Crear</Button>
          </div>
        </form>
      </Modal>

      {/* Modal Editar */}
      <Modal open={!!modalEditar} onClose={() => setModalEditar(null)} title={`Editar — ${modalEditar?.nombre}`}>
        <form onSubmit={editarForm.handleSubmit(handleEditar)} className="space-y-4">
          <Input
            id="nombreEdit"
            label="Nombre"
            error={editarForm.formState.errors.nombre?.message}
            {...editarForm.register('nombre', { required: 'Campo obligatorio' })}
          />
          <Input
            id="descripcionEdit"
            label="Descripción (opcional)"
            {...editarForm.register('descripcion')}
          />
          <Input
            id="montoEdit"
            label="Monto default (S/.)"
            type="number"
            step="0.01"
            error={editarForm.formState.errors.monto?.message}
            {...editarForm.register('monto', {
              required: 'Campo obligatorio',
              min: { value: 0.01, message: 'Debe ser mayor a 0' },
            })}
          />
          <Input
            id="diaEmisionEdit"
            label="Día de emisión (1-31, opcional)"
            type="number"
            min={1}
            max={31}
            {...editarForm.register('diaEmision')}
          />
          <div className="flex gap-3 justify-end pt-2">
            <Button variant="secondary" type="button" onClick={() => setModalEditar(null)}>Cancelar</Button>
            <Button type="submit" loading={submitting}>Guardar cambios</Button>
          </div>
        </form>
      </Modal>

      {/* Modal Desactivar */}
      <Modal open={!!modalEliminar} onClose={() => setModalEliminar(null)} title="Desactivar Concepto">
        <p className="text-sm text-gray-600 mb-6">
          ¿Deseas desactivar el concepto <strong>{modalEliminar?.nombre}</strong>?
          No se elimina: quedará inactivo y no podrá usarse en nuevas deudas.
        </p>
        <div className="flex gap-3 justify-end">
          <Button variant="secondary" onClick={() => setModalEliminar(null)}>Cancelar</Button>
          <Button variant="danger" loading={submitting} onClick={handleEliminar}>Desactivar</Button>
        </div>
      </Modal>
    </>
  );
}
