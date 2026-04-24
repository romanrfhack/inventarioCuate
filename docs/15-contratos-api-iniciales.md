# 15. Contratos API iniciales

## Convenciones
- prefijo sugerido: `/api`
- respuestas de error con `code`, `message`, `details`
- respuestas de listas con paginación simple cuando aplique

## 1. Productos

### GET `/api/productos`
- request: query opcional `search`, `page`, `pageSize`, `soloConRevision`
- response: lista de productos resumidos
- validaciones: page/pageSize válidos
- errores esperados: `400_BAD_REQUEST`

### GET `/api/productos/{productoId}`
- request: ruta con id
- response: detalle de producto
- validaciones: id existente
- errores esperados: `404_NOT_FOUND`

### POST `/api/productos`
- request:
```json
{
  "codigo": "BUJ-001",
  "descripcion": "Bujía NGK CR7HSA",
  "marca": "NGK",
  "proveedorId": null,
  "costo": 38.50,
  "precioVenta": 65.00,
  "unidad": "pieza"
}
```
- response: producto creado
- validaciones: descripcion requerida, costo/precio no negativos
- errores esperados: `400_VALIDATION_ERROR`, `409_DUPLICATE_CODE_POTENTIAL`

## 2. Inventario actual

### GET `/api/inventario`
- request: query opcional `search`, `soloNegativos`, `soloSinExistencia`
- response: lista con producto y existencia actual
- validaciones: filtros válidos
- errores esperados: `400_BAD_REQUEST`

### GET `/api/inventario/{productoId}`
- request: id de producto
- response: saldo actual y resumen de movimientos
- validaciones: producto existente
- errores esperados: `404_NOT_FOUND`

## 3. Movimientos

### POST `/api/movimientos/entrada`
- request:
```json
{
  "productoId": "uuid",
  "cantidad": 5,
  "motivo": "Entrada manual demo"
}
```
- response: movimiento creado y saldo resultante
- validaciones: cantidad > 0, producto existente
- errores esperados: `400_VALIDATION_ERROR`, `404_NOT_FOUND`

### POST `/api/movimientos/ajuste`
- request:
```json
{
  "productoId": "uuid",
  "tipoAjuste": "negativo",
  "cantidad": 1,
  "motivo": "Producto dañado"
}
```
- response: ajuste y saldo resultante
- validaciones: cantidad > 0, motivo requerido
- errores esperados: `400_VALIDATION_ERROR`, `404_NOT_FOUND`

### GET `/api/movimientos`
- request: query opcional `productoId`, `tipo`, `from`, `to`
- response: lista de movimientos
- validaciones: rango válido
- errores esperados: `400_BAD_REQUEST`

## 4. Ventas

### POST `/api/ventas`
- request:
```json
{
  "items": [
    {
      "productoId": "uuid",
      "cantidad": 2,
      "precioUnitario": 65.00
    }
  ]
}
```
- response: venta creada con folio, total y detalle
- validaciones: items requeridos, cantidad > 0, precio válido, stock suficiente según regla operativa
- errores esperados: `400_VALIDATION_ERROR`, `409_INSUFFICIENT_STOCK`, `409_PRICE_REQUIRED`

### POST `/api/ventas/{ventaId}/cancelar`
- request:
```json
{
  "motivo": "Cancelación demo"
}
```
- response: venta cancelada y reversa aplicada
- validaciones: motivo requerido, venta cancelable
- errores esperados: `404_NOT_FOUND`, `409_INVALID_STATUS`

### GET `/api/ventas`
- request: query opcional `from`, `to`, `folio`
- response: lista resumida de ventas
- validaciones: rango válido
- errores esperados: `400_BAD_REQUEST`

## 5. Carga de inventario inicial

### POST `/api/cargas-iniciales/preview`
- request: archivo CSV multipart
- response:
```json
{
  "rows": 10,
  "validRows": 8,
  "invalidRows": 2,
  "errors": [
    { "row": 4, "code": "NEGATIVE_INITIAL_STOCK", "message": "existencia_inicial no puede ser negativa" }
  ]
}
```
- validaciones: archivo requerido, columnas mínimas presentes
- errores esperados: `400_INVALID_FILE`, `400_TEMPLATE_MISMATCH`

### POST `/api/cargas-iniciales/aplicar`
- request:
```json
{
  "previewToken": "token",
  "modo": "initial-load"
}
```
- response: resumen de carga aplicada
- validaciones: preview válido, entorno permitido, reglas de carga vigentes
- errores esperados: `400_INVALID_PREVIEW`, `409_INITIAL_LOAD_ALREADY_APPLIED`

## 6. Demo/reset

### POST `/api/demo/seed`
- request:
```json
{
  "includeMovements": true
}
```
- response: resumen de carga demo
- validaciones: entorno demo, rol admin
- errores esperados: `403_FORBIDDEN`, `409_INVALID_ENVIRONMENT`

### POST `/api/demo/reset`
- request:
```json
{
  "confirm": true,
  "reason": "Reset demo controlado"
}
```
- response: resumen de datos eliminados y resembrados
- validaciones: admin, entorno demo, confirm=true
- errores esperados: `403_FORBIDDEN`, `409_INVALID_ENVIRONMENT`, `400_CONFIRMATION_REQUIRED`

## 7. Reportes mínimos

### GET `/api/reportes/inventario-actual`
- request: query opcional `search`
- response: inventario actual consolidado
- validaciones: query válida
- errores esperados: `400_BAD_REQUEST`

### GET `/api/reportes/ventas`
- request: query `from`, `to`
- response: ventas del periodo
- validaciones: rango válido
- errores esperados: `400_BAD_REQUEST`

### GET `/api/reportes/anomalias-producto`
- request: sin body o con filtros opcionales
- response: productos sin código, sin costo, sin precio o con revisión
- validaciones: filtros válidos
- errores esperados: `400_BAD_REQUEST`
