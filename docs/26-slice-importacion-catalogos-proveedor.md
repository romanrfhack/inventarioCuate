# Slice 26 - Importación de catálogos de proveedor conocidos

## Objetivo
Habilitar importación incremental de catálogos de proveedor conocidos con perfiles específicos, sin tocar inventario local (`InventoryBalance.CurrentStock`).

## Alcance del slice
- Preview y apply controlado para 3 perfiles iniciales: `alessia`, `masuda`, `c-cedis`.
- Persistencia de lote y detalle normalizado.
- Clasificación por fila: `match_codigo`, `producto_nuevo`, `conflicto_codigo`, `dato_incompleto`, `requiere_revision`.
- Actualización controlada de costo, precio y metadatos de catálogo del producto.
- Alta controlada de producto nuevo cuando el match es claro y los datos mínimos son suficientes.

## Regla funcional crítica
Estos archivos **no son inventario local**. La existencia o stock del proveedor se guarda como disponibilidad informativa del proveedor. **Nunca** se actualiza `InventoryBalance.CurrentStock` desde este slice.

## Proveedores soportados

### 1) Alessia
- Archivo real: `data/provider-catalogs/raw/alessia/07 Abril Lista Alessia 26.xlsm`
- Hojas detectadas: `Ale 25`, `st`, `Cn`, `Pr`, `Cd`
- Hoja principal sugerida: `Ale 25`
- Hoja auxiliar útil: `st` para disponibilidad por código
- Columnas detectadas en principal:
  - `Existencia`
  - `Codigo`
  - `MARCA`
  - `U-M`
  - `Piezas Caja`
  - `NOMBRE - REFACCION`
  - `MOTOS COMPATIBLES`
  - `Precio`
  - `Total`
  - `NUEVO`
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
- Qué se importa:
  - código, descripción, marca, unidad, piezas por caja, compatibilidad, precio sugerido, disponibilidad informativa
- Qué no se importa:
  - `Total` como inventario local
  - secciones decorativas o rótulos internos

### 2) Masuda
- Archivo real: `data/provider-catalogs/raw/masuda/LISTA DE PRECIO - MASUDA IMPORTADOR REGIONAL 09-ABRIL.xlsx`
- Hojas detectadas: `COMPRA`, `ACCESORIOS Y REFACCIONES`, `ZONA 17`, etc.
- Hoja principal sugerida: `COMPRA`
- Columnas detectadas:
  - `NO`
  - `CODIGO`
  - `DESCRIPCION`
  - `U.`
  - `IMPORTADOR REGIONAL`
  - `PEDIDO`
  - `IMPORTE TOTAL`
  - `LINEA`
  - `FAMILIA`
  - `SUB-FAMILIA`
  - `PZA X CAJA`
  - `inventario MID 09/04/2026`
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
- Qué se importa:
  - estructura de línea/familia/subfamilia, costo proveedor y disponibilidad textual
- Qué no se importa:
  - `PEDIDO` e `IMPORTE TOTAL` como operación local

### 3) C-CEDIS
- Archivo real: `data/provider-catalogs/raw/c-cedis/ListaPreciosC-CEDIS-05042026.xlsx.xls`
- Formato real detectado: OpenXML, no depender de la extensión `.xls`
- Hojas detectadas: `Hoja1`, `Hoja3`, `Hoja2`
- Hoja principal sugerida: `Hoja1`
- Hoja auxiliar útil: `Hoja3` para interpretación de niveles
- Columnas detectadas:
  - `CODIGO`
  - `PEDIDO`
  - `IMAGEN`
  - `DESCRIPCION`
  - `STOCK`
  - `COMPATIBILIDAD`
  - `DEPARTAMENTO`
  - `SECCION`
  - `MAYOREO`
  - `MAS DE 25 MIL`
  - `MAS DE 50 MIL`
  - y niveles adicionales (`MAS DE 100 MIL`, etc.)
- Mapeo canónico:
  - `CODIGO` -> `codigo`
  - `DESCRIPCION` -> `descripcion`
  - `STOCK` -> `disponibilidad_proveedor`, `stock_proveedor_texto`
  - `COMPATIBILIDAD` -> `compatibilidad`
  - `DEPARTAMENTO` -> `familia`
  - `SECCION` -> `categoria`
  - precios por columnas -> `precio_sugerido`, `precios_por_nivel`
- Qué se importa:
  - código, descripción, compatibilidad, familia/categoría, stock informativo y precios por nivel
- Qué no se importa:
  - imagen y pedido como dato operativo local

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

## Reglas de apply
- `match_codigo`: actualiza costo/precio y campos de catálogo del producto existente.
- `producto_nuevo`: crea producto solo si tiene código, descripción y al menos costo o precio.
- `conflicto_codigo`: no aplica automáticamente.
- `dato_incompleto`: no aplica automáticamente.
- `requiere_revision`: no aplica automáticamente.
- Nunca actualiza `InventoryBalance.CurrentStock`.
- Siempre deja trazabilidad en lote/detalle con origen de hoja y fila.

## Riesgos
- Hojas con encabezados desplazados o renglones decorativos pueden requerir heurística adicional.
- Alessia mezcla disponibilidad entre hoja principal y hoja auxiliar `st`.
- C-CEDIS tiene niveles extras además de los tres iniciales mencionados, conviene mantenerlos en JSON.
- Aún no hay una entidad dedicada de catálogo de proveedor por producto; este slice actualiza campos del producto y conserva el detalle del lote como trazabilidad.

## Pendientes
- Evaluar si conviene una tabla `ProductSupplierCatalogSnapshot` por proveedor/producto.
- Definir prioridad cuando el mismo producto llegue desde múltiples proveedores.
- Incorporar override manual por fila antes del apply.
- Normalizar mejor compatibilidad y familias.
