# 17. Modelo de datos técnico inicial

## Propósito

Definir una propuesta técnica inicial de persistencia para el MVP sin cerrar todavía el modelo físico final detallado.

## Tablas o entidades principales

### 1. Productos
Campos principales:
- producto_id
- clave_interna
- codigo_principal nullable
- descripcion
- marca_texto nullable
- proveedor_principal_id nullable
- costo_vigente nullable
- precio_venta_vigente nullable
- unidad nullable
- requiere_revision
- motivo_revision nullable
- estatus_producto
- created_at
- updated_at

Índices obvios:
- unique parcial por `clave_interna`
- índice por `codigo_principal`
- índice por `descripcion`

### 2. Proveedores
Campos principales:
- proveedor_id
- nombre
- clave_externa nullable
- contacto nullable
- telefono nullable
- email nullable
- activo

Índices obvios:
- índice por `nombre`
- índice por `clave_externa`

### 3. ProductoProveedores
Campos principales:
- producto_proveedor_id
- producto_id
- proveedor_id
- codigo_proveedor nullable
- es_principal
- costo_referencia nullable
- activo

Índices obvios:
- índice compuesto `producto_id, proveedor_id`
- índice por `codigo_proveedor`

### 4. InventariosActuales
Campos principales:
- inventario_actual_id
- producto_id
- existencia_actual
- ubicacion nullable
- fecha_corte_base nullable
- origen_corte_base nullable
- requiere_revision
- updated_at

Índices obvios:
- unique por `producto_id` en MVP de un solo almacén
- índice por `existencia_actual`

### 5. MovimientosInventario
Campos principales:
- movimiento_inventario_id
- producto_id
- tipo_movimiento
- cantidad
- saldo_resultante nullable
- origen_tipo
- origen_id nullable
- motivo nullable
- referencia_externa nullable
- usuario_id nullable
- turno_id nullable
- created_at

Índices obvios:
- índice por `producto_id, created_at desc`
- índice por `tipo_movimiento`
- índice por `origen_tipo, origen_id`

### 6. Ventas
Campos principales:
- venta_id
- folio
- usuario_id
- turno_id nullable
- subtotal
- descuento_total
- total
- estatus_venta
- motivo_cancelacion nullable
- created_at

Índices obvios:
- unique por `folio`
- índice por `created_at`
- índice por `usuario_id`

### 7. VentaDetalles
Campos principales:
- venta_detalle_id
- venta_id
- producto_id
- descripcion_snapshot
- codigo_snapshot nullable
- cantidad
- precio_unitario
- costo_referencia nullable
- descuento_linea
- subtotal_linea

Índices obvios:
- índice por `venta_id`
- índice por `producto_id`

### 8. AjustesInventario
Campos principales:
- ajuste_inventario_id
- producto_id
- tipo_ajuste
- cantidad_ajuste
- existencia_anterior
- existencia_nueva
- motivo
- usuario_id
- turno_id nullable
- created_at

Índices obvios:
- índice por `producto_id, created_at desc`
- índice por `usuario_id`

### 9. CargasInventarioInicial
Campos principales:
- carga_inventario_inicial_id
- tipo_carga
- nombre_archivo nullable
- estatus_carga
- resumen_json nullable
- usuario_id
- created_at

Índices obvios:
- índice por `created_at`
- índice por `estatus_carga`

### 10. CargaInventarioInicialDetalles
Campos principales:
- carga_inventario_inicial_detalle_id
- carga_inventario_inicial_id
- fila_origen
- codigo nullable
- descripcion
- existencia_inicial
- costo nullable
- precio_venta nullable
- producto_id_match nullable
- estatus_fila
- motivo_revision nullable

Índices obvios:
- índice por `carga_inventario_inicial_id`
- índice por `producto_id_match`
- índice por `estatus_fila`

### 11. Usuarios
Campos principales:
- usuario_id
- username
- nombre
- rol
- activo
- created_at

Índices obvios:
- unique por `username`
- índice por `rol`

### 12. Turnos
Campos principales:
- turno_id
- usuario_id
- estatus_turno
- fecha_hora_apertura
- fecha_hora_cierre nullable
- observaciones nullable

Índices obvios:
- índice por `usuario_id, fecha_hora_apertura desc`
- índice por `estatus_turno`

### 13. AuditoriaDemoResets
Campos principales:
- auditoria_demo_reset_id
- ejecutado_por_usuario_id
- motivo
- entorno
- resumen_json
- created_at

Índices obvios:
- índice por `created_at`
- índice por `ejecutado_por_usuario_id`

## Relaciones principales
- Productos 1:N MovimientosInventario
- Productos 1:1 InventariosActuales en MVP
- Productos 1:N VentaDetalles
- Productos N:M Proveedores vía ProductoProveedores
- Ventas 1:N VentaDetalles
- Usuarios 1:N Ventas
- Usuarios 1:N MovimientosInventario
- Usuarios 1:N AjustesInventario
- Usuarios 1:N CargasInventarioInicial
- CargasInventarioInicial 1:N CargaInventarioInicialDetalles

## Qué partes podrían cambiar después
- soporte multi-almacén real en `InventariosActuales`
- separación de precio/costo histórico en tablas independientes
- mayor normalización de catálogo de códigos alternos
- consolidación o separación adicional de ajustes y movimientos
- estrategia de auditoría completa
