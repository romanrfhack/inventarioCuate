# 10. Cambio de baseline de inventario inicial

## Propósito

Documentar el cambio de decisión de negocio respecto al arranque del inventario del sistema.

## Qué cambia respecto a la interpretación anterior

### Antes
- se consideraba provisionalmente que una hoja del Excel, especialmente `Hoja 4`, podía servir como snapshot de inventario inicial
- la columna `F` se trataba como candidata a existencia consolidada útil para arranque

### Ahora
- el Excel actual ya no se usará como inventario inicial oficial
- el sistema arrancará con existencias en cero o con una carga explícita desde una plantilla nueva y controlada
- el Excel se conserva como referencia histórica, referencia de catálogo, fuente demo y apoyo analítico, pero no como saldo vigente confiable

## Por qué ya no se usará el Excel como inventario inicial oficial

### Confirmado por decisión de negocio
- el Excel actual no representa una base suficientemente confiable para establecer el inventario vigente del sistema
- el arranque debe apoyarse en una carga explícita, controlada y validable por el cliente

### Implicación práctica
- el MVP deja de depender de una interpretación ambigua del Excel para poder nacer
- se reduce el riesgo de arrancar con saldos incorrectos
- se habilita una demo operativa con datos pequeños y controlados antes de pedir el inventario real

## Qué implicaciones tiene en modelo funcional y migración

### En modelo funcional
- `InventarioActual` ya no depende del Excel como fuente de corte inicial
- se vuelve central la capacidad de carga explícita de inventario inicial
- el flujo de operación temprana se centra en catálogo, carga de inventario, movimientos, ventas y validación de saldos
- la función de reset demo se vuelve relevante para pruebas controladas

### En migración
- ya no hay migración directa de existencias desde el Excel actual
- sí puede haber reutilización parcial del Excel como fuente de catálogo, datos demo o apoyo para homologación futura
- la carga real de inventario deberá pedirse en una plantilla formal nueva

## Qué partes del análisis anterior siguen sirviendo

- estructura general del catálogo observado en el Excel
- detección de datos faltantes, duplicados y anomalías
- necesidad de separar catálogo, inventario y movimientos
- necesidad de manejar revisión de datos incompletos
- utilidad del Excel como referencia histórica y de catálogo
- detección de que proveedor no está claramente resuelto en la fuente actual

## Qué partes dejan de ser decisiones críticas para el arranque

- usar `Hoja 4` como baseline de arranque
- tomar `F` como existencia inicial confiable del MVP
- interpretar con precisión total las columnas posteriores a `F` para arrancar la operación temprana

Estas decisiones siguen siendo útiles si más adelante se quiere explotar mejor la referencia histórica, pero ya no bloquean diseño ni construcción del MVP temprano.

## Qué queda confirmado

- inventario inicial del sistema: cero o carga explícita
- Excel actual: referencia, no baseline operativo
- el MVP temprano debe soportar demo controlada e importación de inventario inicial formal

## Qué cambia por decisión de negocio

- desaparece la dependencia del Excel como snapshot vigente
- cambia la prioridad de validaciones funcionales
- el foco pasa a plantilla de carga, datos demo y flujo controlado de arranque

## Qué sigue provisional

- columnas finales exactas de la plantilla si aparecen necesidades nuevas del cliente
- nivel de automatización de la carga inicial
- alcance exacto del reset en ambientes no demo
