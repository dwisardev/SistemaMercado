'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useAuth } from '@/context/AuthContext';
import { getAxiosErrorMessage } from '@/lib/utils';
import Input from '@/components/ui/Input';
import Button from '@/components/ui/Button';

interface FormValues {
  email: string;
  password: string;
}

export default function LoginPage() {
  const { login } = useAuth();
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>();

  const onSubmit = async (data: FormValues) => {
    setError('');
    setLoading(true);
    try {
      await login(data);
    } catch (err) {
      setError(getAxiosErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-600 to-blue-800 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md p-8">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="w-14 h-14 bg-blue-600 rounded-xl flex items-center justify-center mx-auto mb-3 shadow-lg">
            <span className="text-white text-2xl font-bold">M</span>
          </div>
          <h1 className="text-2xl font-bold text-gray-900">MercaGest</h1>
          <p className="text-sm text-gray-500 mt-1">Sistema de Gestión de Mercado</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            id="email"
            label="Correo electrónico"
            type="email"
            placeholder="usuario@mercado.com"
            autoComplete="email"
            error={errors.email?.message}
            {...register('email', {
              required: 'El correo es obligatorio',
              pattern: { value: /\S+@\S+\.\S+/, message: 'Correo inválido' },
            })}
          />

          <Input
            id="password"
            label="Contraseña"
            type="password"
            placeholder="••••••••"
            autoComplete="current-password"
            error={errors.password?.message}
            {...register('password', { required: 'La contraseña es obligatoria' })}
          />

          {error && (
            <div className="bg-red-50 border border-red-200 rounded-lg px-4 py-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <Button type="submit" size="lg" loading={loading} className="w-full mt-2">
            Iniciar sesión
          </Button>
        </form>
      </div>
    </div>
  );
}
