# Slice 26 - Importación de catálogos de proveedor conocidos

## Objetivo
Habilitar importación incremental de catálogos de proveedor conocidos con perfiles específicos, sin tocar inventario local (`InventoryBalance.CurrentStock`) y sin perder información cuando varios proveedores manejan el mismo producto.

## Alcance del slice
- Preview y apply controlado para 3 perfiles iniciales: `alessia`, `masuda`, `c-cedis`.
- Persistencia de lote y detalle normalizado.
- Persistencia proveedor-producto en `ProductSupplierCatalogSnapshot`.
- Clasificación por fila: `match_codigo`, `producto_nuevo`, `conflicto_codigo`, `dato_incompleto`, `requiere_revision`.
- Actualización controlada de costo, precio y metadatos conservadores del producto.
- Alta controlada de producto nuevo cuando el match es claro y los datos mínimos son suficientes.
- Fixtures pequeñas y versionadas para pruebas reproducibles.

## Regla funcional crítica
Estos archivos **no son inventario local**. La existencia o stock del proveedor se guarda como disponibilidad informativa del proveedor. **Nunca** se actualiza `InventoryBalance.CurrentStock` desde este slice.

## Parser soportado
El parser ahora se nombra `SupplierCatalogSpreadsheetParser` porque procesa hojas de cálculo Excel/OpenXML y no solo CSV.

## Proveedores soportados

### 1) Alessia
- Fixture versionada: `data/provider-catalogs/fixtures/alessia/alessia-fixture.xlsx`
- Archivo real opcional: `data/provider-catalogs/raw/alessia/07 Abril Lista Alessia 26.xlsm`
- Hojas detectadas: `Ale 25`, `st`, `Pr`
- Hoja principal sugerida: `Ale 25`
- Hoja auxiliar útil: `st` para disponibilidad por código
- Mapeo canónico:
  - `Codigo` -> `codigo`
  - `MARCA` -> `marca`
  - `U-M` -> `unidad`
  - `Piezas Caja` -> `piezas_por_caja`
  - `NOMBRE - REFACCION` -> `descripcion`
  - `MOTOS COMPATIBLES` -> `compatibilidad`
  - `Precio` -> `precio_sugerido`
  - `Existencia` / hoja `st` -> `disponibilidad_proveedor`, `stock_proveedor_texto`
  - `NUEVO` -> `requiere_revision`, `motivo_revision`

### 2) Masuda
- Fixture versionada: `data/provider-catalogs/fixtures/masuda/masuda-fixture.xlsx`
- Archivo real opcional: `data/provider-catalogs/raw/masuda/LISTA DE PRECIO - MASUDA IMPORTADOR REGIONAL 09-ABRIL.xlsx`
- Hoja principal sugerida: `COMPRA`
- Mapeo canónico:
  - `CODIGO` -> `codigo`
  - `DESCRIPCION` -> `descripcion`
  - `U.` -> `unidad`
  - `IMPORTADOR REGIONAL` -> `costo_proveedor`
  - `LINEA` -> `linea`
  - `FAMILIA` -> `familia`
  - `SUB-FAMILIA` -> `subfamilia`
  - `PZA X CAJA` -> `piezas_por_caja`
  - `inventario MID...` -> `stock_proveedor_texto`, `disponibilidad_proveedor`

### 3) C-CEDIS
- Fixture versionada: `data/provider-catalogs/fixtures/c-cedis/c-cedis-fixture.xlsx`
- Archivo real opcional: `data/provider-catalogs/raw/c-cedis/ListaPreciosC-CEDIS-05042026.xlsx.xls`
- Formato real detectado: OpenXML, no depender de la extensión `.xls`
- Hojas detectadas: `Hoja1`, `Hoja3`
- Hoja principal sugerida: `Hoja1`
- Mapeo canónico:
  - `CODIGO` -> `codigo`
  - `DESCRIPCION` -> `descripcion`
  - `STOCK` -> `disponibilidad_proveedor`, `stock_proveedor_texto`
  - `COMPATIBILIDAD` -> `compatibilidad`
  - `DEPARTAMENTO` -> `familia`
  - `SECCION` -> `categoria`
  - precios por columnas -> `precio_sugerido`, `precios_por_nivel`

## Modelo canónico mínimo
- `proveedor`
- `perfil_importacion`
- `codigo`
- `descripcion`
- `marca`
- `unidad`
- `piezas_por_caja`
- `compatibilidad`
- `linea`
- `familia`
- `subfamilia`
- `categoria`
- `costo_proveedor`
- `precio_sugerido`
- `precios_por_nivel`
- `disponibilidad_proveedor`
- `stock_proveedor_texto`
- `hoja_origen`
- `fila_origen`
- `requiere_revision`
- `motivo_revision`

## Modelo proveedor-producto
Se agrega `ProductSupplierCatalogSnapshot` para conservar la última foto importada por proveedor y producto.

Campos persistidos:
- `id`
- `productId`
- `supplierName`
- `supplierProfile`
- `supplierCode`
- `supplierDescription`
- `supplierBrand`
- `supplierCost`
- `suggestedSalePrice`
- `priceLevelsJson`
- `supplierAvailability`
- `supplierStockText`
- `compatibility`
- `category`
- `line`
- `family`
- `subFamily`
- `lastImportBatchId`
- `lastImportedAt`
- `requiresReview`
- `reviewReason`

Regla de unicidad operativa: un snapshot por `productId + supplierProfile`.

## Reglas de apply
- `match_codigo`: actualiza o crea snapshot proveedor-producto y actualiza `Product` de forma conservadora.
- `producto_nuevo`: crea producto solo si tiene código, descripción y al menos costo o precio, y luego crea snapshot.
- `conflicto_codigo`: no aplica automáticamente.
- `dato_incompleto`: no aplica automáticamente.
- `requiere_revision`: no aplica automáticamente.
- Nunca actualiza `InventoryBalance.CurrentStock`.
- Nunca pisa snapshots de otros proveedores.
- Filas ambiguas quedan en revisión manual.
- `Product` solo se toca así:
  - `PrimaryCode` solo si está vacío.
  - campos descriptivos (`Brand`, `Unit`, `Compatibility`, `Line`, `Family`, `SubFamily`, `Category`, `PiecesPerBox`) solo si faltan.
  - `CurrentCost` y `CurrentSalePrice` se actualizan cuando la fila trae propuesta aplicable.
  - no se usa `SupplierName`, `SupplierAvailability` ni `SupplierStockText` de `Product` como fuente canónica entre proveedores.

## Fixtures vs archivos reales
- Pruebas automáticas: usan `data/provider-catalogs/fixtures/**`.
- Archivos reales: mantenerlos en `data/provider-catalogs/raw/**` y tratarlos como insumo opcional local.
- `data/provider-catalogs/raw/**` queda ignorado para nuevos archivos no versionados.

## Cómo correr pruebas
- Reproducible y por defecto:
  - `dotnet test`
- Validación completa local:
  - `dotnet build`
  - `npm run build`
- Si se quieren revisar archivos reales, usarlos manualmente desde `data/provider-catalogs/raw/**`; no forman parte del set mínimo de pruebas del repo.

## Riesgos
- El snapshot está definido por `productId + supplierProfile`; si un mismo proveedor publica dos códigos distintos para el mismo producto, el último importado reemplaza la foto previa.
- La actualización conservadora de `Product` evita pérdida de contexto entre proveedores, pero no resuelve prioridad comercial entre costos/precios de varios proveedores.
- Sigue sin existir matching avanzado por descripción, compatibilidad o equivalencias.

## Decisiones pendientes
- Definir estrategia de prioridad para `CurrentCost` y `CurrentSalePrice` cuando varios proveedores actualizan el mismo producto.
- Evaluar si conviene soportar más de un snapshot por proveedor cuando un mismo producto tenga múltiples códigos activos.
- Incorporar override manual por fila antes del apply.
- Normalizar mejor compatibilidad y familias.
