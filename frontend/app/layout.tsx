import type { Metadata } from 'next';
import { Geist } from 'next/font/google';
import './globals.css';
import { AuthProvider } from '@/context/AuthContext';
import { ToastProvider } from '@/components/ui/Toast';

const geist = Geist({ subsets: ['latin'], variable: '--font-geist-sans' });

export const metadata: Metadata = {
  title: 'MercaGest — Sistema de Gestión de Mercado',
  description: 'Administración de puestos, pagos y reportes',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="es" className={`${geist.variable} h-full`}>
      <body className="min-h-full bg-gray-50 font-sans antialiased">
        <AuthProvider>
          <ToastProvider>{children}</ToastProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
