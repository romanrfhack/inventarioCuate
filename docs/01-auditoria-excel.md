# 01. Auditoría del Excel actual

## Resumen ejecutivo

Se auditó el archivo `data/raw/CUATE NEXT.xlsm` directamente sobre su estructura OpenXML, sin modificar el insumo fuente.

Hallazgos principales:
- el archivo contiene 4 hojas operativas con estructura muy similar
- cada hoja parece representar un corte o snapshot sucesivo del inventario durante diciembre
- la estructura mezcla catálogo de productos, precios, existencia consolidada y bloques de movimientos en la misma hoja
- existen productos sin código, sin costo y sin precio de venta
- existen códigos duplicados dentro de una misma hoja
- existen existencias negativas en algunos cortes
- existen registros donde el costo es mayor o igual al precio de venta, lo que sugiere errores de captura o semántica por validar

Se generaron dos salidas borrador:
- `data/processed/inventario_normalizado_borrador.csv`
- `data/processed/movimientos_detectados_borrador.csv`

## Método de inspección

### Qué se sabe

- el archivo es un `.xlsm` válido y accesible
- no fue necesario instalar `openpyxl` ni `pandas`
- se construyó un script en `scripts/inspect_excel/inspect_excel.py` para inspección reproducible
- la extracción se hizo leyendo el paquete OpenXML del archivo

### Qué falta validar

- macros, formato visual y fórmulas complejas no se evaluaron con un motor de Excel
- no se verificó el comportamiento de VBA ni eventos del libro
- no se confirmó si todas las columnas visibles en Excel coinciden semánticamente con los encabezados inferidos desde XML

## Estructura detectada del archivo

### Hojas detectadas

1. `Hoja 1 `
2. `Hoja 2`
3. `Hoja 3`
4. `Hoja 4`

### Interpretación preliminar de las hojas

#### Qué se sabe

En las 4 hojas se observan:
- columnas base de producto en `A:E`
- una columna de total consolidado en `F`
- columnas posteriores con bloques numéricos por día o corte parcial
- encabezado del mes `DICIEMBRE`

#### Qué se infiere

Las hojas parecen cortes consecutivos del mismo inventario dentro de diciembre:
- `Hoja 1 `: días 23 a 28 y 1
- `Hoja 2`: días 2 a 8
- `Hoja 3`: días 9 a 15
- `Hoja 4`: días 16 a 22

Esto sugiere que `Hoja 4` es el corte más reciente dentro del archivo actual, aunque esto debe validarse con el cliente porque la numeración de hoja no garantiza cronología oficial del proceso.

## Mapeo preliminar de columnas

### Qué se sabe

Columnas base observadas con evidencia consistente:
- `A`: descripción
- `B`: código
- `C`: marca
- `D`: costo
- `E`: precio de venta
- `F`: total del corte o existencia actual consolidada de la hoja

### Qué se infiere

- `F` funciona como existencia consolidada del corte visible en esa hoja
- las columnas posteriores a `F` representan movimientos por bloque
- en cada par de columnas se alterna una semántica tipo entrada/salida
- el encabezado visible en fila 1 parece usar el número de día para esos bloques

### Qué falta validar

- si `D` es costo unitario vigente o costo histórico
- si `E` es precio único o precio sugerido
- si `F` es existencia inicial, final o total recalculado del bloque
- si la alternancia de columnas posteriores corresponde exactamente a entrada/salida o a otra lógica operativa

## Métricas por hoja

| Hoja | Productos detectados | Con código | Sin código | Sin costo | Sin precio venta | Códigos duplicados | Existencia negativa | Costo >= precio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Hoja 1 | 8164 | 7820 | 344 | 416 | 311 | 53 | 1 | 16 |
| Hoja 2 | 8180 | 7837 | 343 | 413 | 310 | 53 | 1 | 16 |
| Hoja 3 | 8184 | 7840 | 344 | 413 | 310 | 53 | 1 | 16 |
| Hoja 4 | 8127 | 7783 | 344 | 418 | 313 | 52 | 2 | 15 |

## Evidencia de productos y datos

### Productos sin código

Patrón observado:
- existen múltiples productos con descripción y precio/costo, pero sin código
- ejemplo observado: `ASIENTO DE 70`, `ACEITE ACEDELCO SAE 5W30`, `AKRON 80W90`

Riesgo:
- dificulta unicidad, búsqueda rápida, importación y conciliación futura

### Productos sin costo

Patrón observado:
- algunos productos sí tienen precio de venta pero no costo
- ejemplo observado: código `A12073`, descripción `ADITIVO PARA GASOLINA TOPOI`

Riesgo:
- impide calcular margen y afecta reportes de utilidad

### Productos sin precio de venta

Se detectan registros sin valor en columna `E`.

Riesgo:
- impide venta consistente o requiere reglas operativas manuales fuera del sistema

### Códigos duplicados

Códigos con recurrencia interna observada por hoja, ejemplo:
- `TR` aparece 4 veces
- `COMPLETO` aparece 3 veces
- `CORPLLVG-046` aparece 3 veces

Interpretación:
- pueden ser duplicados reales
- o claves genéricas reutilizadas para variantes distintas

Debe validarse antes de asumir unicidad de `codigo` en el modelo final.

### Existencias negativas

Casos observados:
- `BALATAS DELANTERAS DE RC150` con código `F14020122` aparece con existencia `-1` en algunos cortes
- en `Hoja 4` también aparece `TAPON DE CARTER FZ16` con código `TACN17` y existencia `-1`

Riesgo:
- inconsistencia operativa o venta sin existencia

### Costo mayor o igual al precio de venta

Casos observados:
- `71-097`, `BALATAS DE KEEWAY DELANTERAS`, costo `73517`, precio `130`
- `026RTE`, `BUJES DE EJE DE ARRANQUE DE MOTONETA CHICO`, costo `649`, precio `75`
- `CDD-013`, `CADENA DE DISTRIBUCION PARA ATV-180`, costo `9162`, precio `110`
- `CN-BP-NS200-RR`, costo `0`, precio `0`

Interpretación:
- muy probablemente hay errores de captura, semántica distinta del costo, o valores contaminados

## Hoja o corte más reciente

### Qué se sabe

Por encabezados visibles, las hojas cubren estos bloques:
- Hoja 1: 23, 24, 25, 26, 27, 28, 1
- Hoja 2: 2, 3, 4, 5, 6, 7, 8
- Hoja 3: 9, 10, 11, 12, 13, 14, 15
- Hoja 4: 16, 17, 18, 19, 20, 21, 22

### Recomendación preliminar

Usar `Hoja 4` como mejor candidata a inventario inicial del sistema, porque parece ser el corte más reciente representado dentro del archivo.

### Advertencia

Esto sigue siendo una inferencia operativa. Debe validarse explícitamente con el cliente antes de fijarlo como snapshot oficial de arranque.

## Problemas de datos detectados

- mezcla de catálogo y movimientos en una sola tabla
- encabezados poco normalizados
- posible dependencia de semántica visual del Excel
- productos sin código
- productos sin costo
- productos sin precio
- códigos duplicados
- existencias negativas
- márgenes imposibles o sospechosos
- ausencia visible de proveedor a nivel fila en la estructura principal auditada

## Suposiciones hechas

- se tomó `A:E` como columnas maestras de producto por repetición consistente en las 4 hojas
- se tomó `F` como existencia consolidada del corte de cada hoja
- se asumió que columnas posteriores con números distintos de cero representan movimientos detectables
- se generó `movimientos_detectados_borrador.csv` con una inferencia de tipo `entrada/salida` basada en alternancia de columnas después de `F`
- se dejó `proveedor` vacío en el inventario normalizado porque no hay evidencia clara y consistente en la estructura auditada

## Preguntas pendientes para validar con el cliente

1. ¿Las 4 hojas representan semanas, cortes manuales o versiones sucesivas del mismo archivo?
2. ¿`Hoja 4` debe considerarse el inventario vigente de arranque?
3. ¿La columna `F` es existencia final, existencia inicial ajustada o total acumulado?
4. ¿Las columnas posteriores a `F` sí corresponden a entradas y salidas alternadas por día?
5. ¿El código de producto debe ser único o hoy existen claves genéricas permitidas?
6. ¿Dónde vive realmente el proveedor, si no está en la hoja principal?
7. ¿Los valores extremos de costo son errores de captura o responden a otra regla de negocio?
8. ¿Los productos sin código deben conservarse temporalmente o depurarse antes de migrar?

## Bloqueadores técnicos detectados

### Qué se sabe

- `openpyxl` y `pandas` no estaban instalados en el entorno
- aun así, el archivo pudo inspeccionarse sin bloqueo usando lectura directa del paquete OpenXML

### Impacto

No hubo bloqueo para la auditoría estructural inicial.

### Límite actual

Si después se requiere análisis de fórmulas evaluadas, formatos complejos, nombres definidos o VBA, convendrá abrir el libro con tooling especializado o con Excel real en una etapa posterior.
