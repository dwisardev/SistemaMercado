'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useAuth } from '@/context/AuthContext';
import { getAxiosErrorMessage } from '@/lib/utils';

interface FormValues {
  email: string;
  password: string;
}

export default function LoginPage() {
  const { login } = useAuth();
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [showPwd, setShowPwd] = useState(false);

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

        {/* Logo + nombre */}
        <div className="flex items-center gap-3 mb-10">
          <div className="w-12 h-12 rounded-2xl bg-orange-600 flex items-center justify-center shadow-lg">
            <svg viewBox="0 0 24 24" fill="white" className="w-7 h-7">
              <path d="M20 8H4L2 2H0V0h3.27l.94 2H23l-3 8zM4.88 16a2 2 0 102 2 2 2 0 00-2-2zm11 0a2 2 0 102 2 2 2 0 00-2-2zM4 10l1.5 4h13L20 10H4z"/>
            </svg>
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
          {/* Email */}
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-semibold text-gray-700">
              Correo electrónico
            </label>
            <input
              type="email"
              autoComplete="email"
              placeholder="usuario@mercado.com"
              className={`rounded-xl border px-4 py-2.5 text-sm shadow-sm transition
                focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500
                ${errors.email ? 'border-red-400' : 'border-gray-300'}`}
              {...register('email', {
                required: 'El correo es obligatorio',
                pattern: { value: /\S+@\S+\.\S+/, message: 'Correo inválido' },
              })}
            />
            {errors.email && (
              <p className="text-xs text-red-600">{errors.email.message}</p>
            )}
          </div>

          {/* Password */}
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-semibold text-gray-700">Contraseña</label>
            <div className="relative">
              <input
                type={showPwd ? 'text' : 'password'}
                autoComplete="current-password"
                placeholder="••••••••"
                className={`w-full rounded-xl border px-4 py-2.5 pr-11 text-sm shadow-sm transition
                  focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500
                  ${errors.password ? 'border-red-400' : 'border-gray-300'}`}
                {...register('password', { required: 'La contraseña es obligatoria' })}
              />
              <button
                type="button"
                onClick={() => setShowPwd(!showPwd)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition"
                tabIndex={-1}
              >
                {showPwd ? (
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth={2} viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M13.875 18.825A10.05 10.05 0 0112 19c-5 0-9-4-9-7s4-7 9-7a9.93 9.93 0 016.072 2.04M9.879 9.879A3 3 0 1014.12 14.12M3 3l18 18"/>
                  </svg>
                ) : (
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth={2} viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.477 0 8.268 2.943 9.542 7-1.274 4.057-5.065 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
                  </svg>
                )}
              </button>
            </div>
            {errors.password && (
              <p className="text-xs text-red-600">{errors.password.message}</p>
            )}
          </div>

          {error && (
            <div className="bg-red-50 border border-red-200 rounded-xl px-4 py-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-orange-600 hover:bg-orange-700 active:bg-orange-800
              text-white font-bold py-3 px-4 rounded-xl transition-colors shadow-md
              disabled:opacity-60 disabled:cursor-not-allowed mt-1"
          >
            {loading ? 'Ingresando...' : 'Ingresar al sistema'}
          </button>
        </form>

        <p className="text-center text-xs text-gray-400 mt-10">
          © {new Date().getFullYear()} MercaGest · Hecho con orgullo en Perú 🇵🇪
        </p>
      </div>

      {/* ── Panel derecho: imagen mercado peruano ─────────────────── */}
      <div className="hidden lg:flex lg:w-[58%] relative overflow-hidden">
        {/* Sube tu imagen a frontend/public/mercado.jpg y aparecerá aquí */}
        <img
          src="/mercado.jpg"
          alt="Mercado peruano"
          className="w-full h-full object-cover"
        />
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
