# 00. Contexto de negocio

## Qué se sabe

El proyecto Refaccionaria CUATE - Inventario Operativo busca reemplazar o formalizar la operación actual basada en Excel para cubrir, al menos, estos procesos:
- control de inventario
- registro de ventas rápidas
- actualización de existencias
- importación de catálogos de proveedores
- generación de reportes

## Alcance de la fase actual

Esta fase no construye todavía la aplicación final.

El alcance real en Paso 1 es:
- ordenar el repositorio base
- inspeccionar el archivo Excel actual
- documentar estructura, hallazgos y riesgos
- generar un borrador de normalización inicial
- dejar claros los huecos antes de definir base de datos final e interfaz

## Qué se infiere

A partir del archivo fuente auditado, parece que la operación actual trabaja con cortes sucesivos del mismo inventario dentro de un mismo mes, usando varias hojas como snapshots o bloques temporales.

También se infiere que la misma hoja mezcla:
- catálogo base de producto
- precio y costo
- existencia consolidada
- movimientos por bloques de días

Eso sugiere un alto acoplamiento entre catálogo, inventario y movimientos.

## Qué falta validar

- si las 4 hojas representan semanas, cortes manuales o versiones del mismo inventario
- si el mes visible en el archivo es un caso aislado o la plantilla operativa general
- si la columna `F` es siempre la existencia final consolidada del corte de cada hoja
- si las columnas posteriores representan entradas y salidas por día o por evento agrupado
- si existe un catálogo maestro separado de proveedores o compras
- si el código de producto actual es realmente único a nivel negocio

## Riesgos de negocio visibles desde esta fase

- dependencia operativa de un Excel manual grande
- mezcla de catálogo y movimientos en una misma estructura
- códigos faltantes o duplicados
- costos y precios potencialmente inconsistentes
- existencias negativas ya presentes en algunos cortes
