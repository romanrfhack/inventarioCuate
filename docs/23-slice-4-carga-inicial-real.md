# 23. Slice 4 - Carga inicial real

## Alcance

Este slice convierte el flujo de `initial-load` de scaffold técnico a flujo real y verificable para carga inicial desde la plantilla formal definida para el proyecto.

Incluye:
- parser CSV real
- preview real persistido por fila
- apply transaccional real sobre cargas válidas
- trazabilidad de usuario, fecha y lote
- pruebas automatizadas del parser y de consistencia de persistencia del flujo
- script operativo mínimo para ejecutar una carga controlada contra la API ya levantada
- consulta de cargas previas y su estado

## Reglas aplicadas

- no mezclar demo con datos reales
- no fusionar duplicados automáticamente
- productos sin código pueden existir con identificador interno y bandera de revisión
- productos sin precio no bloquean carga inicial
- productos sin costo o proveedor quedan marcados para revisión
- la carga inicial requiere token de confirmación
- apply solo corre sobre preview válido y estado permitido
- se registra inventario base y movimiento `carga_inicial`

## Decisiones tomadas

- el primer alcance funcional usa CSV enviado como contenido y no importador genérico multi-fuente
- warnings no bloquean el preview ni el apply
- filas inválidas sí impiden aplicar la carga completa
- el producto se homologa primero por código exacto cuando existe
- si no hay código, se crea producto nuevo con clave interna y `RequiresReview`

## Endpoints afectados

- `GET /api/initial-load`
- `GET /api/initial-load/{loadId}`
- `POST /api/initial-load/preview`
- `POST /api/initial-load/apply/{loadId}`

## Validaciones implementadas

### Preview
- columnas obligatorias: `descripcion`, `existencia_inicial`
- `existencia_inicial` debe ser numérica y no negativa
- `costo` y `precio_venta`, si vienen, deben ser numéricos y no negativos
- productos sin código generan warning
- productos sin costo, precio o proveedor generan warning
- se persiste detalle por fila con estado `valid`, `warning` o `invalid`

### Apply
- token de confirmación obligatorio
- la carga debe existir
- la carga debe estar en estado `previewed`
- no se permite apply sobre previews ya mutados o estados no permitidos
- no se aplica si existen filas inválidas

## Riesgos

- todavía no hay matching más sofisticado que código exacto
- proveedor sigue siendo dato pendiente de homologación real
- aún faltan pruebas HTTP end-to-end embebidas en el suite de tests
- todavía no existe UI completa para operar esta carga desde frontend

## Pendientes del siguiente slice

- pruebas HTTP end-to-end contra API real y base de datos para preview/apply completo
- parser multipart/file upload si se decide cambiar el contrato actual
- homologación asistida de productos duplicados o similares
- vista frontend para preview y confirmación de carga
- endurecer auditoría de cambios por lote

## Qué ya quedó validado con más confianza operativa

- el flujo preview -> apply fue validado manualmente a nivel API en el VPS
- el parser CSV quedó validado automáticamente
- la persistencia base del lote aplicado quedó validada automáticamente en tests
- la presencia de productos, inventario actual y movimientos `carga_inicial` quedó cubierta en pruebas de consistencia

## Validación manual ya confirmada

Con evidencia real del VPS quedó validado manualmente que:
- `initial-load/preview` respondió correctamente
- `initial-load/apply` respondió `202 Accepted` usando preview fresco y token válido

Eso confirma que el flujo técnico preview -> apply ya funciona a nivel API sobre el baseline validado.
