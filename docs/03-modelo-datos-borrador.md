# 03. Modelo de datos borrador

## Propósito

Este borrador no define todavía el modelo final. Sirve para separar entidades que hoy aparecen mezcladas en el Excel.

## Entidades candidatas

### Producto
Campos probables:
- producto_id
- codigo
- descripcion
- marca
- proveedor_principal_id
- activo
- requiere_revision
- motivo_revision

### PrecioProducto
Campos probables:
- precio_producto_id
- producto_id
- costo_vigente
- precio_venta_vigente
- fecha_vigencia

### InventarioActual
Campos probables:
- inventario_id
- producto_id
- existencia_actual
- fecha_corte

### MovimientoInventario
Campos probables:
- movimiento_id
- producto_id
- tipo_movimiento
- cantidad
- fecha_movimiento
- origen
- usuario_id
- observaciones
- referencia_externa

### Proveedor
Campos probables:
- proveedor_id
- nombre
- clave_externa
- contacto
- telefono
- email
- activo

### Venta
Campos probables:
- venta_id
- folio
- fecha
- usuario_id
- turno_id
- total
- estatus

### VentaDetalle
Campos probables:
- venta_detalle_id
- venta_id
- producto_id
- cantidad
- precio_unitario
- costo_referencia
- subtotal

### Usuario
Campos probables:
- usuario_id
- nombre
- username
- rol
- activo

### Turno
Campos probables:
- turno_id
- usuario_id
- fecha_apertura
- fecha_cierre
- estatus

## Qué se sabe

Del Excel actual sí se puede sostener la existencia de atributos equivalentes a:
- descripción
- código
- marca
- costo
- precio de venta
- existencia total del corte
- bloques de movimiento por columnas posteriores

## Qué se infiere

- proveedor no está claramente identificado en la estructura visible del Excel auditado
- los movimientos detectados son inferidos desde columnas por bloque, no desde una bitácora transaccional formal
- la fecha exacta por movimiento todavía no es confiable a nivel de evento individual

## Qué falta validar

- unicidad real del código
n- política de actualización de costo y precio
- definición oficial de existencia inicial contra existencia final
- trazabilidad mínima requerida por venta, ajuste y turno
