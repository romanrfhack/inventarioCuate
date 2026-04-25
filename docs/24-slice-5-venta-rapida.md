# Slice 5. Venta rápida

## Slice 5.1, alcance corto implementado

Suposición pragmática: aunque en la documentación previa la venta rápida aparecía como slice 3, este cambio se registró como **Slice 5** porque el pedido actual así lo solicita y la carga inicial ya existe como baseline operativa.

### Backend
- endpoint `POST /api/sales/quick`
- endpoint `GET /api/sales`
- endpoint `POST /api/sales/{saleId}/cancel`
- valida que existan partidas, producto válido, cantidad mayor a cero y precio disponible
- valida existencia suficiente por producto considerando partidas repetidas en la misma venta
- crea cabecera `Sale` y detalle `SaleDetail`
- descuenta inventario actual inmediatamente
- registra movimiento `venta` en `InventoryMovements`
- permite cancelación segura idempotente por estatus, con reversa de existencias y movimiento `venta_cancelacion`
- actualiza reset demo para limpiar ventas y detalles

### Frontend
- pantalla `Venta rápida` ampliada a captura de múltiples partidas
- selección de producto, cantidad y precio unitario por renglón
- autocompleta precio sugerido vigente
- muestra última venta registrada y stock restante por partida
- listado básico de ventas recientes
- acción de cancelación desde listado

### Pruebas
- venta exitosa con múltiples partidas y producto repetido en la misma venta
- venta rechazada por stock insuficiente
- cancelación revierte stock, registra movimiento de reversa y aparece en listado

## Contratos implementados

### POST `/api/sales/quick`
```json
{
  "items": [
    {
      "productId": "uuid",
      "quantity": 2,
      "unitPrice": 350.00
    },
    {
      "productId": "uuid-otro",
      "quantity": 1,
      "unitPrice": 145.00
    }
  ]
}
```

Respuesta exitosa:
```json
{
  "saleId": "uuid",
  "folio": "VTA-20260425-0001",
  "total": 845.00,
  "createdAt": "2026-04-25T20:00:00Z",
  "items": [
    {
      "productId": "uuid",
      "description": "Balata delantera sedan",
      "quantity": 2,
      "unitPrice": 350.00,
      "lineTotal": 700.00,
      "remainingStock": 8
    },
    {
      "productId": "uuid-otro",
      "description": "Aceite 5W30 litro",
      "quantity": 1,
      "unitPrice": 145.00,
      "lineTotal": 145.00,
      "remainingStock": 19
    }
  ]
}
```

### GET `/api/sales`
Devuelve hasta 50 ventas recientes con estatus, total, cantidad de partidas y desglose básico de renglones.

### POST `/api/sales/{saleId}/cancel`
Respuesta exitosa:
```json
{
  "saleId": "uuid",
  "folio": "VTA-20260425-0001",
  "status": "cancelled",
  "cancelledAt": "2026-04-25T20:15:00Z",
  "items": [
    {
      "productId": "uuid",
      "description": "Balata delantera sedan",
      "restoredQuantity": 2,
      "resultingStock": 10
    }
  ]
}
```

Errores relevantes:
- `400_VALIDATION_ERROR`
- `404_SALE_NOT_FOUND`
- `409_PRICE_REQUIRED`
- `409_INSUFFICIENT_STOCK`
- `409_SALE_ALREADY_CANCELLED`

## Slice 5.2, ajuste incremental implementado

### Backend
- endpoint nuevo `GET /api/sales/{saleId}` para detalle puntual de venta
- `GET /api/sales` ahora acepta filtros opcionales `folio`, `status`, `dateFrom`, `dateTo`
- mantiene límite de 50 registros recientes ya filtrados
- `GET /api/sales/{saleId}` responde `404_SALE_NOT_FOUND` si no existe

### Frontend
- listado de ventas con filtros básicos por folio, estatus y rango de fechas
- botón `Ver detalle` para cargar desglose puntual de una venta sin recargar toda la pantalla
- feedback visual mejorado al cancelar: resumen de última cancelación con partidas restituidas y stock resultante
- badges de estatus para distinguir confirmadas vs canceladas

### Pruebas críticas agregadas
- consulta de detalle de venta con partidas y resumen acumulado
- filtrado de ventas por estatus, coincidencia parcial de folio y fecha del día
