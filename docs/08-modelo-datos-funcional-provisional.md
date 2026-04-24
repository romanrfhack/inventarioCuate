# 08. Modelo de datos funcional provisional

## Propósito

Este documento propone entidades funcionales para el MVP a partir del Excel auditado y de las reglas provisionales. No define todavía la base de datos física final.

## Relación con documentos previos

- `docs/03-modelo-datos-borrador.md` sigue siendo útil como separación inicial de entidades
- este documento lo reemplaza funcionalmente para discusión del Paso 2
- cualquier diseño técnico posterior debe partir de este documento y no solo del borrador inicial

## 1. Producto

### Propósito
Representar el catálogo maestro interno del sistema.

### Campos sugeridos
- producto_id
- clave_interna
- codigo_principal
- descripcion
- marca_texto
- proveedor_principal_id nullable
- estatus_producto
- requiere_revision
- motivo_revision
- fecha_alta
- fecha_ultima_revision

### Claves candidatas
- primaria: `producto_id`
- candidata operativa: `codigo_principal` cuando exista y esté validado
- candidata provisional: `codigo_principal + descripcion`

### Relaciones principales
- 1:N con `ProductoCodigoAlterno`
- 1:N con `ProductoProveedor`
- 1:1 o 1:N lógico con `InventarioActual`
- 1:N con `MovimientoInventario`
- 1:N con `VentaDetalle`

### Confirmado
- producto necesita al menos descripción, identificador interno y banderas de revisión

### Inferencia
- `codigo_principal` puede no ser suficientemente confiable como clave natural única

### Depende de validación cliente
- si el código será obligatorio y único

## 2. ProductoCodigoAlterno

### Propósito
Soportar códigos adicionales, códigos históricos o códigos de proveedor.

### Campos sugeridos
- producto_codigo_alterno_id
- producto_id
- tipo_codigo
- codigo
- proveedor_id nullable
- es_principal
- activo

### Claves candidatas
- primaria: `producto_codigo_alterno_id`
- candidata operativa: `tipo_codigo + codigo`

### Relaciones principales
- N:1 con `Producto`
- N:1 con `Proveedor`

### Confirmado
- existen riesgos con duplicados y futuros catálogos de proveedor

### Inferencia
- puede requerirse aunque no todos los códigos actuales lo necesiten desde día 1

### Depende de validación cliente
- si los códigos de proveedor deben conservarse explícitamente

## 3. Proveedor

### Propósito
Representar origen comercial de compra o catálogo.

### Campos sugeridos
- proveedor_id
- nombre
- clave_externa
- telefono
- email
- contacto
- activo
- requiere_revision

### Claves candidatas
- primaria: `proveedor_id`
- candidata operativa: `nombre` o `clave_externa`, sujeto a saneamiento

### Relaciones principales
- 1:N con `ProductoProveedor`
- 1:N con `ImportacionProveedor`
- 1:N con `ProductoCodigoAlterno`

### Confirmado
- proveedor debe existir como entidad independiente del producto

### Inferencia
- hoy no se puede poblar confiablemente desde el Excel auditado

### Depende de validación cliente
- fuente real del proveedor y si será obligatorio desde MVP

## 4. ProductoProveedor

### Propósito
Vincular producto con uno o varios proveedores.

### Campos sugeridos
- producto_proveedor_id
- producto_id
- proveedor_id
- codigo_proveedor
- es_principal
- costo_referencia
- activo

### Claves candidatas
- primaria: `producto_proveedor_id`
- candidata operativa: `producto_id + proveedor_id + codigo_proveedor`

### Relaciones principales
- N:1 con `Producto`
- N:1 con `Proveedor`

### Confirmado
- el vínculo producto-proveedor será necesario para compras e importaciones futuras

### Inferencia
- no es seguro que el MVP inicial necesite múltiples proveedores por producto desde la primera versión operativa

### Depende de validación cliente
- si basta proveedor principal o si necesita multivinculación desde el inicio

## 5. InventarioActual

### Propósito
Guardar el saldo vigente por producto para la operación diaria.

### Campos sugeridos
- inventario_actual_id
- producto_id
- almacen_id nullable
- existencia_actual
- fecha_corte_base
- origen_corte_base
- requiere_revision

### Claves candidatas
- primaria: `inventario_actual_id`
- candidata operativa: `producto_id` en escenario de un solo almacén

### Relaciones principales
- N:1 con `Producto`

### Confirmado
- el sistema necesita una entidad separada para saldo vigente

### Inferencia
- `almacen_id` puede omitirse en MVP si solo hay una ubicación

### Depende de validación cliente
- si existirá más de una ubicación física

## 6. MovimientoInventario

### Propósito
Registrar toda alteración del saldo de inventario.

### Campos sugeridos
- movimiento_inventario_id
- producto_id
- tipo_movimiento
- cantidad
- saldo_resultante nullable
- fecha_hora_movimiento
- origen_tipo
- origen_id nullable
- usuario_id nullable
- turno_id nullable
- motivo
- referencia_externa

### Claves candidatas
- primaria: `movimiento_inventario_id`

### Relaciones principales
- N:1 con `Producto`
- N:1 con `Usuario`
- N:1 con `Turno`
- relación polimórfica lógica con `Venta`, `AjusteInventario` o importación

### Confirmado
- movimientos deben separarse del saldo actual

### Inferencia
- el historial derivado del Excel no equivale a una bitácora transaccional definitiva

### Depende de validación cliente
- granularidad exacta de movimientos y controles de autorización

## 7. AjusteInventario

### Propósito
Representar ajustes manuales o de regularización con identidad propia.

### Campos sugeridos
- ajuste_inventario_id
- producto_id
- tipo_ajuste
- cantidad_ajuste
- existencia_anterior
- existencia_nueva
- motivo
- usuario_id
- turno_id nullable
- fecha_hora

### Claves candidatas
- primaria: `ajuste_inventario_id`

### Relaciones principales
- N:1 con `Producto`
- N:1 con `Usuario`
- N:1 con `Turno`
- 1:1 lógico o 1:N con `MovimientoInventario`

### Confirmado
- el negocio necesita correcciones controladas y trazables

### Inferencia
- conviene separarlo de `MovimientoInventario` por claridad operativa, aunque técnicamente pudiera modelarse dentro del mismo flujo

### Depende de validación cliente
- nivel de detalle y autorización requerido para ajustes

## 8. Venta

### Propósito
Representar la cabecera de una venta rápida.

### Campos sugeridos
- venta_id
- folio
- fecha_hora_venta
- usuario_id
- turno_id nullable
- subtotal
- descuento_total
- total
- estatus_venta
- motivo_cancelacion nullable

### Claves candidatas
- primaria: `venta_id`
- candidata operativa: `folio`

### Relaciones principales
- 1:N con `VentaDetalle`
- N:1 con `Usuario`
- N:1 con `Turno`

### Confirmado
- la venta necesita cabecera con folio, fecha, usuario y total

### Inferencia
- turno puede ser nullable si el MVP arranca sin corte formal

### Depende de validación cliente
- si habrá descuentos, cancelaciones parciales o devoluciones desde MVP

## 9. VentaDetalle

### Propósito
Representar los productos vendidos en cada venta.

### Campos sugeridos
- venta_detalle_id
- venta_id
- producto_id
- descripcion_snapshot
- codigo_snapshot
- cantidad
- precio_unitario
- costo_referencia nullable
- descuento_linea
- subtotal_linea

### Claves candidatas
- primaria: `venta_detalle_id`

### Relaciones principales
- N:1 con `Venta`
- N:1 con `Producto`

### Confirmado
- el detalle debe conservar snapshot del producto vendido y precio aplicado

### Inferencia
- costo de referencia puede ser nullable mientras existan costos faltantes

### Depende de validación cliente
- si necesitará promociones o reglas especiales desde MVP

## 10. ImportacionProveedor

### Propósito
Representar un lote de carga de catálogo o datos desde proveedor.

### Campos sugeridos
- importacion_proveedor_id
- proveedor_id nullable
- nombre_archivo
- fecha_hora_carga
- usuario_id
- estatus_importacion
- resumen_resultado

### Claves candidatas
- primaria: `importacion_proveedor_id`

### Relaciones principales
- N:1 con `Proveedor`
- N:1 con `Usuario`
- 1:N con `ImportacionProveedorDetalle`

### Confirmado
- la importación debe tener identidad propia y trazabilidad

### Inferencia
- puede usarse también para cargas internas, no solo de proveedor

### Depende de validación cliente
- si toda importación será asociada a proveedor específico

## 11. ImportacionProveedorDetalle

### Propósito
Guardar cada fila importada, su matching y su resultado.

### Campos sugeridos
- importacion_proveedor_detalle_id
- importacion_proveedor_id
- fila_origen
- codigo_fuente
- descripcion_fuente
- costo_fuente nullable
- precio_fuente nullable
- producto_id_match nullable
- estatus_match
- motivo_revision

### Claves candidatas
- primaria: `importacion_proveedor_detalle_id`

### Relaciones principales
- N:1 con `ImportacionProveedor`
- N:1 con `Producto`

### Confirmado
- la importación necesita una capa de detalle para revisión de conflictos

### Inferencia
- el matching por descripción solo debe ser asistido, no automático

### Depende de validación cliente
- tolerancia a conflictos y aprobación manual requerida

## 12. Usuario

### Propósito
Representar a la persona que opera ventas, ajustes e importaciones.

### Campos sugeridos
- usuario_id
- nombre
- username
- rol
- activo
- fecha_alta

### Claves candidatas
- primaria: `usuario_id`
- candidata operativa: `username`

### Relaciones principales
- 1:N con `Venta`
- 1:N con `MovimientoInventario`
- 1:N con `AjusteInventario`
- 1:N con `ImportacionProveedor`
- 1:N con `Turno`

### Confirmado
- el MVP necesita al menos trazabilidad básica por usuario

### Inferencia
- granularidad de roles todavía no está cerrada

### Depende de validación cliente
- roles mínimos y forma de autenticación

## 13. Turno

### Propósito
Representar una sesión operativa o corte de caja/inventario por usuario.

### Campos sugeridos
- turno_id
- usuario_id
- fecha_hora_apertura
- fecha_hora_cierre nullable
- estatus_turno
- observaciones

### Claves candidatas
- primaria: `turno_id`

### Relaciones principales
- N:1 con `Usuario`
- 1:N con `Venta`
- 1:N con `MovimientoInventario`
- 1:N con `AjusteInventario`

### Confirmado
- el requerimiento menciona usuarios y turnos

### Inferencia
- puede no ser obligatorio en la primera entrega funcional si se prioriza velocidad

### Depende de validación cliente
- si turno entra en MVP o en una fase inmediata posterior

## Resumen de madurez

### Confirmado
- Producto
- InventarioActual
- MovimientoInventario
- Venta
- VentaDetalle
- Usuario
- Proveedor como entidad separada
- ImportacionProveedor y su detalle como necesidad funcional

### En inferencia fuerte pero útil
- ProductoCodigoAlterno
- ProductoProveedor
- AjusteInventario como entidad separada
- Turno dentro de MVP estricto

### Depende de validación cliente
- unicidad real de código
- proveedor obligatorio o no
- alcance exacto de turnos
- política de excepciones de venta sin precio
- política de matching y actualización por importación
