# 06. Reglas provisionales de migración

## Propósito

Estas reglas no sustituyen validaciones de negocio. Sirven para poder seguir diseñando el sistema y preparar migración sin bloquear el trabajo por decisiones todavía abiertas.

## Reglas provisionales

### 1. Regla provisional de unicidad de producto

- si existe `codigo`, el producto se identifica provisionalmente por combinación `codigo + descripcion`
- si un mismo `codigo` aparece varias veces, no se colapsa automáticamente
- los casos duplicados quedan marcados para revisión funcional

### 2. Regla provisional para productos sin código

- todo producto sin código conserva un `producto_id_temporal`
- no se asume equivalencia automática entre descripciones parecidas
- quedan marcados como candidatos a homologación manual

### 3. Regla provisional para costo faltante

- el producto se migra sin costo definitivo
- queda marcado con revisión obligatoria
- no debe usarse ese costo faltante como base de margen o valuación confiable

### 4. Regla provisional para precio faltante

- el producto se migra sin precio definitivo
- queda marcado con revisión obligatoria
- el diseño funcional debe contemplar restricción o advertencia antes de venta

### 5. Regla provisional para existencia negativa

- la existencia negativa se conserva tal como está en el borrador de migración
- se marca como incidencia crítica
- no se corrige automáticamente sin decisión operativa

### 6. Regla provisional para costo mayor o igual al precio

- el registro se conserva
- se marca con bandera de revisión
- no se asume que el costo sea correcto ni que el precio esté validado

### 7. Regla provisional para proveedor desconocido

- `proveedor` queda vacío o como no identificado en artefactos borrador
- no se inventa proveedor a partir de marca u otros campos ambiguos
- la relación producto-proveedor queda pendiente de fuente confiable adicional

### 8. Regla provisional para hoja base de inventario inicial

- se propone usar `Hoja 4` como base provisional de inventario inicial
- esta decisión es operativa y temporal hasta validación del cliente
- no debe tratarse todavía como verdad definitiva del proyecto

## Qué está confirmado

- estas reglas permiten avanzar en diseño preliminar sin alterar el archivo fuente
- los artefactos actuales ya soportan este enfoque de revisión por bandera

## Qué sigue siendo inferencia

- la clave natural final del producto
- la semántica oficial completa de movimientos
- la hoja definitiva de arranque
- la política de saneamiento previa a migración

## Qué propongo asumir provisionalmente

- continuar análisis funcional con estas reglas
- no automatizar fusiones ni limpiezas agresivas todavía
- validar contigo las decisiones críticas antes de pasar a modelo final y reglas de operación cerradas
