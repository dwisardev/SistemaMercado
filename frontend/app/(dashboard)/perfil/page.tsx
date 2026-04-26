'use client';

import { useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { perfilApi } from '@/lib/api';
import { getAxiosErrorMessage } from '@/lib/utils';
import Navbar from '@/components/layout/Navbar';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import { useToast } from '@/components/ui/Toast';

export default function PerfilPage() {
  const { user } = useAuth();
  const { toast } = useToast();

  const [form, setForm] = useState({ passwordActual: '', nuevaPassword: '', confirmar: '' });
  const [loading, setLoading] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((prev) => ({ ...prev, [e.target.name]: e.target.value }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (form.nuevaPassword !== form.confirmar) {
      toast('Las contraseñas no coinciden.', 'error');
      return;
    }
    if (form.nuevaPassword.length < 6) {
      toast('La nueva contraseña debe tener al menos 6 caracteres.', 'error');
      return;
    }
    setLoading(true);
    try {
      await perfilApi.cambiarPassword({
        passwordActual: form.passwordActual,
        nuevaPassword: form.nuevaPassword,
      });
      toast('Contraseña actualizada correctamente.', 'success');
      setForm({ passwordActual: '', nuevaPassword: '', confirmar: '' });
    } catch (err) {
      toast(getAxiosErrorMessage(err), 'error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Navbar title="Mi Perfil" />
      <div className="p-6 max-w-lg mx-auto space-y-6">
        {/* Info del usuario */}
        <Card>
          <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-4">
            Información de la cuenta
          </h2>
          <dl className="space-y-3">
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Nombre</dt>
              <dd className="text-sm font-medium text-gray-900">{user?.nombreCompleto}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Email</dt>
              <dd className="text-sm font-medium text-gray-900">{user?.email}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Rol</dt>
              <dd className="text-sm font-medium text-gray-900">{user?.rol}</dd>
            </div>
          </dl>
        </Card>

        {/* Cambio de contraseña */}
        <Card>
          <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-4">
            Cambiar contraseña
          </h2>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Input
              label="Contraseña actual"
              name="passwordActual"
              type="password"
              value={form.passwordActual}
              onChange={handleChange}
              required
            />
            <Input
              label="Nueva contraseña"
              name="nuevaPassword"
              type="password"
              value={form.nuevaPassword}
              onChange={handleChange}
              required
            />
            <Input
              label="Confirmar nueva contraseña"
              name="confirmar"
              type="password"
              value={form.confirmar}
              onChange={handleChange}
              required
            />
            <Button type="submit" loading={loading} className="w-full">
              Actualizar contraseña
            </Button>
          </form>
        </Card>
      </div>
    </>
  );
}
