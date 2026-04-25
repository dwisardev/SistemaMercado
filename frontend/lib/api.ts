import api from './axios';
import type {
  LoginRequest, AuthUser,
  Puesto, CreatePuestoDto, AsignarDuenoDto,
  ConceptoCobro,
  Deuda, CargaIndividualDeudaDto, CargaMasivaDeudaDto, CargaMasivaResult,
  Pago, RegistrarPagoDto, AnularPagoDto,
  CajaDiariaDto, MorosidadDto, DeudaPendienteDto,
  Usuario, CreateUsuarioDto,
  Notificacion,
} from './types';

// ─── Auth ─────────────────────────────────────────────────────────────────────
export const authApi = {
  login: (dto: LoginRequest) =>
    api.post<AuthUser>('/api/auth/login', dto).then((r) => r.data),
};

// ─── Puestos ──────────────────────────────────────────────────────────────────
export const puestosApi = {
  getAll: () => api.get<Puesto[]>('/api/puestos').then((r) => r.data),
  getMisPuestos: () => api.get<Puesto[]>('/api/puestos/mis-puestos').then((r) => r.data),
  update: (id: string, dto: { descripcion?: string; sector?: string; estado?: string }) =>
    api.patch<Puesto>(`/api/puestos/${id}`, dto).then((r) => r.data),
  create: (dto: CreatePuestoDto) =>
    api.post<Puesto>('/api/puestos', dto).then((r) => r.data),
  asignarDueno: (id: string, dto: AsignarDuenoDto) =>
    api.patch<Puesto>(`/api/puestos/${id}/asignar-dueno`, dto).then((r) => r.data),
  liberar: (id: string) =>
    api.patch<Puesto>(`/api/puestos/${id}/liberar`).then((r) => r.data),
};

// ─── Conceptos ────────────────────────────────────────────────────────────────
export const conceptosApi = {
  getAll: () => api.get<ConceptoCobro[]>('/api/conceptos').then((r) => r.data),
  create: (dto: { nombre: string; descripcion?: string; monto: number; diaEmision?: number }) =>
    api.post<ConceptoCobro>('/api/conceptos', dto).then((r) => r.data),
  update: (id: string, dto: { nombre?: string; descripcion?: string; monto?: number; diaEmision?: number; activo?: boolean }) =>
    api.patch<ConceptoCobro>(`/api/conceptos/${id}`, dto).then((r) => r.data),
  delete: (id: string) =>
    api.delete(`/api/conceptos/${id}`).then((r) => r.data),
};

// ─── Deudas ───────────────────────────────────────────────────────────────────
export const deudasApi = {
  cargarIndividual: (dto: CargaIndividualDeudaDto) =>
    api.post<Deuda>('/api/deudas/individual', dto).then((r) => r.data),
  cargarMasiva: (dto: CargaMasivaDeudaDto) =>
    api.post<CargaMasivaResult>('/api/deudas/masiva', dto).then((r) => r.data),
  getPorPuesto: (puestoId: string) =>
    api.get<Deuda[]>('/api/deudas', { params: { puestoId } }).then((r) => r.data),
  getFiltradas: (params: { puestoId?: string; estado?: string; periodo?: string }) =>
    api.get<Deuda[]>('/api/deudas', { params }).then((r) => r.data),
};

// ─── Pagos ────────────────────────────────────────────────────────────────────
export const pagosApi = {
  getAll: (params?: { fechaInicio?: string; fechaFin?: string; puestoId?: string; estado?: string }) =>
    api.get<Pago[]>('/api/pagos', { params }).then((r) => r.data),
  registrar: (dto: RegistrarPagoDto) =>
    api.post<Pago>('/api/pagos', dto).then((r) => r.data),
  getComprobante: (id: string) =>
    api.get<Blob>(`/api/pagos/${id}/comprobante`, { responseType: 'blob' }).then((r) => r.data),
  anular: (id: string, dto: AnularPagoDto) =>
    api.patch<Pago>(`/api/pagos/${id}/anular`, dto).then((r) => r.data),
};

// ─── Reportes ─────────────────────────────────────────────────────────────────
export const reportesApi = {
  cajaDiaria: (fecha?: string) =>
    api.get<CajaDiariaDto>('/api/reportes/caja-diaria', { params: { fecha } }).then((r) => r.data),
  morosidad: () =>
    api.get<MorosidadDto[]>('/api/reportes/morosidad').then((r) => r.data),
  deudasPendientes: () =>
    api.get<DeudaPendienteDto[]>('/api/reportes/deudas-pendientes').then((r) => r.data),
};

// ─── Usuarios ─────────────────────────────────────────────────────────────────
export const usuariosApi = {
  getAll: () => api.get<Usuario[]>('/api/usuarios').then((r) => r.data),
  create: (dto: CreateUsuarioDto) =>
    api.post<Usuario>('/api/usuarios', dto).then((r) => r.data),
  update: (id: string, dto: { nombreCompleto?: string; rol?: string; activo?: boolean; nuevaPassword?: string }) =>
    api.patch<Usuario>(`/api/usuarios/${id}`, dto).then((r) => r.data),
};

// ─── Notificaciones ───────────────────────────────────────────────────────────
export const notificacionesApi = {
  getAll: () => api.get<Notificacion[]>('/api/notificaciones').then((r) => r.data),
  marcarLeida: (id: string) =>
    api.patch<Notificacion>(`/api/notificaciones/${id}`).then((r) => r.data),
  marcarTodasLeidas: () =>
    api.patch('/api/notificaciones/todas-leidas').then((r) => r.data),
};
