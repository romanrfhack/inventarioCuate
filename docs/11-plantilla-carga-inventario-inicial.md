# 11. Plantilla de carga de inventario inicial

## Objetivo de la plantilla

Recibir del cliente un inventario inicial real, limpio y validable para cargar existencias al sistema sin depender del Excel histórico actual.

## Regla base

- esta plantilla sí será la fuente oficial de inventario inicial cuando el cliente la llene y valide
- si no se carga plantilla, el sistema arranca con existencias en cero

## Reglas de llenado

- una fila representa un producto
- no mezclar varios productos en una misma fila
- usar una sola unidad por fila
- capturar `existencia_inicial` como número real disponible al momento del corte
- no usar fórmulas, colores o notas visuales como mecanismo de datos
- evitar celdas combinadas si la plantilla luego se exporta a Excel
- si no se conoce proveedor, dejar vacío, no inventarlo
- si no se conoce código, dejar vacío, pero la descripción debe ser clara

## Validaciones mínimas

- `descripcion` obligatoria
- `existencia_inicial` obligatoria y mayor o igual a cero
- `costo`, si se captura, debe ser mayor o igual a cero
- `precio_venta`, si se captura, debe ser mayor o igual a cero
- `unidad`, si se captura, debe ser consistente, por ejemplo `pieza`
- no duplicar filas idénticas de producto sin justificación
- si `codigo` se repite, debe revisarse antes de carga definitiva

## Columnas obligatorias

- `descripcion`
- `existencia_inicial`

## Columnas opcionales pero altamente recomendadas

- `codigo`
- `marca`
- `proveedor`
- `costo`
- `precio_venta`
- `unidad`
- `ubicacion`
- `observaciones`

## Propuesta de columnas finales

1. `codigo`
2. `descripcion`
3. `marca`
4. `proveedor`
5. `costo`
6. `precio_venta`
7. `existencia_inicial`
8. `unidad`
9. `ubicacion`
10. `observaciones`

### Justificación breve

Esta estructura cubre lo mínimo operativo para:
- crear o homologar catálogo
- cargar saldo inicial
- tener contexto comercial básico
- evitar depender de campos ambiguos o demasiado avanzados para la primera carga

## Errores frecuentes

- dejar descripción demasiado genérica
- usar costo con texto o símbolos
- capturar precio de venta en la columna de costo o al revés
- registrar existencia negativa en arranque
- repetir el mismo producto en varias filas sin criterio
- usar marca como proveedor
- dejar unidad inconsistente entre productos similares

## Ejemplo de 5 registros

| codigo | descripcion | marca | proveedor | costo | precio_venta | existencia_inicial | unidad | ubicacion | observaciones |
|---|---|---|---|---:|---:|---:|---|---|---|
| BUJ-001 | Bujía NGK CR7HSA | NGK | MotoPartes del Centro | 38.50 | 65.00 | 12 | pieza | A1 | producto de rotación media |
| ACE-20W50-1L | Aceite 20W50 1L | Akron | Lubricantes del Bajío | 72.00 | 110.00 | 20 | pieza | B2 | presentación 1 litro |
| BAL-FT150-D | Balatas delanteras FT150 | Genérica | Frenos MX | 95.00 | 160.00 | 8 | juego | C1 | validar compatibilidades |
| FIL-AIR-ITALIKA125 | Filtro de aire Italika 125 | Italika | | 48.00 | 85.00 | 5 | pieza | D3 | proveedor pendiente |
| LLN-275-17 | Llanta 275-17 | Chao Yang | Llantas Express | 410.00 | 590.00 | 3 | pieza | E1 | inventario contado manualmente |
