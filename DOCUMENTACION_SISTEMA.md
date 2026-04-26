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

---

## 8. Versión 1.2 — Mejoras implementadas

Esta versión añade funcionalidades sobre la base estable de v1.1. Las secciones anteriores (1–7) documentan el estado al cierre de v1.1.

### 8.1 Backend — nuevos endpoints y comportamientos

#### PATCH /api/usuarios/me/password — cambio de contraseña propio

**Qué se hizo:** Nuevo endpoint que permite a cualquier usuario autenticado cambiar su propia contraseña sin necesidad de que un Admin intervenga.

**Por qué:** En v1.1 el único camino para cambiar una contraseña era que un Admin editara el usuario desde `/api/usuarios/{id}`. Ningún usuario podía gestionarse la contraseña de forma autónoma.

**Cómo:** El endpoint está en `UsuariosController` con `[Authorize]` (sin restricción de rol). Extrae el `NameIdentifier` del JWT para identificar al usuario, verifica la contraseña actual con `BCrypt.Verify` y guarda el nuevo hash con `BCrypt.HashPassword`. Si la contraseña actual no coincide devuelve 400.

**DTO:** `CambiarPasswordDto` con los campos `PasswordActual` (required) y `NuevaPassword` (required, mínimo 6 caracteres).

---

#### Rate Limiting en el endpoint de login

**Qué se hizo:** Se configuró un `FixedWindowRateLimiter` con la política `"login"` (10 peticiones por minuto por IP). Se aplica solo a `POST /api/auth/login` con `[EnableRateLimiting("login")]`.

**Por qué:** Sin rate limiting, el endpoint de login era vulnerable a ataques de fuerza bruta. Un atacante podía probar miles de contraseñas sin ser bloqueado.

**Cómo:** Se usa el rate limiter nativo de ASP.NET Core 10 (`Microsoft.AspNetCore.RateLimiting`). Las peticiones que excedan el límite reciben HTTP 429. No se aplica globalmente para evitar impacto en endpoints de uso frecuente del frontend.

---

### 8.2 Scripts SQL versionados

#### sgm_casts.sql — casts de asignación para enums

**Qué se hizo:** Se creó el archivo `sgm_casts.sql` en la raíz del repositorio con los 10 `CREATE CAST` (uno por combinación tipo/variante varchar|text para cada uno de los 5 enums del schema `sgm`).

**Por qué:** En v1.1 los casts estaban documentados en la sección 4.5 pero no existía un archivo ejecutable versionado. Al levantar un entorno nuevo (local, CI, staging) había que copiarlos manualmente desde la documentación. Ahora alcanza con ejecutar `sgm_casts.sql` tras el script principal.

**Cómo usarlo:**
```bash
# Después de sgm_postgresql17_local.sql, ejecutar:
psql -h localhost -p 5433 -U postgres -d sgm_db -f sgm_casts.sql
```

---

### 8.3 Frontend — nuevas páginas y mejoras

#### Página /perfil — cambio de contraseña propio

**Qué se hizo:** Nueva página accesible para todos los roles (Admin, Cajero, Dueño) con información de la cuenta y formulario para cambiar la contraseña.

**Por qué:** En v1.1 no había forma de que un usuario gestionara su propia contraseña desde la UI. Solo un Admin podía hacerlo desde la tabla de usuarios.

**Cómo:** La página tiene dos cards: una muestra nombre, email y rol (solo lectura); la otra tiene el formulario con los campos "contraseña actual", "nueva contraseña" y "confirmar nueva contraseña". La validación de que ambas contraseñas coincidan se hace en cliente antes de llamar al API. Llama a `PATCH /api/usuarios/me/password` vía `perfilApi.cambiarPassword()`.

---

### 8.4 Sidebar — rutas actualizadas en v1.2

| Ruta | Roles que la ven | Cambio |
|---|---|---|
| `/perfil` | Admin, Cajero, Dueño | Nueva en v1.2 |

> Todas las demás rutas permanecen igual que en v1.1 (ver sección 7.3).

---

### 8.5 Guía de arranque actualizada

Se agregaron dos archivos SQL al repositorio que simplifican el setup de un entorno nuevo:

| Archivo | Cuándo ejecutar | Qué hace |
|---|---|---|
| `sgm_postgresql17_local.sql` | Al crear la DB por primera vez | Crea schema, enums, tablas, índices, datos seed y grants |
| `sgm_casts.sql` | Después del script principal, una sola vez por entorno | Crea los 10 `CREATE CAST` que permiten a EF Core escribir enums |
| `sgm_patch_passwords.sql` | Solo en bases existentes de v1.0 sin columna `password_hash` | Agrega la columna `password_hash` a `sgm.usuarios` |

```bash
# Setup completo de un entorno nuevo
docker run -d --name sgm-postgres -e POSTGRES_PASSWORD=postgres -p 5433:5432 postgres:17
psql -h localhost -p 5433 -U postgres -c "CREATE DATABASE sgm_db;"
psql -h localhost -p 5433 -U postgres -d sgm_db -f sgm_postgresql17_local.sql
psql -h localhost -p 5433 -U postgres -d sgm_db -f sgm_casts.sql

# Backend + Frontend (igual que en sección 6)
dotnet run --project SMG.API/SGM.API.csproj &
cd frontend && npm run dev
```

---

### 8.6 Pendientes tras v1.2

#### Backend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Alta | Generación de PDF del comprobante con QuestPDF | La biblioteca está instalada pero el endpoint devuelve JSON |
| Alta | Refresh tokens + blacklist de JWT | El token dura 24 h sin renovación; un token robado sigue válido hasta expirar |
| Media | Job periódico para notificaciones de deuda próxima a vencer | Solo se notifica al registrar/anular pagos; no hay alerta preventiva |
| Media | FluentValidation en DTOs | La validación actual es básica; errores de formato llegan hasta la BD |
| Baja | Activar auditoría (`AuditLog`) | La entidad y el repositorio existen pero ningún controlador los usa |
| Baja | Paginación server-side | Los `GET` siguen devolviendo todos los registros |
| Baja | Unificar namespaces `SMG` vs `SGM` | Smell técnico heredado del setup inicial |

#### Frontend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Alta | Botón de descarga de comprobante PDF | Depende de que el backend implemente la generación PDF con QuestPDF |
| Media | Paginación server-side | La paginación actual descarga todos los registros |
| Baja | Modo oscuro | No implementado |
| Baja | Tests unitarios | No hay cobertura de pruebas |

---

## 9. Versión 1.3 — Mejoras implementadas

Esta versión activa auditoría, añade validación declarativa con FluentValidation, e incorpora alertas preventivas automáticas por deudas próximas a vencer. Las secciones anteriores (1–8) documentan el estado al cierre de v1.2.

### 9.1 Backend — nuevas capacidades

#### Auditoría activada (AuditLog)

**Qué se hizo:** Se completó el pipeline de auditoría que ya existía en v1.0 como stubs vacíos:
- `AuditLogConfiguration` — mapeo EF Core de la tabla `sgm.audit_logs` con sus columnas `jsonb` e `inet`.
- `AuditLogRepository` — implementación de `IAuditLogRepository` con métodos de consulta por rango de fechas, usuario y acción.
- `AuditMiddleware` — middleware HTTP que captura cada `POST`, `PATCH`, `DELETE` o `PUT` con respuesta exitosa (2xx) y guarda un registro en `audit_logs` con: acción (`METHOD /ruta`), tabla afectada (segundo segmento del path), `usuario_id`, nombre de usuario, IP y user-agent.

**Por qué:** La entidad `AuditLog`, su repositorio (stub), la interfaz y el `DbSet` existían desde v1.0 pero no había nada que los usara. Sin auditoría, no hay trazabilidad de quién registró, anuló o modificó datos críticos.

**Cómo:** `AuditMiddleware` se registra en el pipeline después de `UseAuthorization()`, por lo que tiene acceso al `HttpContext.User` ya autenticado. El repositorio se inyecta como parámetro del método `InvokeAsync` (no del constructor) para respetar el ciclo de vida scoped. Si el guardado falla, la excepción se captura silenciosamente para no impactar la respuesta.

---

#### FluentValidation en DTOs

**Qué se hizo:** Se instaló `FluentValidation.AspNetCore` v11.3.1 y se crearon cuatro validadores en `SMG.API/Validators/`:

| Validador | DTO | Reglas clave |
|---|---|---|
| `LoginRequestValidator` | `LoginRequestDto` | Email válido, contraseña no vacía |
| `RegistrarPagoValidator` | `RegistrarPagoDto` | Monto > 0, método en lista permitida |
| `CargaIndividualDeudaValidator` | `CargaIndividualDeudaDto` | Monto > 0, fecha parseable, periodo no vacío |
| `CreateUsuarioValidator` | `CreateUsuarioDto` | Email válido, contraseña mínimo 6 caracteres, rol en lista permitida |

**Por qué:** La validación anterior era básica (nulidades en el controlador). Errores de formato — como un email malformado, un monto negativo o un método de pago inexistente — llegaban hasta la capa de base de datos y producían errores 500 poco descriptivos.

**Cómo:** `AddFluentValidationAutoValidation()` + `AddValidatorsFromAssemblyContaining<LoginRequestValidator>()` en `Program.cs`. Al fallar una validación, `[ApiController]` devuelve automáticamente un 400 con los mensajes del validador, sin necesidad de cambiar los controladores.

---

#### Job periódico — alertas de deuda próxima a vencer

**Qué se hizo:** Se creó `DeudaAlertaService` (`BackgroundService`) en `SGM.Infrastructure/Jobs/`. Se ejecuta una vez al arrancar y luego cada día a medianoche UTC.

**Por qué:** En v1.1 las notificaciones automáticas solo se generaban al registrar o anular un pago. No había alerta preventiva: el dueño se enteraba de la deuda cuando ya le rebotaba el pago o cuando la revisaba manualmente.

**Cómo:** Busca en la BD todas las `Deudas` con `Estado = Pendiente`, `FechaVencimiento = hoy + 3 días` y `DuenoId != null`. Por cada una crea una `Notificacion` de tipo `"deuda_proxima_vencer"` dirigida al dueño del puesto. Usa `IServiceScopeFactory` para crear un scope por ejecución (los `BackgroundService` son singleton y no pueden tener `AppDbContext` inyectado directamente). Los errores de una ejecución se loguean con `ILogger` sin afectar las siguientes.

---

### 9.2 Pendientes tras v1.3

#### Backend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Alta | Refresh tokens + blacklist de JWT | El token dura 24 h sin renovación; un token robado sigue válido hasta expirar |
| Media | Paginación server-side | Los `GET` siguen devolviendo todos los registros |
| Baja | Unificar namespaces `SMG` vs `SGM` | Smell técnico heredado del setup inicial |

#### Frontend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Media | Paginación server-side | La paginación actual descarga todos los registros |
| Baja | Modo oscuro | No implementado |
| Baja | Tests unitarios | No hay cobertura de pruebas |

---

## 10. Versión 1.4 — Mejoras implementadas

Esta versión resuelve los dos pendientes de mayor prioridad de v1.3: logout efectivo mediante blacklist de tokens y paginación server-side del historial de pagos.

### 10.1 Backend — JWT Blacklist

**Qué se hizo:** El logout ahora invalida el token inmediatamente. Se crearon:
- `ITokenBlacklist` (interfaz en `SMG.Core/Interfaces/Services/`)
- `TokenBlacklist` (implementación en `SGM.Infrastructure/Services/`) — singleton en memoria con `ConcurrentDictionary<string, DateTime>` que mapea cada JTI revocado a su fecha de expiración. Las entradas se limpian automáticamente al consultarlas si ya expiraron.
- Hook `OnTokenValidated` en `JwtBearerEvents` dentro de `Program.cs` — rechaza con 401 cualquier token cuyo JTI esté en el blacklist.
- `AuthService.LogoutAsync` actualizado — parsea el token con `JwtSecurityTokenHandler.ReadJwtToken`, extrae el `jti` y llama a `ITokenBlacklist.Revoke(jti, expiry)`.

**Por qué:** Antes el logout era solo de lado cliente. Un token robado o copiado seguía siendo válido hasta expirar (24 horas). Con el blacklist, al hacer logout el token queda invalidado inmediatamente en el servidor.

**Limitación conocida:** El blacklist es in-memory; al reiniciar el backend los JTIs revocados se pierden. Tokens robados antes de un reinicio recuperarían acceso por su vida restante. Para producción se requeriría persistencia (Redis o tabla SQL).

---

### 10.2 Backend — Paginación server-side para `GET /api/pagos`

**Qué se hizo:** `GET /api/pagos` ahora acepta `?page=` y `?pageSize=` (por defecto 1 y 25; máximo 100). La respuesta cambia de `Pago[]` a `PaginadoDto<PagoResponseDto>` con los campos `data`, `total`, `page`, `pageSize` y `totalPages`.

**Por qué:** El historial de pagos crece de forma ilimitada. Antes se descargaban todos los registros en una sola petición. Con paginación server-side el backend solo envía la página solicitada.

**Cómo:**
- `PaginadoDto<T>` — DTO genérico nuevo en `SMG.API/DTOs/Response/`.
- `IPagoRepository.GetFiltradosPaginadoAsync` — nuevo método que devuelve `(IEnumerable<Pago> Data, int Total)` usando `Skip`/`Take` sobre la misma query de filtros.
- `BuildFiltroQuery` — método privado compartido entre el método existente y el nuevo paginado.
- `PagosController.GetAll` retorna `PaginadoDto<PagoResponseDto>` en lugar de `IEnumerable<PagoResponseDto>`.

---

### 10.3 Frontend — Historial de pagos con paginación server-side

**Qué se hizo:** La página `/historial-pagos` eliminó `usePagination` y pasa directamente `?page=&pageSize=25` al API. El estado de paginación se gestiona localmente pero el servidor devuelve solo la página solicitada.

**Por qué:** Con la paginación client-side anterior, al aplicar filtros se descargaban TODOS los pagos del período. Con rangos amplios (varios meses) esto generaba payloads innecesariamente grandes.

**Cómo:** `pagosApi.getAll` en `api.ts` ahora devuelve el shape paginado completo. El componente llama a `cargarPagos(page)` al montar y al cambiar de página; al cambiar filtros resetea a `page=1`.

---

### 10.4 Pendientes tras v1.4

#### Backend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Alta | Refresh tokens | El token sigue siendo de 24 h; el usuario debe re-loguear al expirar |
| Alta | Persistir el JWT blacklist (Redis o tabla SQL) | Con reinicio del backend, tokens revocados recuperan acceso por su vida restante |
| Media | Paginación server-side para `/api/puestos` y `/api/usuarios` | Client-side actual; menos crítico por volumen esperado |
| Baja | Unificar namespaces `SMG` vs `SGM` | Smell técnico heredado del setup inicial |

#### Frontend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Media | Paginación server-side para puestos y usuarios | Baja prioridad dado el volumen esperado |
| Baja | Modo oscuro | No implementado |
| Baja | Tests unitarios | No hay cobertura de pruebas |

---

## 11. Versión 1.5 — Mejoras implementadas

Esta versión implementa refresh tokens completos (rotación, revocación, TTL configurable), reducción del token de acceso a 2 horas y renovación automática transparente en el frontend.

### 11.1 Backend — Refresh Tokens

**Qué se hizo:**
- Nueva entidad `RefreshToken` (`SMG.Core/Entities/`) con campos `id`, `usuario_id`, `token`, `expires_at`, `revocado`, `created_at`.
- `RefreshTokenConfiguration` — mapeo EF Core a la tabla `sgm.refresh_tokens`.
- `IRefreshTokenRepository` + `RefreshTokenRepository` con métodos: `GetByTokenAsync`, `AddAsync`, `RevokeByUsuarioIdAsync`, `EliminarExpiradosAsync`.
- `AppDbContext` — nuevo `DbSet<RefreshToken>`.
- `AuthService` completamente reescrito:
  - `LoginAsync` genera un access token (JWT 2 h) **y** un refresh token (token aleatorio de 64 bytes, base64url, TTL 7 días), guarda el refresh token en DB.
  - `RefreshAsync(refreshToken)` — valida el refresh token en DB, revoca el viejo (rotación), emite nuevos access + refresh token.
  - `LogoutAsync(accessToken, refreshToken)` — blacklistea el JTI del access token **y** revoca todos los refresh tokens del usuario en DB.
- `AuthController` — nuevo endpoint `POST /api/auth/refresh`; `POST /api/auth/logout` ahora recibe un body `{ refreshToken }` además del header `Authorization`.
- `LoginResponseDto` + `LoginResult` — incluyen `refreshToken` y `refreshTokenExpiresAt`.
- `appsettings.json` — `ExpirationHours` reducido de 24 a **2**; nuevo campo `RefreshTokenDays: 7`.

**Por qué:** Con tokens de 24 h sin renovación, el usuario debía re-loguear cada día. Con refresh tokens de 7 días el cliente obtiene un nuevo access token de forma transparente al expirar los 2 h, sin interrumpir la sesión.

**Rotación de refresh tokens:** cada uso del refresh token emite un refresh token nuevo y revoca el anterior, evitando reutilización.

---

### 11.2 Frontend — Auto-refresh transparente

**Qué se hizo:**
- `AuthUser` en `types.ts` — ahora incluye `refreshToken` y `refreshTokenExpiresAt`.
- `auth.ts` — `storeUser` persiste `refreshToken` en `localStorage`; `clearUser` lo elimina; nueva función `getStoredRefreshToken`.
- `api.ts` — `authApi` expone `refresh(refreshToken)` y `logout(refreshToken?)`.
- `axios.ts` — interceptor de respuesta actualizado: al recibir 401, si existe un `refreshToken` en `localStorage`, llama automáticamente a `POST /api/auth/refresh`, actualiza el storage y reintenta la request original. Las requests que lleguen durante el refresh se encolan y se procesan con el nuevo token. Solo redirige a `/login` si no hay refresh token o si el refresh falla.
- `AuthContext` — `logout` ahora llama a `authApi.logout(refreshToken)` antes de limpiar el storage, de forma best-effort (si falla, el logout local continúa igual).

---

### 11.3 Script SQL de patch

`sgm_v1.5_patch.sql` — crea la tabla `sgm.refresh_tokens` con sus índices. Ejecutar en bases de datos existentes (v1.0–v1.4). Para entornos nuevos, incluirlo después de `sgm_postgresql17_local.sql`.

```bash
psql -h localhost -p 5433 -U postgres -d sgm_db -f sgm_v1.5_patch.sql
```

---

### 11.4 Pendientes tras v1.5

#### Backend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Media | Persistir JWT blacklist en DB | Con reinicio del backend, tokens revocados (en el intervalo de 2 h) recuperan acceso |
| Baja | Paginación server-side para `/api/puestos` y `/api/usuarios` | Volumen pequeño; client-side actual es suficiente |
| Baja | Unificar namespaces `SMG` vs `SGM` | Smell técnico heredado del setup inicial |

#### Frontend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Baja | Paginación server-side para puestos y usuarios | Volumen pequeño; client-side actual es suficiente |
| Baja | Modo oscuro | No implementado |
| Baja | Tests unitarios | No hay cobertura de pruebas |

---

## 12. Versión 1.6 — JWT Blacklist Persistente + Paginación Server-Side (Puestos y Usuarios)

### Resumen de cambios

| Área | Cambio |
|---|---|
| Backend | JWT blacklist persistida en `sgm.tokens_revocados` con carga en startup |
| Backend | `GET /api/puestos` — paginación server-side con filtro por búsqueda |
| Backend | `GET /api/usuarios` — paginación server-side con filtro por búsqueda y rol |
| Frontend | Páginas Puestos y Usuarios refactorizadas a paginación server-side |

---

### 12.1 Persistencia del JWT Blacklist

#### Motivación

En v1.5, el `ITokenBlacklist` era un `ConcurrentDictionary<string, DateTime>` en memoria pura. Al reiniciar el proceso, todos los JTIs revocados se perdían. Durante la ventana de expiración del access token (2 horas), un token que fue revocado vía logout recuperaba acceso automáticamente tras un restart.

#### Entidad y tabla

**`SMG.Core/Entities/TokenRevocado.cs`**
```csharp
public class TokenRevocado
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public string   Jti       { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
}
```

Tabla PostgreSQL: `sgm.tokens_revocados`  
DDL: **`sgm_v1.6_patch.sql`**  
Índice único en `jti`; índice adicional en `expires_at` para limpieza eficiente.

#### Repositorio

**`SMG.Core/Interfaces/Repositories/ITokenRevocadoRepository.cs`** — namespace `SMG.Core.Repositories`

```csharp
public interface ITokenRevocadoRepository
{
    Task AddAsync(TokenRevocado token);
    Task<IEnumerable<TokenRevocado>> GetActivosAsync();
    Task EliminarExpiradosAsync();
}
```

**`SGM.Infrastructure/Repositories/TokenRevocadoRepository.cs`** — `AddAsync` verifica existencia para evitar errores de clave duplicada en doble-logout; `GetActivosAsync` filtra por `ExpiresAt > UtcNow`; `EliminarExpiradosAsync` usa `ExecuteDeleteAsync` para DELETE bulk sin materializar entidades.

#### Interfaz ITokenBlacklist actualizada

`void Revoke` → `Task RevokeAsync` (permite await de la escritura a DB)  
Se agrega `Task LoadFromDatabaseAsync()` para rehidratación en startup.

```csharp
public interface ITokenBlacklist
{
    Task RevokeAsync(string jti, DateTime expiry);
    bool IsRevoked(string jti);        // Síncrono — usado en OnTokenValidated
    Task LoadFromDatabaseAsync();
}
```

#### TokenBlacklist — implementación híbrida

`SGM.Infrastructure/Services/TokenBlacklist.cs` — singleton que recibe `IServiceScopeFactory` para crear scopes DB cuando es necesario:

- **`IsRevoked`**: lectura pura del `ConcurrentDictionary` — O(1), sin IO, compatible con el hook síncrono de JWT Bearer.
- **`RevokeAsync`**: escribe en el dict en memoria Y persiste en DB vía scope efímero.
- **`LoadFromDatabaseAsync`**: al arrancar, carga todos los `TokenRevocado` activos en el dict en memoria, restaurando el estado post-restart.

#### Startup (Program.cs)

```csharp
builder.Services.AddScoped<ITokenRevocadoRepository, TokenRevocadoRepository>();

// Después del seeder:
var tokenBlacklist = app.Services.GetRequiredService<ITokenBlacklist>();
await tokenBlacklist.LoadFromDatabaseAsync();
```

#### AuthService

`LogoutAsync` actualizado: `_blacklist.Revoke(...)` → `await _blacklist.RevokeAsync(...)`.

---

### 12.2 Paginación Server-Side — Puestos

#### Repositorio

`IPuestoRepository` — nuevo método:
```csharp
Task<(IEnumerable<Puesto> Data, int Total)> GetPaginadoAsync(string? search, int page, int pageSize);
```

`PuestoRepository.GetPaginadoAsync` — construye `IQueryable<Puesto>` con `Include(p => p.Dueno)`, aplica filtro `Contains` en `Codigo`, `Ubicacion` y `Dueno.NombreCompleto`, materializa `CountAsync` y luego `Skip/Take`.

#### Controller

`GET /api/puestos` — nuevos query params:

| Param | Default | Restricción |
|---|---|---|
| `search` | `null` | — |
| `page` | `1` | ≥ 1 |
| `pageSize` | `25` | 1–100 |

Retorna `PaginadoDto<PuestoResponseDto>` en lugar de `IEnumerable<PuestoResponseDto>`.

El constructor ahora recibe `IPuestoService` (para mutaciones) + `IPuestoRepository` (para la query paginada).

#### Frontend

`puestosApi.getAll` — nueva firma:
```typescript
getAll(params?: { search?: string; page?: number; pageSize?: number })
  => Promise<{ data: Puesto[]; total: number; page: number; pageSize: number; totalPages: number }>
```

`puestos/page.tsx` refactorizado:
- Eliminado `usePagination` y filtrado client-side.
- Estado: `page`, `total`, `totalPages`; `cargarPuestos(page, search)` via `useCallback`.
- `useEffect` sobre `[search]` → resetea a página 1 y recarga.
- `useEffect` sobre `[page]` → recarga con nueva página.
- Dueños para el modal de asignación se cargan aparte: `usuariosApi.getAll({ rol: 'Dueno', pageSize: 500 })`.
- Tras cada mutación (crear/editar/asignar/liberar) → `await cargarPuestos(page, search)` para reflejar cambios.

---

### 12.3 Paginación Server-Side — Usuarios

#### Repositorio

`IUsuarioRepository` — nuevo método:
```csharp
Task<(IEnumerable<Usuario> Data, int Total)> GetPaginadoAsync(
    string? search, string? rol, int page, int pageSize);
```

Filtro `search` aplica `Contains` sobre `NombreCompleto` y `Email`.  
Filtro `rol` hace `Enum.TryParse<RolUsuario>` y aplica `Where(u => u.Rol == rolEnum)`.

#### Controller

`GET /api/usuarios` — nuevos query params:

| Param | Default | Restricción |
|---|---|---|
| `search` | `null` | — |
| `rol` | `null` | `Admin`, `Cajero`, `Dueno` |
| `page` | `1` | ≥ 1 |
| `pageSize` | `25` | 1–200 |

`pageSize` máx 200 (vs 100 en puestos) para que la página de puestos pueda pedir todos los dueños en una sola llamada con `pageSize=500` (se satura al tope 200, pero cubre el volumen esperado).

#### Frontend

`usuariosApi.getAll` — nueva firma:
```typescript
getAll(params?: { search?: string; rol?: string; page?: number; pageSize?: number })
  => Promise<{ data: Usuario[]; total: number; page: number; pageSize: number; totalPages: number }>
```

`usuarios/page.tsx` refactorizado con el mismo patrón que puestos: `cargarUsuarios(page, search)`, doble `useEffect` search/page, sin `usePagination`, recarga tras cada mutación.

---

### 12.4 Archivos modificados

#### Nuevos
- `SMG.Core/Entities/TokenRevocado.cs`
- `SMG.Core/Interfaces/Repositories/ITokenRevocadoRepository.cs`
- `SGM.Infrastructure/Data/Configuations/TokenRevocadoConfiguration.cs`
- `SGM.Infrastructure/Repositories/TokenRevocadoRepository.cs`
- `sgm_v1.6_patch.sql`

#### Modificados
| Archivo | Cambio |
|---|---|
| `SMG.Core/Interfaces/Services/ITokenBlacklist.cs` | `void Revoke` → `Task RevokeAsync` + `LoadFromDatabaseAsync` |
| `SGM.Infrastructure/Services/TokenBlacklist.cs` | Inyecta `IServiceScopeFactory`; implementación híbrida memoria+DB |
| `SGM.Infrastructure/Data/AppDbContext.cs` | `DbSet<TokenRevocado> TokensRevocados` |
| `SGM.Infrastructure/Services/AuthService.cs` | `await _blacklist.RevokeAsync(...)` |
| `SMG.Core/Interfaces/Repositories/IPuestoRepository.cs` | `GetPaginadoAsync` |
| `SGM.Infrastructure/Repositories/PuestoRepository.cs` | Implementa `GetPaginadoAsync` |
| `SMG.API/Controllers/PuestosController.cs` | Inyecta `IPuestoRepository`; `GetAll` retorna `PaginadoDto` |
| `SMG.Core/Interfaces/Repositories/IUsuarioRepository.cs` | `GetPaginadoAsync` |
| `SGM.Infrastructure/Repositories/UsuarioRepository.cs` | Implementa `GetPaginadoAsync` |
| `SMG.API/Controllers/UsuariosController.cs` | Inyecta `IUsuarioRepository`; `GetAll` retorna `PaginadoDto` |
| `SMG.API/Program.cs` | Registra `ITokenRevocadoRepository`; carga blacklist en startup |
| `frontend/lib/api.ts` | `puestosApi.getAll` y `usuariosApi.getAll` con forma paginada |
| `frontend/app/(dashboard)/puestos/page.tsx` | Paginación server-side; dueños por separado |
| `frontend/app/(dashboard)/usuarios/page.tsx` | Paginación server-side |

---

### 12.5 Pendientes técnicos

#### Backend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Media | `EliminarExpiradosAsync` en background job | Limpiar `tokens_revocados` periódicamente para evitar crecimiento indefinido |
| Baja | Unificar namespaces `SMG` vs `SGM` | Smell técnico heredado del setup inicial |

#### Frontend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Baja | Debounce en campo de búsqueda | Evitar request por cada keystroke |
| Baja | Modo oscuro | No implementado |
| Baja | Tests unitarios | No hay cobertura de pruebas |

---

## 13. Versión 1.7 — Correcciones y limpieza de tokens

### 13.1 Backend — correcciones

#### Startup resiliente ante DB no migrada

**Qué se hizo:** El bloque `await tokenBlacklist.LoadFromDatabaseAsync()` en `Program.cs` se envolvió en `try/catch`. Si la tabla `sgm.tokens_revocados` no existe (patch v1.6 no aplicado) o hay cualquier error de conexión, el backend loguea un aviso y arranca igual en lugar de crashear.

**Por qué:** Sin el try-catch, cualquier excepción durante la carga del blacklist terminaba el proceso en startup, haciendo que todas las requests devolvieran 504. El aviso en el log indica al operador que debe aplicar `sgm_v1.6_patch.sql`.

**Cómo:** Captura `Exception` genérico en startup, logea con `LogWarning` (no `LogError` porque el sistema funciona con degradación aceptable) y continúa el pipeline.

---

#### Limpieza periódica de `tokens_revocados` expirados

**Qué se hizo:** `DeudaAlertaService` incorpora un segundo método `LimpiarTokensRevocadosExpiradosAsync` que llama a `ITokenRevocadoRepository.EliminarExpiradosAsync`. Ambas operaciones — alertas de deuda y limpieza de tokens — se ejecutan en paralelo conceptual dentro del método `RunDailyTasksAsync`, una vez al arrancar y luego cada medianoche UTC.

**Por qué:** En v1.6 se creó la tabla `tokens_revocados` con un índice por `expires_at` preparado para limpieza eficiente, pero nadie la llamaba. Sin purga, la tabla crece sin límite a razón de un registro por logout durante la ventana de 2 horas de vida del access token.

**Cómo:** `ITokenRevocadoRepository.EliminarExpiradosAsync` ya existía y usa `ExecuteDeleteAsync` (bulk DELETE sin materializar entidades). El job reutiliza el patrón de scope ya establecido en `NotificarDeudasProximasAsync`. Los errores se capturan silenciosamente para no bloquear el ciclo diario.

---

### 13.2 Frontend — corrección

#### Bug en selector de puestos — Historial de Pagos

**Qué se hizo:** Se corrigió la llamada `puestosApi.getAll().then(setPuestos)` en `historial-pagos/page.tsx` → `puestosApi.getAll({ pageSize: 500 }).then((r) => setPuestos(r.data))`.

**Por qué:** `puestosApi.getAll()` devuelve `PaginadoDto<Puesto>` (`{ data, total, page, … }`) desde v1.6. La llamada anterior asignaba el objeto paginado completo al estado `puestos: Puesto[]`, haciendo que `puestos.map(...)` en el dropdown lanzara un error en runtime y el selector quedara vacío.

---

### 13.3 Archivos modificados

| Archivo | Cambio |
|---|---|
| `SMG.API/Program.cs` | `LoadFromDatabaseAsync` en try-catch con LogWarning |
| `SGM.Infrastructure/Jobs/DeudaAlertaService.cs` | `RunDailyTasksAsync` + `LimpiarTokensRevocadosExpiradosAsync` |
| `frontend/app/(dashboard)/historial-pagos/page.tsx` | `puestosApi.getAll({ pageSize: 500 }).then(r => setPuestos(r.data))` |

---

### 13.4 Pendientes técnicos

#### Backend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Baja | Unificar namespaces `SMG` vs `SGM` | Smell técnico heredado del setup inicial |

#### Frontend

| Prioridad | Mejora | Motivo |
|---|---|---|
| Baja | Debounce en campo de búsqueda (puestos y usuarios) | Evitar request por cada keystroke |
| Baja | Modo oscuro | No implementado |
| Baja | Tests unitarios | No hay cobertura de pruebas |

---

## 14. Versión 1.8 — Corrección crítica: lectura de enums PostgreSQL

### 14.1 Problema

El backend fallaba con `InvalidCastException: Reading as 'System.Int32' is not supported for fields having DataTypeName 'sgm.rol_usuario'` en todos los endpoints que involucran tablas con columnas enum (`usuarios.rol`, `puestos.estado`, `pagos.estado`, `pagos.metodo_pago`, `deudas.estado`). Ningún login, listado de puestos, ni reporte de morosidad respondía — todos retornaban 500.

**Causa raíz:** Npgsql EF Core 10.0.1 no podía leer columnas de tipo PostgreSQL native enum en esquema no-public (`sgm.rol_usuario`). Su materializador llamaba `GetInt32()` (el tipo subyacente de C# enum), pero la columna era del tipo `sgm.rol_usuario` — incompatibles. `MapEnum` sin schema qualifier y `HasConversion<string>` fueron ignorados porque el plugin de Npgsql EF Core interceptaba el mapeo de tipos antes de aplicar el converter.

### 14.2 Solución aplicada: convertir columnas a TEXT + HasConversion

**Estrategia elegida:** convertir las 5 columnas enum en PostgreSQL a `text` y usar `HasConversion` en las configuraciones de EF Core. Esto desacopla completamente la representación en DB del tipo C#, sin depender del soporte de native enum types de Npgsql.

**Pasos en la base de datos:**

1. Dropped 4 views que dependían de las columnas enum (`vw_deudas_pendientes`, `vw_caja_diaria`, `vw_morosidad`, `vw_resumen_puesto`)
2. Dropped 3 partial indexes con predicados referenciando tipos enum (`idx_deudas_pendientes`, `idx_deudas_vencidas`, `idx_pagos_fecha_activo`)
3. Actualizadas las funciones trigger `fn_validar_pago` y `fn_actualizar_estado_deuda` para declarar variables como `TEXT` en lugar de `sgm.estado_deuda` / `sgm.estado_pago`
4. `ALTER TABLE ... ALTER COLUMN ... TYPE text USING estado::text` en las 5 columnas
5. Recreadas las views y los indexes usando comparaciones de texto plano (`WHERE d.estado = 'pendiente'` en lugar de `WHERE d.estado = 'pendiente'::sgm.estado_deuda`)

**Pasos en el código C#:**

Cada configuración EF Core recibió `HasConversion` que serializa el enum a string en minúsculas (snake_case donde aplica) y lo parsea de vuelta:

```csharp
// UsuarioConfiguration — RolUsuario → "admin" | "cajero" | "dueno"
builder.Property(u => u.Rol)
    .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<RolUsuario>(v, true));

// PuestoConfiguration — EstadoPuesto, caso especial EnMantenimiento → "en_mantenimiento"
builder.Property(p => p.Estado)
    .HasConversion(
        v => v == EstadoPuesto.EnMantenimiento ? "en_mantenimiento" : v.ToString().ToLower(),
        v => v == "en_mantenimiento" ? EstadoPuesto.EnMantenimiento : Enum.Parse<EstadoPuesto>(v, true));

// PagoConfiguration — EstadoPago y MetodoPago → toLower directo
// DeudaConfiguration — EstadoDeuda → toLower directo
```

`Program.cs`: eliminadas las llamadas a `dataSourceBuilder.MapEnum<T>()` y la variable `translator` (ya no son necesarias porque las columnas son TEXT).

### 14.3 Archivos modificados

| Archivo | Cambio |
|---|---|
| `SGM.Infrastructure/Data/Configuations/UsuarioConfiguration.cs` | `HasConversion` para `RolUsuario` |
| `SGM.Infrastructure/Data/Configuations/PuestoConfiguration.cs` | `HasConversion` para `EstadoPuesto` |
| `SGM.Infrastructure/Data/Configuations/PagoConfiguration.cs` | `HasConversion` para `EstadoPago` y `MetodoPago` |
| `SGM.Infrastructure/Data/Configuations/DeudaConfiguration.cs` | `HasConversion` para `EstadoDeuda` |
| `SMG.API/Program.cs` | Eliminadas llamadas `MapEnum` y `using Npgsql.NameTranslation` |
| Base de datos | 5 columnas enum → text; 4 views y 3 indexes recreados; 2 trigger functions actualizadas |

---

## 15. Versión 1.9 — Correcciones de arranque, UI y rediseño de login

### 15.1 Tabla `refresh_tokens` faltante en el script de base de datos

**Problema:** El endpoint `POST /api/auth/login` fallaba con `relation "sgm.refresh_tokens" does not exist` en entornos recién instalados, porque la tabla no estaba en `sgm_postgresql17_local.sql`.

**Solución:** Añadida la tabla como sección 2.2 del script principal, justo después de `sgm.usuarios` (de quien depende por FK). Las secciones siguientes fueron renumeradas (2.2 → 2.3 PUESTOS … 2.8 PAGOS).

```sql
CREATE TABLE IF NOT EXISTS sgm.refresh_tokens (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id  UUID         NOT NULL REFERENCES sgm.usuarios(id) ON DELETE CASCADE,
    token       VARCHAR(128) NOT NULL UNIQUE,
    expires_at  TIMESTAMPTZ  NOT NULL,
    revocado    BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
```

### 15.2 Corrección de bugs en frontend — selects paginados

**Problema:** `puestosApi.getAll()` retorna `{ data: Puesto[], total, page, … }` desde v1.6, pero dos páginas del dashboard lo trataban como `Puesto[]` directamente, lanzando `TypeError: ps.filter is not a function` en runtime y mostrando "Error cargando datos".

| Archivo | Línea | Fix |
|---|---|---|
| `frontend/app/(dashboard)/deudas/page.tsx` | 62 | `ps.filter(…)` → `ps.data.filter(…)` |
| `frontend/app/(dashboard)/pagos/page.tsx` | 42 | `ps.filter(…)` → `ps.data.filter(…)` |
| `frontend/app/(dashboard)/pagos/page.tsx` | 186 | `numeroComprobante` (`string \| undefined`) → `?? null` para coincidir con firma `string \| null` |

### 15.3 Componentes UI — texto negro en selects y Heroicons

**Problema:** Los `<select>` y `<Select>` del dashboard mostraban las opciones con texto invisible sobre fondos claros por falta de `text-gray-900`.

**Cambios en componentes compartidos:**

- `components/ui/Select.tsx` — añadido `text-gray-900`, `appearance-none` y `ChevronDownIcon` (Heroicons) como flecha decorativa.
- `components/ui/Input.tsx` — toggle de contraseña integrado en el propio componente usando `EyeIcon` / `EyeSlashIcon`; nueva prop `icon` para icono izquierdo opcional; `text-gray-900` explícito.

**Selects inline corregidos** (añadido `bg-white text-gray-900`):
`deudas/page.tsx`, `puestos/page.tsx`, `reportes/page.tsx`, `usuarios/page.tsx`, `historial-pagos/page.tsx`

**Dependencia instalada:** `@heroicons/react`

### 15.4 Rediseño de página de login

Layout dividido estilo split-screen inspirado en SeedProd, con tema de mercado peruano:

- **Panel izquierdo (42 %):** fondo blanco, logo con `ShoppingBagIcon`, campos con íconos (`EnvelopeIcon`, `LockClosedIcon`), botón naranja con `ArrowRightEndOnRectangleIcon`, foco naranja en todos los inputs.
- **Panel derecho (58 %):** imagen de fondo (`/public/mercado.jpg`) cargada por CSS `background-image` (evita el preload automático de Next.js), overlay degradado negro inferior, texto "¡Bienvenido al mercado de tu comunidad!".

### 15.5 Corrección de middleware — archivos estáticos bloqueados

**Problema:** El middleware de autenticación (`proxy.ts`) interceptaba peticiones a `/mercado.jpg` (y cualquier archivo estático en `/public`) redirigiendo a `/login`, impidiendo que la imagen del panel derecho cargara.

**Fix:** Actualizado el matcher para excluir extensiones de archivo estático:

```ts
matcher: ['/((?!_next/static|_next/image|favicon.ico|login|api|.*\\.(?:jpg|jpeg|png|webp|svg|gif|ico|mp4|woff2?|ttf)).*)']
```

### 15.6 Archivos modificados

| Archivo | Cambio |
|---|---|
| `sgm_postgresql17_local.sql` | Añadida tabla `sgm.refresh_tokens` (sección 2.2); renumeradas secciones 2.3–2.8 |
| `SGM.Infrastructure/Data/Configuations/DeudaConfiguration.cs` | `HasConversion` para `EstadoDeuda` |
| `frontend/app/(dashboard)/deudas/page.tsx` | `ps.data.filter` + `text-gray-900` en selects inline |
| `frontend/app/(dashboard)/pagos/page.tsx` | `ps.data.filter` + fix tipo `?? null` |
| `frontend/app/(dashboard)/puestos/page.tsx` | `text-gray-900` en selects inline |
| `frontend/app/(dashboard)/reportes/page.tsx` | `text-gray-900` en selects inline |
| `frontend/app/(dashboard)/usuarios/page.tsx` | `text-gray-900` en selects inline |
| `frontend/app/(dashboard)/historial-pagos/page.tsx` | `text-gray-900` en selects inline |
| `frontend/components/ui/Select.tsx` | `text-gray-900` + `ChevronDownIcon` + `appearance-none` |
| `frontend/components/ui/Input.tsx` | Toggle contraseña integrado + prop `icon` + `text-gray-900` |
| `frontend/app/(auth)/login/page.tsx` | Rediseño completo split-screen con Heroicons |
| `frontend/proxy.ts` | Matcher actualizado para excluir archivos estáticos |
| `frontend/package.json` | Nueva dependencia `@heroicons/react` |
