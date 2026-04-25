'use client';

import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { usuariosApi } from '@/lib/api';
import type { Usuario, Rol } from '@/lib/types';
import { formatDate, getAxiosErrorMessage } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';
import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import Select from '@/components/ui/Select';
import { useToast } from '@/components/ui/Toast';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import Pagination from '@/components/ui/Pagination';
import { usePagination } from '@/lib/usePagination';

interface UsuarioForm {
  nombreCompleto: string;
  email: string;
  password: string;
  rol: Rol;
}

interface EditarForm {
  nombreCompleto: string;
  rol: Rol;
  nuevaPassword?: string;
}

const rolColor: Record<Rol, 'blue' | 'green' | 'purple'> = {
  Admin: 'blue', Cajero: 'green', Dueno: 'purple',
};

export default function UsuariosPage() {
  const { hasRole, user: currentUser } = useAuth();
  const router = useRouter();
  const { toast } = useToast();
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalNuevo, setModalNuevo] = useState(false);
  const [modalEditar, setModalEditar] = useState<Usuario | null>(null);
  const [modalToggle, setModalToggle] = useState<Usuario | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [search, setSearch] = useState('');

  const nuevoForm = useForm<UsuarioForm>({ defaultValues: { rol: 'Cajero' } });
  const editarForm = useForm<EditarForm>();

  useEffect(() => {
    if (!hasRole('Admin')) { router.push('/dashboard'); return; }
    usuariosApi.getAll()
      .then(setUsuarios)
      .catch(() => toast('Error cargando usuarios', 'error'))
      .finally(() => setLoading(false));
  }, [hasRole, router, toast]);

  const handleCrear = async (values: UsuarioForm) => {
    setSubmitting(true);
    try {
      const nuevo = await usuariosApi.create(values);
      setUsuarios((prev) => [nuevo, ...prev]);
      setModalNuevo(false);
      nuevoForm.reset();
      toast('Usuario creado correctamente', 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const abrirEditar = (u: Usuario) => {
    editarForm.reset({ nombreCompleto: u.nombreCompleto, rol: u.rol, nuevaPassword: '' });
    setModalEditar(u);
  };

  const handleEditar = async (values: EditarForm) => {
    if (!modalEditar) return;
    setSubmitting(true);
    try {
      const updated = await usuariosApi.update(modalEditar.id, {
        nombreCompleto: values.nombreCompleto,
        rol: values.rol,
        nuevaPassword: values.nuevaPassword || undefined,
      });
      setUsuarios((prev) => prev.map((u) => (u.id === updated.id ? updated : u)));
      setModalEditar(null);
      toast('Usuario actualizado', 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleActivo = async () => {
    if (!modalToggle) return;
    setSubmitting(true);
    try {
      const updated = await usuariosApi.update(modalToggle.id, { activo: !modalToggle.activo });
      setUsuarios((prev) => prev.map((u) => (u.id === updated.id ? updated : u)));
      setModalToggle(null);
      toast(`Usuario ${updated.activo ? 'activado' : 'desactivado'}`, 'success');
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const filtered = usuarios.filter((u) =>
    u.nombreCompleto.toLowerCase().includes(search.toLowerCase()) ||
    u.email.toLowerCase().includes(search.toLowerCase()) ||
    u.rol.toLowerCase().includes(search.toLowerCase())
  );
  const { page, setPage, totalPages, paged, total } = usePagination(filtered, 20);

  return (
    <>
      <Navbar title="Usuarios" />
      <div className="p-6">
        <Card>
          <div className="flex flex-col sm:flex-row gap-3 mb-6">
            <input
              type="text"
              placeholder="Buscar por nombre, email o rol..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="flex-1 rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <Button onClick={() => setModalNuevo(true)}>+ Nuevo Usuario</Button>
          </div>

          {loading ? (
            <div className="space-y-3">
              {[...Array(5)].map((_, i) => <div key={i} className="h-12 bg-gray-100 rounded animate-pulse" />)}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-gray-500 border-b border-gray-200">
                    <th className="pb-3 font-medium">Nombre</th>
                    <th className="pb-3 font-medium">Email</th>
                    <th className="pb-3 font-medium">Rol</th>
                    <th className="pb-3 font-medium">Estado</th>
                    <th className="pb-3 font-medium">Creado</th>
                    <th className="pb-3 font-medium text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {filtered.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="py-8 text-center text-gray-400">
                        No se encontraron usuarios
                      </td>
                    </tr>
                  ) : (
                    paged.map((u) => (
                      <tr key={u.id} className="hover:bg-gray-50">
                        <td className="py-3 font-medium text-gray-800">{u.nombreCompleto}</td>
                        <td className="py-3 text-gray-500">{u.email}</td>
                        <td className="py-3">
                          <Badge color={rolColor[u.rol] ?? 'gray'}>{u.rol}</Badge>
                        </td>
                        <td className="py-3">
                          <Badge color={u.activo ? 'green' : 'gray'}>
                            {u.activo ? 'Activo' : 'Inactivo'}
                          </Badge>
                        </td>
                        <td className="py-3 text-gray-400 text-xs">{formatDate(u.createdAt)}</td>
                        <td className="py-3 text-right">
                          <div className="flex gap-2 justify-end">
                            <Button size="sm" variant="secondary" onClick={() => abrirEditar(u)}>
                              Editar
                            </Button>
                            {u.id !== currentUser?.usuarioId && (
                              <Button
                                size="sm"
                                variant={u.activo ? 'danger' : 'secondary'}
                                onClick={() => setModalToggle(u)}
                              >
                                {u.activo ? 'Desactivar' : 'Activar'}
                              </Button>
                            )}
                          </div>
                        </td>
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

      {/* Modal Nuevo Usuario */}
      <Modal open={modalNuevo} onClose={() => setModalNuevo(false)} title="Nuevo Usuario">
        <form onSubmit={nuevoForm.handleSubmit(handleCrear)} className="space-y-4">
          <Input
            id="nombreCompleto"
            label="Nombre completo"
            placeholder="Juan Pérez"
            error={nuevoForm.formState.errors.nombreCompleto?.message}
            {...nuevoForm.register('nombreCompleto', { required: 'Campo obligatorio' })}
          />
          <Input
            id="emailUser"
            label="Correo electrónico"
            type="email"
            placeholder="juan@mercado.com"
            error={nuevoForm.formState.errors.email?.message}
            {...nuevoForm.register('email', {
              required: 'Campo obligatorio',
              pattern: { value: /\S+@\S+\.\S+/, message: 'Correo inválido' },
            })}
          />
          <Input
            id="passwordUser"
            label="Contraseña"
            type="password"
            placeholder="Mínimo 6 caracteres"
            error={nuevoForm.formState.errors.password?.message}
            {...nuevoForm.register('password', { required: 'Campo obligatorio', minLength: { value: 6, message: 'Mínimo 6 caracteres' } })}
          />
          <Select id="rolUser" label="Rol" {...nuevoForm.register('rol', { required: true })}>
            <option value="Admin">Admin</option>
            <option value="Cajero">Cajero</option>
            <option value="Dueno">Dueño</option>
          </Select>
          <div className="flex gap-3 justify-end pt-2">
            <Button variant="secondary" type="button" onClick={() => setModalNuevo(false)}>Cancelar</Button>
            <Button type="submit" loading={submitting}>Crear Usuario</Button>
          </div>
        </form>
      </Modal>

      {/* Modal Editar Usuario */}
      <Modal open={!!modalEditar} onClose={() => setModalEditar(null)} title={`Editar — ${modalEditar?.nombreCompleto}`}>
        <form onSubmit={editarForm.handleSubmit(handleEditar)} className="space-y-4">
          <Input
            id="nombreEdit"
            label="Nombre completo"
            error={editarForm.formState.errors.nombreCompleto?.message}
            {...editarForm.register('nombreCompleto', { required: 'Campo obligatorio' })}
          />
          <Select id="rolEdit" label="Rol" {...editarForm.register('rol')}>
            <option value="Admin">Admin</option>
            <option value="Cajero">Cajero</option>
            <option value="Dueno">Dueño</option>
          </Select>
          <Input
            id="nuevaPasswordEdit"
            label="Nueva contraseña (dejar vacío para no cambiar)"
            type="password"
            placeholder="Mínimo 6 caracteres"
            error={editarForm.formState.errors.nuevaPassword?.message}
            {...editarForm.register('nuevaPassword', {
              validate: (v) => !v || v.length >= 6 || 'Mínimo 6 caracteres',
            })}
          />
          <div className="flex gap-3 justify-end pt-2">
            <Button variant="secondary" type="button" onClick={() => setModalEditar(null)}>Cancelar</Button>
            <Button type="submit" loading={submitting}>Guardar cambios</Button>
          </div>
        </form>
      </Modal>

      {/* Modal Toggle Activo */}
      <Modal
        open={!!modalToggle}
        onClose={() => setModalToggle(null)}
        title={modalToggle?.activo ? 'Desactivar usuario' : 'Activar usuario'}
      >
        <p className="text-sm text-gray-600 mb-6">
          ¿Deseas <strong>{modalToggle?.activo ? 'desactivar' : 'activar'}</strong> al usuario{' '}
          <strong>{modalToggle?.nombreCompleto}</strong>?
          {modalToggle?.activo && ' No podrá iniciar sesión mientras esté inactivo.'}
        </p>
        <div className="flex gap-3 justify-end">
          <Button variant="secondary" onClick={() => setModalToggle(null)}>Cancelar</Button>
          <Button
            variant={modalToggle?.activo ? 'danger' : 'primary'}
            loading={submitting}
            onClick={handleToggleActivo}
          >
            {modalToggle?.activo ? 'Desactivar' : 'Activar'}
          </Button>
        </div>
      </Modal>
    </>
  );
}
