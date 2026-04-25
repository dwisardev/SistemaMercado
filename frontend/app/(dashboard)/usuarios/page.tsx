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

interface UsuarioForm {
  nombreCompleto: string;
  email: string;
  password: string;
  rol: Rol;
}

const rolColor: Record<Rol, 'blue' | 'green' | 'purple'> = {
  Admin: 'blue', Cajero: 'green', Dueno: 'purple',
};

export default function UsuariosPage() {
  const { hasRole } = useAuth();
  const router = useRouter();
  const { toast } = useToast();
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalNuevo, setModalNuevo] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [search, setSearch] = useState('');

  const { register, handleSubmit, reset, formState: { errors } } = useForm<UsuarioForm>({
    defaultValues: { rol: 'Cajero' },
  });

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
      reset();
      toast('Usuario creado correctamente', 'success');
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
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {filtered.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="py-8 text-center text-gray-400">
                        No se encontraron usuarios
                      </td>
                    </tr>
                  ) : (
                    filtered.map((u) => (
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
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </div>

      <Modal open={modalNuevo} onClose={() => setModalNuevo(false)} title="Nuevo Usuario">
        <form onSubmit={handleSubmit(handleCrear)} className="space-y-4">
          <Input
            id="nombreCompleto"
            label="Nombre completo"
            placeholder="Juan Pérez"
            error={errors.nombreCompleto?.message}
            {...register('nombreCompleto', { required: 'Campo obligatorio' })}
          />
          <Input
            id="emailUser"
            label="Correo electrónico"
            type="email"
            placeholder="juan@mercado.com"
            error={errors.email?.message}
            {...register('email', {
              required: 'Campo obligatorio',
              pattern: { value: /\S+@\S+\.\S+/, message: 'Correo inválido' },
            })}
          />
          <Input
            id="passwordUser"
            label="Contraseña"
            type="password"
            placeholder="Mínimo 6 caracteres"
            error={errors.password?.message}
            {...register('password', { required: 'Campo obligatorio', minLength: { value: 6, message: 'Mínimo 6 caracteres' } })}
          />
          <Select
            id="rolUser"
            label="Rol"
            error={errors.rol?.message}
            {...register('rol', { required: 'Selecciona un rol' })}
          >
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
    </>
  );
}
