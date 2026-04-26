'use client';

import { useForm } from 'react-hook-form';
import { useAuth } from '@/context/AuthContext';
import { getAxiosErrorMessage } from '@/lib/utils';
import { useState } from 'react';
import {
  EnvelopeIcon,
  LockClosedIcon,
  ShoppingBagIcon,
  ExclamationCircleIcon,
  ArrowRightEndOnRectangleIcon,
} from '@heroicons/react/24/outline';
import Input from '@/components/ui/Input';

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
    <div className="min-h-screen flex">

      {/* ── Panel izquierdo: formulario ───────────────────────────── */}
      <div className="w-full lg:w-[42%] flex flex-col justify-center px-8 py-14 bg-white lg:px-14">

        {/* Logo */}
        <div className="flex items-center gap-3 mb-10">
          <div className="w-12 h-12 rounded-2xl bg-orange-600 flex items-center justify-center shadow-lg">
            <ShoppingBagIcon className="w-7 h-7 text-white" />
          </div>
          <div>
            <h1 className="text-lg font-bold text-gray-900 leading-tight">MercaGest</h1>
            <p className="text-xs text-orange-600 font-medium">Gestión de Mercados</p>
          </div>
        </div>

        <h2 className="text-3xl font-bold text-gray-900">Iniciar sesión</h2>
        <p className="text-gray-500 text-sm mt-1 mb-8">
          ¿No tienes cuenta?{' '}
          <span className="text-orange-600 font-medium cursor-pointer hover:underline">
            Contacta al administrador
          </span>
        </p>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          <Input
            id="email"
            label="Correo electrónico"
            type="email"
            autoComplete="email"
            placeholder="usuario@mercado.com"
            icon={<EnvelopeIcon className="w-4 h-4" />}
            error={errors.email?.message}
            className="focus:ring-orange-500 focus:border-orange-500"
            {...register('email', {
              required: 'El correo es obligatorio',
              pattern: { value: /\S+@\S+\.\S+/, message: 'Correo inválido' },
            })}
          />

          <Input
            id="password"
            label="Contraseña"
            type="password"
            autoComplete="current-password"
            placeholder="••••••••"
            icon={<LockClosedIcon className="w-4 h-4" />}
            error={errors.password?.message}
            className="focus:ring-orange-500 focus:border-orange-500"
            {...register('password', { required: 'La contraseña es obligatoria' })}
          />

          {error && (
            <div className="flex items-center gap-2 bg-red-50 border border-red-200 rounded-xl px-4 py-3 text-sm text-red-700">
              <ExclamationCircleIcon className="w-4 h-4 shrink-0" />
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full flex items-center justify-center gap-2
              bg-orange-600 hover:bg-orange-700 active:bg-orange-800
              text-white font-bold py-3 px-4 rounded-xl transition-colors shadow-md
              disabled:opacity-60 disabled:cursor-not-allowed mt-1"
          >
            {loading ? (
              'Ingresando...'
            ) : (
              <>
                <ArrowRightEndOnRectangleIcon className="w-5 h-5" />
                Ingresar al sistema
              </>
            )}
          </button>
        </form>

        <p className="text-center text-xs text-gray-400 mt-10">
          © {new Date().getFullYear()} MercaGest · Hecho con orgullo en Perú 🇵🇪
        </p>
      </div>

      {/* ── Panel derecho: imagen mercado peruano ─────────────────── */}
      {/* Sube tu imagen a frontend/public/mercado.jpg */}
      <div
        className="hidden lg:flex lg:w-[58%] relative overflow-hidden bg-orange-900"
        style={{ backgroundImage: "url('/mercado.jpg')", backgroundSize: 'cover', backgroundPosition: 'center' }}
      >
        {/* Overlay degradado para legibilidad */}
        <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/10 to-transparent" />
        {/* Texto inferior */}
        <div className="absolute bottom-10 left-0 right-0 text-center px-10">
          <p className="text-white font-bold text-xl drop-shadow-lg">
            ¡Bienvenido al mercado de tu comunidad!
          </p>
          <p className="text-white/75 text-sm mt-1 drop-shadow">
            Productos frescos · Precios justos · Sabor peruano 🇵🇪
          </p>
        </div>
      </div>

    </div>
  );
}
