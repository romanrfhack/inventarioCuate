# 12. Flujo demo operativa MVP

## Propósito

Definir un flujo mínimo controlado para demostrar el MVP temprano sin depender del inventario real del cliente.

## Principio base

- la demo usa pocos productos controlados
- el inventario demo puede reiniciarse
- el flujo debe parecer operación real, pero sin mezclar datos demo con datos productivos

## Flujo demo mínimo

### 1. Reset demo

Objetivo:
- dejar el entorno demo en un estado limpio y repetible

Reglas:
- solo administrador
- solo disponible en entorno demo o sandbox
- debe pedir confirmación explícita
- debe dejar bitácora del evento

### 2. Cargar datos demo

Objetivo:
- insertar catálogo demo e inventario inicial demo

Fuente:
- `data/demo/productos_demo.csv`
- `data/demo/movimientos_demo.csv`, si se usa para poblar historial base

Resultado esperado:
- catálogo demo visible
- existencias iniciales demo disponibles
- historial básico demo si se decide cargar movimientos semilla

### 3. Visualizar catálogo

Objetivo:
- confirmar que los productos demo están visibles y utilizables

Validaciones mínimas:
- búsqueda por código
- búsqueda por descripción
- visualización de costo, precio, marca y proveedor
- identificación de existencia actual

### 4. Agregar inventario

Objetivo:
- probar entradas manuales o carga complementaria

Validaciones mínimas:
- seleccionar producto
- capturar cantidad
- registrar motivo
- reflejar nuevo saldo
- guardar movimiento

### 5. Simular venta

Objetivo:
- comprobar flujo de venta rápida y descuento inmediato

Validaciones mínimas:
- seleccionar producto
- capturar cantidad
- validar precio
- confirmar venta
- generar folio
- descontar inventario
- guardar movimiento asociado

### 6. Validar existencia resultante

Objetivo:
- comprobar que el saldo final coincide con operaciones realizadas

Validaciones mínimas:
- saldo antes de venta o ajuste
- saldo después de venta o ajuste
- alertas si se intenta dejar saldo inválido

### 7. Revisar movimientos

Objetivo:
- demostrar trazabilidad mínima del MVP

Validaciones mínimas:
- visualizar entradas
- visualizar ventas
- visualizar ajustes
- identificar usuario, fecha y motivo cuando aplique

### 8. Preparar sustitución de demo por inventario real

Objetivo:
- dejar claro que la demo es temporal y luego será reemplazada por carga real

Paso esperado:
- reset demo
- conservar configuración y usuarios
- cargar plantilla formal de inventario inicial real

## Función de reset

### Debe ser solo admin
Sí.

### Si aplica solo en entorno demo
Sí, recomendado de forma estricta.

### Qué datos borra
- ventas demo
- detalles de venta demo
- movimientos demo
- ajustes demo
- saldos demo
- cargas demo asociadas

### Qué datos conserva
- usuarios
- roles
- catálogos de configuración
- estructura del sistema
- bitácora de auditoría del reset

### Riesgos de esta función
- borrar datos reales por error
- contaminar auditoría si no deja rastro
- generar falsa confianza si no se restringe por entorno

### Recomendación de implementación segura
- habilitar solo en entorno marcado como demo
- limitar a rol administrador
- pedir doble confirmación
- registrar quién ejecutó el reset, cuándo y sobre qué datos
- impedir ejecución si existen indicadores de operación real

## Qué está confirmado

- el MVP temprano debe permitir reset demo, carga de inventario, ventas simuladas y validación de existencias

## Qué cambia por decisión de negocio

- la demo deja de depender del Excel como inventario vigente
- la plantilla formal de carga real se vuelve el paso natural posterior a la demo

## Qué sigue provisional

- si los movimientos demo deben cargarse automáticamente o generarse manualmente durante la prueba
- si el reset se implementa como comando único o como rutina administrativa guiada
