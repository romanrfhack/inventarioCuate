# 21. Scaffold técnico Paso 3B

## Objetivo

Materializar un scaffold real, navegable y listo para crecer por slices sobre ASP.NET Core Web API + Angular + SQL Server, respetando el baseline funcional ya definido.

## Lo implementado

### Backend
- solución `.NET` con proyectos `Api`, `Application`, `Domain` e `Infrastructure`
- `ApplicationDbContext` con entidades iniciales para:
  - productos
  - inventario actual
  - movimientos de inventario
  - usuarios
  - turnos preparados
  - cargas de inventario inicial
  - auditoría de reset demo
- autenticación mínima con JWT y usuarios demo sembrados
- endpoints iniciales:
  - `POST /api/auth/login`
  - `GET /api/catalog/products`
  - `GET /api/inventory/summary`
  - `GET /api/demo-admin/status`
  - `POST /api/demo-admin/seed`
  - `POST /api/demo-admin/reset`
  - `POST /api/initial-load/preview`
  - `POST /api/initial-load/apply/{loadId}`
- entorno `Demo` con `AllowReset=true`
- seed demo separado del reset
- reset protegido por:
  - rol `admin`
  - entorno `Demo`
  - confirmación exacta `RESET DEMO`
- auditoría mínima en `DemoResetAudits`

### Frontend
- app Angular standalone base
- rutas y páginas iniciales:
  - login
  - dashboard
  - catálogo
  - inventario
  - carga inicial
  - demo/admin
- interceptor JWT y guard simple de sesión
- navegación suficiente para demo técnica

### Operación demo
- `docker-compose.demo.yml` para SQL Server local
- `.env.demo.example` como baseline de variables
- usuarios demo previstos:
  - `admin.demo / Demo123!`
  - `operador.demo / Demo123!`

## Decisiones materializadas

- SQL Server quedó materializado como base objetivo del scaffold
- se dejó `Shift` preparado pero no obligatorio todavía
- la carga inicial quedó dividida en dos pasos técnicos:
  1. preview y generación de token de confirmación
  2. aplicación controlada con token
- el parser CSV real y la transacción de aplicación final no se forzaron en este paso para no mezclar scaffold con lógica operativa incompleta
- seed demo y reset demo son rutas separadas para evitar sobrecargar un solo flujo administrativo

## Migración inicial

No se dejó una migración inicial generada en este repo porque el entorno actual no tiene `dotnet SDK` instalado, así que no fue posible ejecutar `dotnet ef migrations add InitialCreate` de forma verificable aquí.

Sí quedó listo el `DbContext` para generar la migración en cuanto exista SDK en el entorno.

## Verificación ejecutada

### Hecho
- validación de estructura de archivos creada
- instalación de dependencias frontend
- build del frontend Angular

### No verificable en este entorno
- restore/build backend .NET
- generación y aplicación real de migración EF Core
- arranque efectivo de la API

Motivo: falta `dotnet` en el host actual.

## Cómo ejecutar

### Backend
1. instalar `.NET SDK 8`
2. levantar SQL Server demo:
   ```bash
   cd app
   docker compose -f docker-compose.demo.yml up -d
   ```
3. exportar variables desde `.env.demo.example`
4. generar migración inicial:
   ```bash
   dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Api --output-dir Persistence/Migrations
   ```
5. correr la API:
   ```bash
   dotnet run --project src/Api
   ```

### Frontend
```bash
cd app/frontend
npm install
npm start
```

## Riesgos y huecos conscientes

- el reset usa `DELETE` explícito y requiere que el esquema ya exista, conviene endurecerlo cuando entre el slice de administración
- la carga inicial todavía no parsea CSV real ni aplica inventario en transacción de negocio
- falta endurecer CORS, secretos y manejo de configuración para ambientes no demo
- aún no hay pruebas automáticas ni manejo de concurrencia de inventario

## Siguiente slice recomendado

Slice técnico recomendado: cerrar **carga inicial real**.

Orden sugerido:
1. parser CSV con validación de plantilla
2. preview persistido con resumen por fila
3. apply transaccional que cree productos faltantes, inventario base y movimientos `carga_inicial`
4. migración EF Core inicial y pruebas de integración del flujo
