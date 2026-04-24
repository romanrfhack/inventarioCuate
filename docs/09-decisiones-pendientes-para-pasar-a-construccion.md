# 09. Decisiones pendientes para pasar a construcción

## Propósito

Separar qué ya está suficientemente maduro para pasar a diseño técnico del MVP y qué sigue necesitando validación funcional antes de construir.

## 1. Decisiones ya suficientemente maduras para avanzar

- separar catálogo de productos, inventario actual y movimientos
- manejar ventas como cabecera más detalle
- registrar toda afectación de inventario mediante movimientos
- conservar productos incompletos con banderas de revisión en lugar de descartarlos
- no fusionar duplicados automáticamente
- manejar proveedor como entidad independiente del producto
- registrar importaciones por lote con detalle y conflictos
- exigir usuario para ventas, ajustes e importaciones

### Riesgo de construir sin validar más
- bajo a medio
- estas decisiones están suficientemente soportadas por el contexto del proyecto y por la auditoría del Excel

## 2. Decisiones que pueden quedar provisionales

- tratar `C` como marca provisional
- permitir producto sin código con identificador interno y revisión obligatoria
- usar `codigo + descripcion` como criterio operativo provisional para distinguir productos
- dejar `turno` como entidad prevista aunque su obligatoriedad pueda aplazarse
- bloquear por defecto venta de productos sin precio, con posibilidad de excepción futura
- permitir que el sistema arranque en cero si todavía no se ha cargado inventario inicial real

### Riesgo de construir sin validar más
- medio
- permiten avanzar, pero podrían requerir ajustes de reglas o migración si el cliente define otra semántica

## 3. Decisiones que debes validarme tú antes de construir

### 3.1 Regla real de unicidad de producto
- si el `codigo` debe ser único, si admite duplicados válidos o si hay códigos genéricos permitidos

Riesgo de construir sin validar:
- alto, porque impacta catálogo, matching de importación y venta rápida

### 3.2 Tratamiento definitivo de productos sin código
- si pueden venderse así, si deben homologarse antes o si el sistema asignará códigos internos visibles

Riesgo de construir sin validar:
- medio a alto, porque cambia flujo de alta, búsqueda y control operativo

### 3.3 Tratamiento definitivo de productos sin costo o sin precio
- si se bloquean, si admiten excepción o si pueden quedar activos parcialmente

Riesgo de construir sin validar:
- alto, porque afecta ventas, utilidad y experiencia operativa

### 3.4 Fuente real del proveedor
- de dónde se tomará el proveedor y si habrá múltiples proveedores por producto desde inicio

Riesgo de construir sin validar:
- medio, porque afecta importaciones y modelo comercial

### 3.5 Alcance real de turnos
- si turno entra en MVP estricto o en una iteración inmediata posterior

Riesgo de construir sin validar:
- medio, porque cambia cabeceras, trazabilidad y algunos reportes

## 4. Recomendación para pasar a construcción

### Ya se puede avanzar a diseño técnico del MVP
Sí, pero bajo una condición importante:
- tratar estas decisiones pendientes como checklist de validación funcional corta, no como discusión abierta indefinida

### Qué conviene validar antes
- unicidad real del código
- política de venta para productos sin precio
- alcance de turnos en MVP
- reglas de uso de la carga inicial formal
- alcance exacto del reset demo

### Riesgo de saltarse esta mini validación
- construir bien técnicamente, pero con reglas operativas ambiguas en alta de inventario inicial, excepciones de venta y operación demo
