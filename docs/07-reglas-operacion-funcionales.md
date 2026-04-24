# 07. Reglas de operación funcionales

## Propósito

Este documento traduce los hallazgos del Excel y las reglas provisionales en reglas funcionales de operación para el MVP. No define todavía implementación técnica final ni base de datos física.

## Criterio de lectura

### Qué está confirmado
- el negocio necesita controlar inventario, registrar ventas rápidas, importar catálogos y generar reportes
- el Excel actual mezcla catálogo, existencias y movimientos
- existen productos con datos incompletos y anomalías operativas que el sistema debe soportar sin perder trazabilidad

### Qué se infiere
- `Hoja 4` parece el mejor corte base actual
- `F` parece representar existencia consolidada del corte
- columnas posteriores a `F` parecen movimientos por bloque
- `C` parece marca en la mayoría de registros

### Qué propongo provisionalmente
- diseñar operación del MVP soportando revisión de datos y estados incompletos
- evitar reglas demasiado rígidas que impidan migrar la operación real

## 1. Catálogo de productos

### Qué identifica a un producto

#### Confirmado
- hoy existen descripción, código, marca probable, costo y precio en el Excel

#### Provisional
- si existe `codigo`, el identificador funcional provisional será `codigo + descripcion`
- si no existe `codigo`, el sistema asignará un identificador interno y dejará el producto en estado de revisión

### Cómo tratar productos sin código

- se pueden migrar y registrar si tienen descripción utilizable
- deben quedar marcados como `requiere_revision`
- no deben bloquear inventario inicial
- para venta rápida, deben poder buscarse por descripción, pero con advertencia operativa

### Cómo tratar códigos duplicados

- no se fusionan automáticamente
- cada caso se conserva por separado hasta validación funcional
- el sistema debe poder marcar duplicados potenciales para homologación
- si un código duplicado entra por importación, queda en conflicto y requiere revisión

### Cómo tratar marca y proveedor mientras no estén 100 por ciento validados

- `marca` se trata provisionalmente como dato catálogo visible
- `proveedor` queda opcional y en revisión mientras no exista fuente confiable
- no se debe poblar proveedor a partir de `marca` sin confirmación

### Qué campos son obligatorios

#### Para alta operativa mínima
- descripcion
- estatus activo
- identificador interno del sistema

#### Para operación plena deseable
- codigo o código alterno confiable
- precio de venta
- costo

### Qué campos pueden quedar en revisión

- codigo
- marca
- proveedor principal
- costo
- precio de venta
- equivalencias o códigos alternos

## 2. Inventario

### Cómo se define existencia actual

#### Provisional
- la existencia actual del MVP será el saldo vigente por producto en `InventarioActual`
- el saldo inicial se cargará desde la hoja base validada, provisionalmente `Hoja 4`

### Cómo se corrige existencia inicial

- la carga inicial no debe reescribir silenciosamente el saldo
- cualquier corrección posterior debe registrarse como ajuste manual con motivo
- debe poder distinguirse entre saldo migrado y saldo corregido

### Cómo se registran entradas y salidas

- toda variación posterior al arranque debe registrarse como movimiento
- tipos mínimos: entrada, salida, ajuste_positivo, ajuste_negativo, venta, cancelacion_venta
- cada movimiento debe afectar el saldo del inventario actual

### Cómo tratar existencia negativa

- el sistema no debe ocultarla
- debe marcarla como incidencia crítica
- puede existir por migración o por operación, pero debe quedar auditada
- conviene alertar antes de vender más si el saldo ya es negativo

### Cómo hacer ajustes manuales

- deben requerir usuario autenticado
- deben guardar motivo
- deben guardar cantidad anterior y cantidad resultante o al menos cantidad de ajuste
- deben quedar visibles en historial

### Qué eventos afectan inventario

- carga inicial
- entrada manual
- salida manual
- venta confirmada
- cancelación o reversa de venta
- ajuste manual
- importación que actualice saldos, solo si esa capacidad se aprueba después

## 3. Venta rápida

### Flujo mínimo de venta

1. identificar usuario
2. buscar producto por código o descripción
3. validar disponibilidad y precio
4. capturar cantidad
5. confirmar venta
6. descontar inventario
7. generar folio interno

### Si la venta descuenta inventario inmediatamente

#### Provisional
- sí, la venta debe descontar inventario inmediatamente al confirmarse
- si la venta se cancela, debe generarse movimiento de reversa

### Cómo tratar productos sin precio

- no deben venderse normalmente sin definir precio
- el sistema puede permitir excepción solo con usuario autorizado y motivo
- por defecto, quedan bloqueados para venta rápida

### Cómo tratar cambios manuales de precio

- deben permitirse solo con registro de usuario y motivo
- debe conservarse el precio aplicado en el detalle de venta, aunque luego cambie el precio vigente del producto
- si hay descuento manual, debe quedar explícito

### Qué datos mínimos guarda una venta

- venta_id
- folio
- fecha y hora
- usuario
- turno o corte si aplica
- total
- estatus
- detalle por producto
- precio unitario aplicado
- cantidad
- subtotal por línea

### Cómo manejar cancelaciones o correcciones

- una venta confirmada no debe borrarse físicamente
- la cancelación debe cambiar estatus y revertir inventario
- si solo se corrige un detalle, conviene manejarlo como cancelación y recreación en MVP para simplificar trazabilidad

## 4. Proveedores

### Cómo registrar proveedor real

- debe existir catálogo propio de proveedores
- el proveedor se registra como entidad separada del producto
- la relación se hace por vínculo explícito producto-proveedor

### Qué pasa cuando el proveedor no se conoce aún

- el producto puede existir sin proveedor principal
- debe quedar como dato pendiente de homologación
- no debe bloquear inventario ni venta rápida del MVP

### Cómo ligar proveedor con producto

- se propone relación muchos a muchos mediante entidad intermedia
- debe poder existir un proveedor principal y opcionalmente códigos externos por proveedor

### Cómo manejar futuros catálogos de proveedor

- cada importación debe guardar su origen
- el matching debe intentar relacionar producto existente con producto del proveedor
- conflictos o no coincidencias deben quedar en revisión antes de aplicar cambios destructivos

## 5. Importación

### Reglas mínimas para importar catálogos

- aceptar archivo estructurado CSV o Excel
- mostrar preview antes de aplicar
- mapear columnas fuente contra campos internos
- registrar lote de importación y usuario ejecutor

### Reglas de matching provisional por código o descripción

Orden sugerido:
1. coincidencia exacta por código del producto
2. coincidencia por código alterno del proveedor, si existe
3. coincidencia asistida por descripción similar, nunca automática sin revisión

### Qué hacer con conflictos

- no sobrescribir automáticamente datos críticos ambiguos
- marcar conflicto si un código coincide con descripciones distintas
- marcar conflicto si descripción parece igual pero hay costos o marcas inconsistentes
- requerir validación humana antes de consolidar

### Qué datos quedan en revisión

- productos nuevos sin código interno
- costos faltantes
- precios faltantes
- proveedor no identificado
- duplicados potenciales
- cambios masivos sospechosos

## 6. Reportes

### Ventas
- ventas por periodo
- ventas por usuario
- ventas por turno, si aplica
- productos más vendidos

### Utilidad bruta
- utilidad bruta por venta y por periodo, solo cuando exista costo confiable
- cuando costo falte o sea dudoso, reportar el caso como utilidad no confiable

### Productos rentables
- ranking simple por margen bruto estimado
- excluir o marcar productos con costo dudoso

### Compras por proveedor

#### Infiere una necesidad futura
- este reporte depende de que exista captura o importación formal de proveedor y costo
- puede quedar como módulo preparado pero no completamente usable desde día 1

### Inventario actual
- existencias actuales por producto
- productos con saldo negativo
- productos con saldo cero
- inventario valorizado con advertencias por costo faltante

### Productos con datos incompletos o anómalos
- sin código
- sin costo
- sin precio
- con costo mayor o igual al precio
- con proveedor faltante
- con duplicado potencial

## 7. Usuarios y turnos

### Operación mínima para MVP

- catálogo de usuarios
- autenticación mínima
- registro del usuario que vende o ajusta
- bitácora básica de movimientos y ventas

### Qué acciones requieren usuario

- confirmar venta
- cancelar venta
- hacer ajuste manual
- importar catálogo
- aprobar conflicto de importación
- editar precio o costo manualmente

### Si hace falta turno o corte desde MVP o puede quedar como fase posterior

#### Confirmado
- el requerimiento inicial menciona usuarios y turnos

#### Provisional
- el MVP puede incluir turno básico opcional desde inicio si el flujo lo necesita para corte diario
- si se busca velocidad, puede arrancarse con usuario obligatorio y turno como fase cercana posterior
- la decisión cambia reportes y trazabilidad, por lo que conviene marcarla como crítica de diseño

## Decisiones funcionales críticas que cambian el diseño

1. si `codigo` será realmente único
2. si `turno` entra desde MVP o en fase inmediata posterior
3. si productos sin precio pueden venderse por excepción
4. si una importación puede actualizar costo y precio automáticamente
5. si la carga inicial tomará `Hoja 4` como snapshot oficial
