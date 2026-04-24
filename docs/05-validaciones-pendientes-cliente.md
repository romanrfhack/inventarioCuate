# 05. Validaciones pendientes con cliente

## Propósito

Esta matriz concentra las decisiones mínimas que conviene validar antes de pasar al Paso 2. La idea es no bloquear diseño funcional, pero tampoco cerrar supuestos críticos sin confirmación.

| Duda | Por qué importa | Impacto si no se valida | Valor provisional sugerido | Decisión requerida del cliente |
|---|---|---|---|---|
| Si `Hoja 4` será el inventario inicial oficial | Define el snapshot base para arranque del sistema | Se puede arrancar con existencias equivocadas | Tomar `Hoja 4` como corte provisional | Confirmar si `Hoja 4` es el corte vigente oficial |
| Si `F` es existencia final confiable | Afecta inventario actual, migración y conciliación | Se podría cargar un saldo incorrecto por producto | Tratar `F` como existencia final provisional | Confirmar semántica exacta de la columna `F` |
| Si las columnas posteriores a `F` alternan entrada/salida | Afecta interpretación de movimientos históricos | El historial provisional de movimientos puede quedar mal clasificado | Mantener alternancia entrada/salida como inferencia de trabajo | Confirmar la lógica real de esas columnas |
| Si `C` es marca, proveedor o ambas cosas | Impacta catálogo, modelo de producto y relación con proveedor | Se mezclarían entidades distintas en el diseño | Tratar `C` como marca provisional | Confirmar si `C` representa solo marca o mezcla conceptos |
| Si los códigos duplicados son válidos o error | Define unicidad de producto y estrategia de migración | No se puede cerrar clave natural ni reglas de importación | Considerar duplicado como caso en revisión, no como válido por defecto | Confirmar si hay códigos genéricos permitidos |
| Cómo tratar productos sin código | Afecta alta de producto, venta rápida e importaciones | Quedan productos no conciliables o difíciles de vender/controlar | Permitir migración con ID temporal y revisión obligatoria | Confirmar si se asignarán códigos antes o después de migrar |
| Cómo tratar productos sin costo o sin precio | Afecta margen, reportes y posibilidad de venta | El sistema podría vender o valorar inventario con datos incompletos | Migrar marcados para revisión y restringir reglas posteriores | Confirmar política operativa para captura obligatoria |
| Cómo tratar existencia negativa | Afecta confiabilidad del inventario inicial | Se arrastra inconsistencia al nuevo sistema | Migrar con bandera crítica y sin corregir automáticamente | Confirmar si se ajusta antes de arranque o durante saneamiento |
| Cómo identificar proveedor real | Afecta catálogo, importación y compras futuras | No se puede modelar bien relación producto-proveedor | Dejar `proveedor` como desconocido temporal | Confirmar fuente real del dato de proveedor |

## Qué está confirmado

- estas dudas impactan directamente diseño funcional y migración
- todavía no todas pueden cerrarse solo con evidencia del Excel

## Qué sigue siendo inferencia

- `Hoja 4` como hoja base oficial
- `F` como existencia final confiable
- alternancia exacta entrada/salida en columnas posteriores
- `C` como marca en el 100 por ciento de los casos

## Qué propongo asumir provisionalmente

- usar esta matriz como checklist de validación breve contigo antes de definir modelo final
- seguir diseño preliminar con valores provisionales documentados, no definitivos
