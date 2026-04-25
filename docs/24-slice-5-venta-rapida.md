# Slice 5. Venta rápida

## Alcance implementado

Suposición pragmática: aunque en la documentación previa la venta rápida aparecía como slice 3, este cambio se registró como **Slice 5** porque el pedido actual así lo solicita y la carga inicial ya existe como baseline operativa.

### Backend
- endpoint `POST /api/sales/quick`
- valida que existan partidas, producto válido, cantidad mayor a cero y precio disponible
- valida existencia suficiente por producto considerando partidas repetidas en la misma venta
- crea cabecera `Sale` y detalle `SaleDetail`
- descuenta inventario actual inmediatamente
- registra movimiento `venta` en `InventoryMovements`
- actualiza reset demo para limpiar ventas y detalles

### Frontend
- pantalla mínima `Venta rápida`
- selección de producto, cantidad y precio unitario
- autocompleta precio sugerido vigente
- muestra última venta registrada y stock restante por partida

### Pruebas
- venta exitosa: crea venta, detalle, descuento de stock y movimiento
- venta rechazada por stock insuficiente

## Contrato implementado

### POST `/api/sales/quick`
```json
{
  "items": [
    {
      "productId": "uuid",
      "quantity": 2,
      "unitPrice": 350.00
    }
  ]
}
```

Respuesta exitosa:
```json
{
  "saleId": "uuid",
  "folio": "VTA-20260425-0001",
  "total": 700.00,
  "createdAt": "2026-04-25T20:00:00Z",
  "items": [
    {
      "productId": "uuid",
      "description": "Balata delantera sedan",
      "quantity": 2,
      "unitPrice": 350.00,
      "lineTotal": 700.00,
      "remainingStock": 8
    }
  ]
}
```

Errores relevantes:
- `400_VALIDATION_ERROR`
- `409_PRICE_REQUIRED`
- `409_INSUFFICIENT_STOCK`
