# 13. Arquitectura técnica MVP

## Visión técnica general

Se propone un MVP web con backend API en ASP.NET Core y frontend Angular, sobre una base de datos relacional. La arquitectura debe ser pragmática, limpia e incremental, priorizando velocidad de entrega, trazabilidad operativa y capacidad de evolución sin sobreingeniería.

## Qué está confirmado

- el inventario inicial ya no proviene del Excel histórico
- el MVP debe soportar demo controlada, carga explícita de inventario inicial, movimientos y venta rápida
- el reset solo debe existir en entorno demo o sandbox

## Decisión técnica propuesta

- backend: ASP.NET Core Web API
- frontend: Angular
- base de datos: SQL Server o PostgreSQL, con preferencia inicial por SQL Server si el ecosistema del proyecto se alinea con .NET tradicional; PostgreSQL también es válida si se busca menor fricción de despliegue
- arquitectura por módulos/slices verticales con capas ligeras

## Provisional

- motor final exacto de base de datos
- mecanismo de autenticación inicial, simple o integrado
- nivel de separación exacta entre Application y Domain en el scaffold

## Componentes principales

1. Frontend Angular
2. Backend API ASP.NET Core
3. Base de datos relacional
4. Procesamiento de archivos CSV para carga inicial
5. Semilla de datos demo controlada
6. Bitácora de auditoría mínima

## Backend propuesto

### Stack sugerido
- ASP.NET Core Web API
- C#
- Entity Framework Core
- FluentValidation o validación equivalente ligera
- patrón CQRS ligero solo donde aporte claridad, no como requisito dogmático

### Organización sugerida
- `src/Api`
- `src/Application`
- `src/Domain`
- `src/Infrastructure`
- `tests/UnitTests`
- `tests/IntegrationTests`

### Criterio arquitectónico
- usar casos de uso o handlers por slice funcional
- evitar repositorios genéricos excesivos
- mantener reglas de negocio explícitas en servicios de aplicación o dominio
- concentrar acceso a datos en Infrastructure

## Frontend propuesto

### Stack sugerido
- Angular
- Angular Material o una librería ligera equivalente para acelerar formularios, tablas y diálogos

### Módulos sugeridos
- catálogo
- inventario
- venta rápida
- carga inicial
- demo/admin
- reportes mínimos

### Criterio de UI
- pantallas simples, operativas y rápidas
- formularios claros
- feedback inmediato en validaciones de inventario y venta
- fuerte separación entre acciones demo y acciones reales

## Base de datos propuesta

### Tipo
- relacional

### Justificación
- el dominio es transaccional
- hay relaciones claras entre productos, inventario, movimientos y ventas
- se requieren integridad, trazabilidad y consultas operativas relativamente estructuradas

### Propuesta inicial
- SQL Server si se prioriza afinidad con ASP.NET Core y tooling empresarial
- PostgreSQL si se prioriza portabilidad y costo de infraestructura

## Módulos principales

### 1. Catálogo
Responsabilidad:
- alta, edición, consulta y revisión de productos

### 2. Inventario actual
Responsabilidad:
- mantener saldo vigente por producto

### 3. Movimientos
Responsabilidad:
- registrar cada afectación al inventario y permitir trazabilidad

### 4. Venta rápida
Responsabilidad:
- registrar ventas simples y descontar inventario en tiempo real

### 5. Carga inicial
Responsabilidad:
- importar plantilla formal de inventario inicial
- validar filas
- crear o homologar productos
- inicializar existencias

### 6. Demo/admin
Responsabilidad:
- cargar semilla demo
- ejecutar reset seguro solo en sandbox/demo

### 7. Reportes mínimos
Responsabilidad:
- consultas operativas básicas de ventas, inventario y anomalías

## Responsabilidades por módulo

- Catálogo: producto, revisión, datos incompletos
- Inventario: saldo actual, disponibilidad, consistencia
- Movimientos: entradas, salidas, ajustes, trazabilidad
- Venta rápida: venta, detalle, reversa
- Carga inicial: parsing, validación, preview, aplicación
- Demo/admin: seed demo, reset demo, auditoría del reset
- Reportes: lectura agregada, sin lógica transaccional

## Justificación de decisiones técnicas

- ASP.NET Core Web API encaja con la preferencia técnica y el tipo de sistema
- Angular permite un frontend administrativo sólido y rápido de estructurar
- EF Core acelera persistencia inicial sin impedir evolución posterior
- arquitectura modular incremental reduce riesgo y permite construir por slices
- separar reset demo como módulo admin minimiza riesgo de contaminar operación real

## Qué se deja fuera del MVP

- multi-almacén formal
- compras avanzadas
- facturación
- contabilidad
- permisos finos complejos
- sincronización offline
- analítica avanzada
- integración automática con múltiples proveedores
