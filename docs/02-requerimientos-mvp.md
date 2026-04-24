# 02. Requerimientos MVP

## Enfoque

Este documento es un primer borrador funcional. No fija todavía stack técnico final. Parte del objetivo declarado del proyecto y de la auditoría inicial del Excel.

## Qué se sabe

El MVP debe cubrir una operación básica pero usable para inventario operativo en refaccionaria.

## Módulos propuestos

### 1. Productos
Objetivo:
- alta, edición y consulta de productos

Capacidades mínimas:
- descripción
- código interno o comercial
- marca
- proveedor principal
- costo vigente
- precio de venta vigente
- estatus activo/inactivo
- banderas de revisión de datos

### 2. Inventario
Objetivo:
- conocer existencia actual y ajustar movimientos de forma controlada

Capacidades mínimas:
- existencia actual por producto
- ajuste manual con motivo
- entradas
- salidas
- historial básico de movimientos
- alerta visual para existencia negativa o inconsistente

### 3. Venta rápida
Objetivo:
- registrar ventas sin fricción operativa

Capacidades mínimas:
- búsqueda por código o descripción
- captura rápida de cantidad
- descuento simple, si aplica
- descuento automático de inventario
- total de venta
- folio o identificador interno

### 4. Proveedores
Objetivo:
- asociar productos con origen comercial

Capacidades mínimas:
- catálogo de proveedores
- datos básicos de contacto
- relación producto-proveedor
- referencia o código de proveedor, si existe

### 5. Importación de catálogos
Objetivo:
- incorporar listas externas sin captura manual total

Capacidades mínimas:
- carga de archivo CSV o Excel
- mapeo de columnas
- detección de coincidencias por código
- preview antes de aplicar cambios
- reporte de filas con error o conflicto

### 6. Reportes
Objetivo:
- dar visibilidad operativa mínima

Capacidades mínimas:
- inventario actual
- productos sin código
- productos sin costo o sin precio
- productos con existencia negativa
- productos con margen sospechoso
- movimientos por periodo
- ventas por periodo

### 7. Usuarios y turnos
Objetivo:
- trazabilidad operativa mínima

Capacidades mínimas:
- usuarios con rol
- registro de inicio y cierre de turno
- asociación de ventas y ajustes a usuario
- bitácora básica de actividad

## Reglas funcionales que conviene validar

- si un producto puede existir sin código por un tiempo limitado
- si un producto puede tener múltiples precios o solo uno vigente
- si proveedor es obligatorio o opcional
- si se manejarán apartados, devoluciones o cancelaciones en venta rápida
- si inventario se controla por pieza únicamente o habrá otras unidades
- si habrá sucursales o solo una ubicación

## No incluido todavía

- contabilidad
- facturación
- compras avanzadas
- multi-almacén formal
- permisos finos por acción
- interfaz final definitiva
