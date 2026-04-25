# Slice 6. Reportes operativos mínimos

## Alcance implementado

Objetivo de este slice: dar visibilidad útil y accionable sin montar BI ni dashboards complejos.

### Backend
- endpoint nuevo `GET /api/reports/operations`
- consolida en una sola respuesta:
  - resumen operativo de inventario y ventas confirmadas
  - inventario actual con valorización básica
  - ventas recientes
  - productos con anomalías
  - ranking inicial de productos rentables por utilidad bruta estimada

### Reglas operativas implementadas
- inventario valorizado con costo y precio vigentes actuales del producto
- utilidad bruta estimada solo cuando el producto tiene `CurrentCost`
- ventas canceladas se muestran en recientes, pero no suman a ventas confirmadas ni utilidad
- anomalías detectadas por producto:
  - `sin_codigo`
  - `sin_costo`
  - `sin_precio`
  - `requiere_revision[:motivo]`
  - `stock_negativo`
  - `sin_existencia`

### Frontend
- pantalla nueva `Reportes`
- tarjetas de resumen con productos, inventario valorizado y ventas confirmadas
- listado corto de anomalías
- tabla de ventas recientes con utilidad cuando aplica
- preview de inventario actual con flags operativos
- tabla de productos más rentables disponible cuando existe base de costo

### Pruebas críticas agregadas
- validación de consolidado operativo con:
  - ventas confirmadas y canceladas
  - utilidad bruta base
  - anomalías por datos faltantes y stock negativo
  - ranking de producto rentable

## Contrato implementado

### GET `/api/reports/operations`

Respuesta resumida:
```json
{
  "summary": {
    "totalProducts": 3,
    "productsWithStock": 2,
    "productsWithoutStock": 1,
    "productsWithNegativeStock": 0,
    "totalStockUnits": 28,
    "inventoryCostValue": 3500.00,
    "inventoryRetailValue": 5400.00,
    "confirmedSalesCount": 2,
    "confirmedSalesTotal": 495.00,
    "confirmedSalesGrossProfit": 160.00,
    "latestSaleDate": "2026-04-25"
  },
  "inventory": [],
  "recentSales": [],
  "productAnomalies": [],
  "profitableProducts": []
}
```

## Riesgos y trade-offs
- la utilidad usa costo vigente actual del producto, no costo histórico congelado al momento de la venta
- la valorización de inventario también usa costo/precio actual, útil para operación rápida pero no para cierre contable
- el reporte regresa una sola carga consolidada, suficiente para el MVP actual; si el volumen crece convendrá paginar o separar consultas
