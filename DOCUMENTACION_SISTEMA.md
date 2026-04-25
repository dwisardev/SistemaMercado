# MercaGest — Documentación Técnica del Sistema

> Sistema de Gestión de Mercado. Stack: Next.js 15 (frontend) + ASP.NET Core 10 Web API (backend) + PostgreSQL 17 (DB), corriendo en GitHub Codespace.

---

## 1. Arquitectura General

```
Browser
  └─► Next.js :3000  (public)
        ├─ proxy rewrite /api/*  ──► ASP.NET Core :5000  (private)
        │                                  └─► PostgreSQL :5433
        └─ middleware (auth guard)
```

El puerto 5000 del backend es **privado** en Codespace; el browser nunca lo alcanza directamente. El frontend hace de proxy inverso vía `next.config.ts` rewrites, por lo que todas las llamadas a la API van por el mismo origen (:3000) y evitan el bloqueo CORS del browser.

---

## 2. Backend — ASP.NET Core 10 Web API

### 2.1 Estructura de capas

```
SMG.API          ← Controladores, DTOs, Program.cs
SGM.Infrastructure ← Repositorios, Servicios, EF Core, DataSeeder
SMG.Core / SGM.Core ← Entidades, Interfaces, Enums
```

> Nota: el proyecto tiene una inconsistencia de naming (`SMG` vs `SGM`) heredada del setup inicial. No afecta la compilación pero es técnicamente un smell.

### 2.2 Endpoints implementados

| Módulo | Método | Ruta | Roles | Descripción |
|---|---|---|---|---|
| Auth | POST | `/api/auth/login` | — | Login, devuelve JWT |
| Auth | POST | `/api/auth/logout` | Auth | Logout (stateless, solo frontend) |
| Puestos | GET | `/api/puestos` | Auth | Lista todos los puestos |
| Puestos | POST | `/api/puestos` | Admin | Crea un puesto |
| Puestos | PATCH | `/api/puestos/{id}/asignar-dueno` | Admin | Asigna dueño a puesto |
| Puestos | PATCH | `/api/puestos/{id}/liberar` | Admin | Libera puesto (quita dueño) |
| Conceptos | GET | `/api/conceptos` | Auth | Lista todos los conceptos de cobro |
| Deudas | GET | `/api/deudas` | Auth | Deudas filtradas por puestoId (requerido) |
| Deudas | POST | `/api/deudas/individual` | Admin | Crea deuda para un puesto |
| Deudas | POST | `/api/deudas/masiva` | Admin | Crea deuda para todos los puestos activos |
| Pagos | POST | `/api/pagos` | Admin, Cajero | Registra un pago |
| Pagos | PATCH | `/api/pagos/{id}/anular` | Admin | Anula un pago |
| Pagos | GET | `/api/pagos/{id}/comprobante` | Auth | Datos del comprobante (JSON) |
| Reportes | GET | `/api/reportes/caja-diaria` | Admin, Cajero | Pagos del día y total recaudado |
| Reportes | GET | `/api/reportes/morosidad` | Admin, Cajero | Puestos con deudas vencidas |
| Reportes | GET | `/api/reportes/deudas-pendientes` | Admin, Cajero | Resumen de deudas sin pagar |
| Usuarios | GET | `/api/usuarios` | Admin | Lista usuarios |
| Usuarios | POST | `/api/usuarios` | Admin | Crea usuario con hash BCrypt |
| Notificaciones | GET | `/api/notificaciones` | Auth | Notificaciones del usuario actual |
| Notificaciones | PATCH | `/api/notificaciones/{id}` | Auth | Marca notificación como leída |
| Notificaciones | PATCH | `/api/notificaciones/todas-leidas` | Auth | Marca todas como leídas |

### 2.3 Seguridad

- **JWT Bearer**: tokens firmados con HS256, validación de issuer/audience/lifetime. Clock skew = 0.
- **Autorización por rol**: `[Authorize(Roles = "Admin")]`, `[Authorize(Roles = "Admin,Cajero")]`.
- **Contraseñas**: BCrypt (biblioteca `BCrypt.Net-Next`). El `DataSeeder` hashea al arrancar los usuarios que tengan `password_hash` vacío.
- **CORS**: política `"Frontend"` que permite solo `localhost:3000` y la URL de Codespace (construida desde `$CODESPACE_NAME`).

### 2.4 Base de datos

- PostgreSQL 17 en Docker (puerto 5433 en host).
- Schema `sgm`, todos los objetos bajo ese namespace.
- 5 tipos enum custom: `estado_pago`, `metodo_pago`, `estado_deuda`, `estado_puesto`, `rol_usuario`.
- ORM: EF Core 10 con Npgsql 10.0.2.
- Las entidades usan `IEntityTypeConfiguration<T>` para el mapeo de columnas.

### 2.5 Lo que **NO** tiene el backend

| Funcionalidad | Observación |
|---|---|
| Historial de pagos con filtros | `GET /api/pagos` no existe; solo se puede ver el comprobante por ID o el resumen del día |
| CRUD completo de Conceptos | Solo existe `GET /api/conceptos`; no hay POST, PATCH ni DELETE |
| Editar puestos | Solo se puede crear y asignar/liberar dueños; no hay `PATCH /api/puestos/{id}` |
| Vista del Dueño | No hay endpoint propio; el Dueño recibe todos los puestos con `GET /api/puestos` |
| Editar / toggle de usuarios | Solo se puede crear usuarios; no hay `PATCH /api/usuarios/{id}` |
| Filtros por estado y período en deudas | `GET /api/deudas` requiere `puestoId` obligatorio y no acepta `estado` ni `periodo` |
| Notificaciones automáticas | Las notificaciones no se generan al registrar/anular pagos; hay que crearlas manualmente |
| Refresh tokens | El JWT dura 24 h; no hay renovación sin re-login |
| Blacklist de tokens | El logout es solo de lado cliente; un token robado sigue válido hasta expirar |
| Paginación server-side | Los `GET` devuelven todos los registros sin `limit/offset` |
| Generación de PDF del comprobante | `GET /comprobante` devuelve JSON, no PDF (QuestPDF está instalado pero sin implementar) |
| Notificaciones por deuda próxima a vencer | No hay job periódico para alertas preventivas |
| Historial de dueños de puestos | La entidad `HistorialDueno` existe pero no tiene endpoint |
| Migrar `CREATE CAST` a script versionado | Los casts de enum están ejecutados manualmente en la DB; no hay migración EF Core |
| FluentValidation | La validación es básica (nulidades y rangos en el controlador) |
| Rate limiting | No hay protección contra abuso de la API |
| Auditoría | `AuditLog` y su repositorio existen pero ningún controlador los usa |
| Unificar namespaces | La inconsistencia `SMG` vs `SGM` persiste |

---

## 3. Frontend — Next.js 15

### 3.1 Estructura

```
frontend/
├── app/
│   ├── (auth)/login/              ← página de login
│   └── (dashboard)/
│       ├── layout.tsx             ← layout con sidebar
│       ├── dashboard/             ← KPIs + resumen (Admin, Cajero)
│       ├── puestos/               ← gestión de puestos (crear, asignar)
│       ├── deudas/                ← carga individual y masiva
│       ├── pagos/                 ← registrar/anular pagos
│       ├── reportes/              ← caja diaria + morosidad
│       ├── usuarios/              ← gestión de usuarios (crear)
│       └── notificaciones/        ← bandeja de notificaciones
├── components/
│   ├── layout/Navbar.tsx
│   ├── layout/Sidebar.tsx
│   └── ui/
│       └── Badge, Button, Card, Input, Modal, Select, Toast
├── context/AuthContext.tsx
├── lib/
│   ├── api.ts                     ← cliente de la API (axios wrappers)
│   ├── auth.ts                    ← localStorage + cookie mirror para middleware
│   ├── axios.ts                   ← instancia axios con interceptor de token
│   ├── types.ts                   ← todos los tipos TypeScript del dominio
│   └── utils.ts                   ← formatCurrency, formatDate, getAxiosErrorMessage
└── proxy.ts                       ← middleware Next.js (auth guard)
```

### 3.2 Páginas implementadas

**Dashboard** — muestra 4 KPIs (recaudado hoy, pagos hoy, puestos morosos, total morosidad) con tablas de últimos pagos del día y ranking de morosos. Carga en paralelo `caja-diaria` y `morosidad`. Solo Admin y Cajero.

**Puestos** — tabla con búsqueda en tiempo real. Admin puede crear puestos y asignar/liberar dueños desde modales. Los estados usan `Badge` con colores. La liberación usa `window.confirm()` de confirmación.

**Deudas** — dos tabs: carga individual y carga masiva. Solo Admin.

**Pagos** — flujo de dos columnas: izquierda selecciona puesto → filtra deudas pendientes/vencidas; derecha muestra formulario de pago con resumen de la deuda seleccionada.

**Reportes** — tabs: caja diaria (con selector de fecha) y morosidad (cards por puesto con desglose de deudas).

**Usuarios** — tabla con búsqueda. Admin puede crear nuevos usuarios desde un modal. La tabla es de solo lectura.

**Notificaciones** — lista de notificaciones del usuario autenticado con acción de marcar leídas.

**Login** — formulario con validación, manejo de error de credenciales y redirección a `/dashboard` tras login exitoso.

### 3.3 Autenticación en el frontend

1. El login llama a `/api/auth/login`, guarda el `AuthUser` en `localStorage` y lo espeja en una cookie `token` (para que el middleware SSR lo lea).
2. `AuthContext` expone `user`, `loading`, `login()`, `logout()` y `hasRole()`.
3. El middleware (`proxy.ts`) protege todas las rutas excepto `/login` y `/api/*`. Si no hay cookie `token`, redirige a `/login?from=<ruta>`.

### 3.4 Lo que **NO** tiene el frontend

| Funcionalidad | Observación |
|---|---|
| Historial de pagos | No hay página para consultar pagos anteriores al día de hoy |
| Gestión de conceptos | No hay página CRUD para conceptos de cobro |
| Vista del Dueño | El Dueño inicia sesión pero ve el mismo dashboard que el Cajero |
| Editar puestos desde UI | No hay modal para editar descripción, sector o estado |
| Editar usuarios desde UI | La tabla de usuarios es de solo lectura; no se puede editar ni desactivar |
| Modal de confirmación para liberar puesto | La liberación usa `window.confirm()` nativo del navegador |
| Pestaña Consultar en Deudas | No hay forma de filtrar deudas por estado o período desde la UI |
| Paginación en tablas largas | Las tablas muestran todos los registros en una sola página |
| Descarga de comprobante PDF | El endpoint devuelve JSON hasta que el backend implemente QuestPDF |
| Página de perfil (cambio de contraseña propio) | El usuario no puede cambiar su propia contraseña |
| Modo oscuro | No implementado |
| Tests unitarios | No hay cobertura de pruebas para `AuthContext`, hooks ni llamadas a la API |

---

## 4. Problemas encontrados y soluciones

### 4.1 Backend no conectaba al frontend — ERR_CERT_AUTHORITY_INVALID

**Problema:** El frontend tenía `NEXT_PUBLIC_API_URL=http://localhost:5000` en `.env.local`. En producción Codespace, los puertos privados no son accesibles desde el browser directamente; la URL generada por el cliente apuntaba al puerto privado del Codespace y el browser la rechazaba con `ERR_CERT_AUTHORITY_INVALID`.

**Solución:** Vaciar `NEXT_PUBLIC_API_URL` y configurar un rewrite en `next.config.ts`:
```ts
async rewrites() {
  return [{ source: "/api/:path*", destination: "http://localhost:5000/api/:path*" }];
}
```
Así el browser solo habla con `:3000` (público) y Next.js hace el proxy server-side hacia `:5000`.

**Por qué funciona:** Los rewrites se ejecutan en el servidor de Next.js, que sí tiene acceso a `localhost:5000` sin pasar por el firewall de Codespace.

---

### 4.2 Middleware interceptando las llamadas a la API — 404 en /api/*

**Problema:** El middleware `proxy.ts` redirigía a `/login` todas las rutas que no tuvieran cookie `token`, incluyendo las llamadas proxied `/api/*`. Resultado: cada petición del cliente al backend terminaba en 302 → `/login` antes de llegar al backend.

**Solución:** Agregar `api` a la exclusión del matcher:
```ts
matcher: ['/((?!_next/static|_next/image|favicon.ico|login|api).*)']
```

**Por qué:** El matcher del middleware de Next.js usa regex negativo; sin excluir `api`, cualquier ruta que empiece con `/api` también pasa por el guard.

---

### 4.3 Credenciales incorrectas en login — hash BCrypt no coincidía

**Problema:** Aunque la contraseña era `admin123`, la base de datos tenía un hash generado por un proceso externo anterior. El `DataSeeder` solo procesaba usuarios con `password_hash = ''`, por lo que el hash existente (incorrecto) nunca se reemplazaba.

**Solución:**
1. Resetear todos los hashes en la DB: `UPDATE sgm.usuarios SET password_hash = ''`.
2. Cambiar el `DataSeeder` para hashear con la contraseña `admin123`.
3. Reiniciar el backend para que el seeder regenere los hashes.

**Por qué:** BCrypt genera hashes distintos en cada ejecución (salt aleatorio). No hay forma de saber qué contraseña generó el hash anterior; solo se puede reemplazar.

---

### 4.4 Todos los endpoints devolvían 404 — controladores vacíos

**Problema:** Al iniciar el proyecto, la mayoría de controladores, servicios y repositorios eran stubs vacíos. Solo `AuthController` estaba implementado.

**Solución:** Implementación completa de la siguiente lista:
- **Repositorios:** `PuestoRepository`, `DeudaRepository`, `PagoRepository`, `ConceptoCobroRepository`, `NotificacionRepository`
- **Servicios:** `PuestoService`, `DeudaService`, `PagoService`, `ConceptoCobroService`, `UsuarioService`, `NotificacionService`
- **Controladores:** `PuestosController`, `DeudasController`, `PagosController`, `ConceptosCobroController`, `ReportesController`, `UsuariosController`, `NotificacionesController`
- **DTOs de request/response:** todos los modelos de entrada y salida de cada endpoint
- **Configuración EF Core:** `NotificacionConfiguration` (nueva), `PagoConfiguration` (corregida), `ConceptoCobroConfiguration` (columna `dia_emision` faltante)

---

### 4.5 500 en POST /api/pagos y POST /api/deudas — enums de PostgreSQL

**Problema:** Al insertar o actualizar registros con columnas de tipo enum custom (`sgm.estado_deuda`, `sgm.estado_pago`, etc.), PostgreSQL rechazaba con:
```
column "estado" is of type estado_deuda but expression is of type character varying
```
EF Core con `HasConversion` convierte los enums de C# a `varchar`. PostgreSQL no hace cast implícito de `varchar` al tipo enum custom.

**Intentos que no funcionaron completamente:**
- Agregar `.HasMaxLength(20)` a las propiedades con `HasConversion`: cambió el mensaje de error de `character varying` a `character varying(20)`, misma causa raíz.
- `NpgsqlDataSourceBuilder.MapEnum<T>()` + `HasPostgresEnum<T>()` en `OnModelCreating`: correctos en teoría, pero en Npgsql EF Core 10.0.1 no resuelven por completo el type mapping cuando `HasConversion` ya fue eliminado (EF Core seguía emitiendo `varchar`).

**Solución definitiva — dos niveles:**
1. Configuración Npgsql correcta (preparación para futuras versiones y para reads):
   ```csharp
   // Program.cs
   var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
   var translator = new NpgsqlSnakeCaseNameTranslator();
   dataSourceBuilder.MapEnum<EstadoPago>("sgm.estado_pago", translator);
   // ... los 5 tipos enum
   ```
   ```csharp
   // AppDbContext.OnModelCreating
   modelBuilder.HasPostgresEnum<EstadoPago>("sgm", "estado_pago", translator);
   // ... los 5 tipos enum
   ```
2. **Casts de asignación en PostgreSQL** (fix definitivo para writes):
   ```sql
   CREATE CAST (varchar AS sgm.estado_deuda) WITH INOUT AS ASSIGNMENT;
   CREATE CAST (text    AS sgm.estado_deuda) WITH INOUT AS ASSIGNMENT;
   -- repetido para los 5 tipos enum
   ```
   `WITH INOUT` usa la función de input del enum (que ya acepta text). `AS ASSIGNMENT` permite el cast en INSERT/UPDATE sin requerir cast explícito en la expresión.

**Por qué el cast de PostgreSQL:** Es la única solución que no depende de la implementación interna de Npgsql EF Core. Si la capa ORM cambia de comportamiento en una versión futura, el cast sigue siendo la red de seguridad.

---

### 4.6 500 en GET /api/conceptos — columna `dia_emision` faltante

**Problema:** `ConceptoCobroConfiguration` no tenía el mapeo de la propiedad `DiaEmision` al nombre de columna `dia_emision`. EF Core intentaba buscar `DiaEmision` (PascalCase) en PostgreSQL, que no existe.

**Solución:** Agregar `builder.Property(cc => cc.DiaEmision).HasColumnName("dia_emision");`

---

### 4.7 Error de compilación — interfaz `internal` en ReporteService

**Problema:** `IReporteService` estaba declarada como `internal`. `ReporteService : IReporteService` fallaba al compilar porque la clase pública no puede implementar una interfaz interna en otro assembly.

**Solución:** `ReporteService` implementa solo los métodos concretos sin heredar la interfaz `internal`. Los reportes se implementaron directamente en `ReportesController` con consultas a `AppDbContext`.

---

## 5. Mejoras recomendadas

### Backend

| Prioridad | Mejora |
|---|---|
| Alta | Historial de pagos con filtros (`GET /api/pagos?fechaInicio=&fechaFin=&puestoId=&estado=`) |
| Alta | CRUD completo de Conceptos (`POST`, `PATCH`, `DELETE /api/conceptos/{id}`) |
| Alta | Editar puestos (`PATCH /api/puestos/{id}`) |
| Alta | Vista del Dueño (`GET /api/puestos/mis-puestos`) |
| Alta | Editar y toggle de usuarios (`PATCH /api/usuarios/{id}`) |
| Alta | Filtros por estado y período en deudas |
| Alta | Notificaciones automáticas al registrar/anular pagos |
| Alta | Generación de PDF del comprobante con QuestPDF |
| Alta | Refresh tokens + blacklist de JWT |
| Media | Job periódico para notificaciones de deuda próxima a vencer |
| Media | Migrar `CREATE CAST` a script SQL versionado |
| Media | FluentValidation en DTOs |
| Baja | Rate limiting (`AspNetCoreRateLimit`) |
| Baja | Activar auditoría (`AuditLog`) |
| Baja | Unificar namespaces `SMG` vs `SGM` |

### Frontend

| Prioridad | Mejora |
|---|---|
| Alta | Historial de pagos (página con filtros de fecha, puesto y estado) |
| Alta | Gestión de conceptos (página CRUD) |
| Alta | Vista del Dueño (página `/mi-cuenta` con sus puestos y deudas) |
| Alta | Editar puestos desde UI (modal) |
| Alta | Editar y desactivar usuarios desde UI (modales) |
| Alta | Modal de confirmación para liberar puesto |
| Alta | Pestaña Consultar en Deudas (filtros por estado y período) |
| Alta | Paginación en tablas largas |
| Media | Botón de descarga de comprobante PDF |
| Media | Página de perfil (cambio de contraseña propio) |
| Baja | Modo oscuro |
| Baja | Tests unitarios para `AuthContext`, hooks y llamadas a la API |

---

## 6. Guía de arranque

```bash
# 1. Base de datos
docker start sgm-postgres

# 2. Backend (en background)
dotnet run --project SMG.API/SGM.API.csproj &

# 3. Frontend
cd frontend && npm run dev
```

Credenciales de acceso (todos hashean a `admin123`):
- **Admin:** `admin@mercado.com`
- **Cajero:** `cajero@mercado.com`
- **Dueño:** `maria.garcia@email.com`

---

## 7. Versión 1.1 — Mejoras implementadas

Esta versión añade funcionalidades sobre la base estable de v1.0. Las secciones anteriores (1–6) documentan el estado original del sistema.

### 7.1 Backend — nuevos endpoints y comportamientos

#### CRUD completo de Conceptos de Cobro

**Qué se hizo:** Se completaron `POST /api/conceptos`, `PATCH /api/conceptos/{id}` y `DELETE /api/conceptos/{id}`.

**Por qué:** En v1.0 solo existía el `GET`. Los administradores no podían crear ni actualizar conceptos sin acceso directo a la base de datos.

**Cómo:** Se rellenaron `CreateConceptoDto` y se creó `UpdateConceptoDto`. Se implementó `UpdateAsync` en `IConceptoCobroRepository` y `ConceptoCobroRepository`. La interfaz `IConceptoCobroService` ya declaraba `UpdateAsync`; se implementó en `ConceptoCobroService`. El `DELETE` es un soft-delete: llama a `UpdateAsync` poniendo `Activo = false`, sin borrar el registro para preservar el historial de deudas asociadas.

---

#### PATCH /api/usuarios/{id} — editar usuario

**Qué se hizo:** Nuevo endpoint que permite editar nombre, rol, estado activo y contraseña de un usuario.

**Por qué:** En v1.0 solo se podía crear usuarios. Desactivar o cambiar la contraseña requería acceso directo a la BD.

**Cómo:** Se rellenó `UpdateUsuarioDto` con los campos opcionales. El controlador aplica solo los campos que lleguen no-nulos. El re-hasheo de contraseña se hace en el controlador con `BCrypt.HashPassword` (no en el servicio, para no interferir con el hash vacío del DataSeeder).

---

#### GET /api/pagos con filtros

**Qué se hizo:** Nuevo endpoint `GET /api/pagos` que acepta `?fechaInicio=`, `?fechaFin=`, `?puestoId=` y `?estado=` como query params opcionales combinables.

**Por qué:** No existía ninguna forma de listar pagos generales; solo se podía consultar el comprobante por ID o ver el resumen del día en el reporte de caja diaria.

**Cómo:** Se añadió `GetFiltradosAsync(DateOnly desde, DateOnly hasta, Guid? puestoId, string? estado)` a `IPagoRepository` y su implementación en `PagoRepository` usando LINQ encadenado sobre `IQueryable<Pago>`.

---

#### GET /api/puestos/mis-puestos

**Qué se hizo:** Endpoint que devuelve solo los puestos donde `DuenoId` coincide con el usuario autenticado.

**Por qué:** El rol Dueño no tenía una vista propia. Con `GET /api/puestos` recibía todos los puestos del mercado.

**Cómo:** `IPuestoService` ya declaraba `GetByDuenoAsync`; se expuso como endpoint `[HttpGet("mis-puestos")]` en `PuestosController`, extrayendo el `NameIdentifier` del JWT.

---

#### PATCH /api/puestos/{id} — editar puesto

**Qué se hizo:** Nuevo endpoint que permite editar `Descripcion`, `Ubicacion` (sector) y `Estado` de un puesto.

**Por qué:** En v1.0 solo se podía crear puestos y asignar/liberar dueños. No había forma de corregir datos como el sector o poner un puesto en mantenimiento sin acceso directo a la BD.

**Cómo:** Se rellenó `UpdatePuestoDto`. Se añadió `UpdatePuestoAsync` a `IPuestoService` y `PuestoService`. El estado `"Ocupado"` no es editable por este endpoint (depende de la asignación de dueño); solo `"Disponible"` y `"Mantenimiento"` tienen efecto.

---

#### GET /api/deudas mejorado — filtros por estado y período

**Qué se hizo:** El endpoint `GET /api/deudas` ahora acepta `?estado=` y `?periodo=` además de `?puestoId=`. Los tres son opcionales pero debe enviarse al menos uno.

**Por qué:** En v1.0 `puestoId` era obligatorio y único filtro. No había forma de consultar todas las deudas vencidas del mercado o todas las de un período específico.

**Cómo:** Se añadió `GetFiltradosAsync(Guid? puestoId, string? estado, string? periodo)` al repositorio. Para `estado = "Vencida"` aplica `Pendiente + FechaVencimiento < hoy` porque `Vencida` no es un valor del enum de la BD sino un estado derivado calculado en cada consulta.

---

#### Notificaciones automáticas al registrar/anular pagos

**Qué se hizo:** Al completar `POST /api/pagos` o `PATCH /api/pagos/{id}/anular`, se crea automáticamente una `Notificacion` para el dueño del puesto afectado.

**Por qué:** En v1.0 las notificaciones no se generaban en ningún flujo del sistema. El dueño no tenía forma de enterarse de que se había registrado o anulado un pago de su puesto.

**Cómo:** Se inyectó `INotificacionRepository` en `PagosController`. Tras guardar el pago (o la anulación), se comprueba si `deuda.Puesto.DuenoId` tiene valor y se llama a `AddAsync` con el tipo, título y mensaje correspondiente. Si la notificación falla, no revierte el pago.

---

### 7.2 Frontend — nuevas páginas y mejoras

#### Página /historial-pagos

**Qué se hizo:** Nueva página accesible para Admin y Cajero con tabla completa de pagos históricos.

**Por qué:** No había ninguna vista para consultar pagos anteriores al día de hoy. Solo existía el reporte de caja diaria (un día a la vez) y el formulario de registro.

**Cómo:** Usa `pagosApi.getAll({ fechaInicio, fechaFin, puestoId, estado })`. Tiene filtros de fecha inicio/fin (por defecto desde el primer día del mes hasta hoy), puesto y estado. KPIs de resumen en la cabecera. Paginación de 25 registros/página. Incluye botón de anulación directa para Admin.

---

#### Página /conceptos

**Qué se hizo:** Nueva página de gestión de conceptos de cobro, solo Admin.

**Por qué:** Los conceptos solo podían verse desde la pantalla de carga de deudas. No había forma de crear, editar ni desactivar conceptos desde la UI.

**Cómo:** Tabla con badge de estado activo/inactivo. Tres modales: Nuevo (crear), Editar (nombre, descripción, monto, día de emisión) y Desactivar (con mensaje explicativo de que no se elimina el registro).

---

#### Página /mi-cuenta — vista del rol Dueño

**Qué se hizo:** Nueva página exclusiva para el rol Dueño. Muestra sus puestos con sus deudas en un acordeón expandible.

**Por qué:** El rol Dueño iniciaba sesión pero veía el mismo dashboard que el Cajero, sin información relevante sobre sus propios puestos.

**Cómo:** Carga puestos con `puestosApi.getMisPuestos()`. Al expandir un puesto llama a `deudasApi.getPorPuesto(id)` de forma lazy (solo carga cuando se abre). Muestra KPIs de total puestos, activos y saldo pendiente acumulado. El login ahora redirige al Dueño a `/mi-cuenta` en lugar de `/dashboard`.

---

#### Editar puestos

**Qué se hizo:** Botón "Editar" en cada fila de la tabla de puestos (solo Admin), con modal para editar descripción, sector y estado.

**Por qué:** Solo se podía crear puestos y asignar/liberar dueños. No había forma de corregir un dato como el sector desde la UI.

**Cómo:** `puestosApi.update(id, { descripcion, sector, estado })` llama a `PATCH /api/puestos/{id}`. El campo estado solo ofrece "Disponible" y "Mantenimiento" para evitar confusión con "Ocupado". La descripción aparece como subtexto bajo el número de puesto en la tabla.

---

#### Editar y toggle de usuarios

**Qué se hizo:** Botones "Editar" y "Desactivar/Activar" en la tabla de usuarios.

**Por qué:** En v1.0 la tabla de usuarios era solo de lectura. Cambiar la contraseña o desactivar a un usuario requería acceso a la BD.

**Cómo:** El modal Editar permite cambiar nombre, rol y contraseña (el campo de nueva contraseña es opcional; vacío = no cambiar). El toggle de activo tiene un modal de confirmación. Se protege que el admin autenticado no pueda desactivarse a sí mismo comparando `u.id !== currentUser?.usuarioId`.

---

#### Pestaña Consultar en Deudas

**Qué se hizo:** Tercera pestaña "Consultar" en la página de Deudas con filtros de puesto, estado y período.

**Por qué:** La página de deudas solo servía para crear deudas. No había forma de consultar el estado de las deudas del mercado por criterios.

**Cómo:** Usa `deudasApi.getFiltradas({ puestoId, estado, periodo })`. Requiere al menos un filtro para hacer la consulta. La tabla muestra puesto, dueño, concepto, período, vencimiento, monto y badge de estado.

---

#### Modal de confirmación — Liberar puesto

**Qué se hizo:** El botón "Liberar" en la tabla de puestos abre un modal de confirmación propio en lugar de `window.confirm()`.

**Por qué:** `window.confirm()` no es compatible con el diseño de la aplicación, bloquea el hilo principal y su apariencia varía por navegador/SO.

**Cómo:** Se agregó `modalLiberar` de tipo `Puesto | null` al estado. El modal muestra el número de puesto y el nombre del dueño para que el Admin confirme con contexto completo.

---

#### Paginación client-side

**Qué se hizo:** Nuevo hook `usePagination<T>` y componente `Pagination` aplicados a historial-pagos (25/pág), puestos (20/pág) y usuarios (20/pág).

**Por qué:** Las tablas cargaban todos los registros y los mostraban en una sola página. Con muchos puestos o pagos históricos la tabla se volvía inmanejable.

**Cómo:** `usePagination` recibe el array completo y el tamaño de página; devuelve `paged` (slice de la página actual), `page`, `setPage`, `totalPages`, `total` y `reset`. `Pagination` es un componente de navegación con flechas de primera/anterior/siguiente/última página y contador "X–Y de N". Al aplicar nuevos filtros se llama a `reset()` para volver a la página 1.

> **Limitación conocida:** La paginación es client-side; todos los registros se siguen descargando del servidor en una sola petición.

---

### 7.3 Sidebar — rutas actualizadas en v1.1

| Ruta | Roles que la ven | Cambio |
|---|---|---|
| `/mi-cuenta` | Dueño | Nueva en v1.1 |
| `/historial-pagos` | Admin, Cajero | Nueva en v1.1 |
| `/conceptos` | Admin | Nueva en v1.1 |
| `/dashboard` | Admin, Cajero | Sin cambio (Dueño ya no la ve) |
| `/puestos` | Admin, Cajero | Sin cambio (Dueño ya no la ve) |
| `/pagos` | Admin, Cajero | Sin cambio (Dueño ya no la ve) |
| `/deudas` | Admin | Sin cambio |
| `/reportes` | Admin, Cajero | Sin cambio |
| `/usuarios` | Admin | Sin cambio |
| `/notificaciones` | Admin, Cajero, Dueño | Sin cambio |

> En v1.0 el Dueño veía las mismas rutas que el Cajero. En v1.1 solo ve `/mi-cuenta` y `/notificaciones`.

---

### 7.4 Interceptor 401 y redirección por rol

**Qué se hizo:** Se agregaron dos comportamientos al `AuthContext` y `axios.ts`:
1. El interceptor de respuesta en `axios.ts` detecta 401 y redirige automáticamente a `/login`, limpiando el storage.
2. El login redirige al Dueño a `/mi-cuenta` en lugar de `/dashboard`.

**Por qué:** En v1.0, si el token expiraba durante la sesión, el usuario veía errores genéricos en la UI en lugar de ser redirigido a login. Además, el Dueño llegaba a un dashboard sin información relevante para él.

---

### 7.5 Pendientes tras v1.1

#### Backend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Alta | Generación de PDF del comprobante con QuestPDF | La biblioteca está instalada pero el endpoint devuelve JSON |
| Alta | Refresh tokens + blacklist de JWT | El token dura 24 h sin renovación; un token robado sigue válido hasta expirar |
| Media | Job periódico para notificaciones de deuda próxima a vencer | Solo se notifica al registrar/anular pagos; no hay alerta preventiva |
| Media | Migrar `CREATE CAST` a script SQL versionado | Los casts están aplicados manualmente; no están en control de versiones |
| Media | FluentValidation en DTOs | La validación actual es básica; errores de formato llegan hasta la BD |
| Baja | Rate limiting | Sin límite de peticiones, la API es vulnerable a abuso |
| Baja | Activar auditoría (`AuditLog`) | La entidad y el repositorio existen pero ningún controlador los usa |
| Baja | Paginación server-side | Los `GET` siguen devolviendo todos los registros |
| Baja | Unificar namespaces `SMG` vs `SGM` | Smell técnico heredado del setup inicial |

#### Frontend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Alta | Botón de descarga de comprobante PDF | Depende de que el backend implemente la generación PDF con QuestPDF |
| Media | Página de perfil (cambio de contraseña propio) | El usuario no puede cambiar su propia contraseña; solo un Admin desde `/usuarios` |
| Media | Paginación server-side | La paginación actual descarga todos los registros |
| Baja | Modo oscuro | No implementado |
| Baja | Tests unitarios | No hay cobertura de pruebas |
