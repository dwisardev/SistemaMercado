// ─── Auth ─────────────────────────────────────────────────────────────────────
export type Rol = 'Admin' | 'Cajero' | 'Dueno';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthUser {
  token: string;
  usuarioId: string;
  nombreCompleto: string;
  email: string;
  rol: Rol;
  expiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

// ─── Puestos ──────────────────────────────────────────────────────────────────
export type EstadoPuesto = 'Disponible' | 'Ocupado' | 'Mantenimiento';

export interface Puesto {
  id: string;
  numeroPuesto: string;
  descripcion?: string;
  estado: EstadoPuesto;
  duenoId?: string;
  duenoNombre?: string;
  duenoEmail?: string;
  sector?: string;
  tarifaMensual?: number;
  createdAt: string;
}

export interface CreatePuestoDto {
  numeroPuesto: string;
  descripcion?: string;
  sector?: string;
  tarifaMensual?: number;
}

export interface AsignarDuenoDto {
  duenoId: string;
}

// ─── Conceptos ────────────────────────────────────────────────────────────────
export interface ConceptoCobro {
  id: string;
  nombre: string;
  descripcion?: string;
  monto: number;
  activo: boolean;
}

// ─── Deudas ───────────────────────────────────────────────────────────────────
export type EstadoDeuda = 'Pendiente' | 'Pagada' | 'Vencida' | 'Anulada';

export interface Deuda {
  id: string;
  puestoId: string;
  puestoNumero?: string;
  duenoNombre?: string;
  conceptoId: string;
  conceptoNombre?: string;
  monto: number;
  saldoPendiente: number;
  estado: EstadoDeuda;
  fechaVencimiento: string;
  periodo: string;
  createdAt: string;
}

export interface CargaIndividualDeudaDto {
  puestoId: string;
  conceptoId: string;
  monto: number;
  fechaVencimiento: string;
  periodo: string;
}

export interface CargaMasivaDeudaDto {
  conceptoId: string;
  monto: number;
  fechaVencimiento: string;
  periodo: string;
}

export interface CargaMasivaResult {
  total: number;
  exitosos: number;
  fallidos: number;
  errores: string[];
}

// ─── Pagos ────────────────────────────────────────────────────────────────────
export type MetodoPago = 'Efectivo' | 'Transferencia' | 'Cheque' | 'Otro';
export type EstadoPago = 'Activo' | 'Anulado';

export interface Pago {
  id: string;
  deudaId: string;
  puestoNumero?: string;
  duenoNombre?: string;
  montoPagado: number;
  fechaPago: string;
  cajeroNombre?: string;
  numeroComprobante?: string;
  metodo: MetodoPago;
  estado: EstadoPago;
  referenciaPago?: string;
  motivoAnulacion?: string;
}

export interface RegistrarPagoDto {
  deudaId: string;
  montoPagado: number;
  metodo: MetodoPago;
  referenciaPago?: string;
  observaciones?: string;
}

export interface AnularPagoDto {
  motivoAnulacion: string;
}

// ─── Reportes ─────────────────────────────────────────────────────────────────
export interface CajaDiariaDto {
  fecha: string;
  totalRecaudado: number;
  cantidadPagos: number;
  pagos: Pago[];
}

export interface MorosidadDto {
  puestoId: string;
  puestoNumero: string;
  duenoNombre: string;
  deudas: Deuda[];
  totalPendiente: number;
}

export interface DeudaPendienteDto {
  puestoId: string;
  puestoNumero: string;
  duenoNombre: string;
  totalPendiente: number;
  deudas: Deuda[];
}

// ─── Usuarios ─────────────────────────────────────────────────────────────────
export interface Usuario {
  id: string;
  nombreCompleto: string;
  email: string;
  rol: Rol;
  activo: boolean;
  createdAt: string;
}

export interface CreateUsuarioDto {
  nombreCompleto: string;
  email: string;
  password: string;
  rol: Rol;
}

// ─── Notificaciones ───────────────────────────────────────────────────────────
export interface Notificacion {
  id: string;
  titulo: string;
  mensaje: string;
  leida: boolean;
  fechaCreacion: string;
  tipo?: string;
}

// ─── Pagination ───────────────────────────────────────────────────────────────
export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
}
