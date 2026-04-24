# 14. Módulos técnicos y slices MVP

## Slice 1. Catálogo y datos demo

### Objetivo
Levantar el catálogo base y permitir una demo operativa mínima con productos controlados.

### Alcance
- entidad Producto
- consulta de catálogo
- carga de `productos_demo.csv`
- visualización de productos con saldo inicial demo

### Dependencias
- modelo de producto
- persistencia básica
- endpoint de carga demo o semilla interna

### Criterios de aceptación
- se pueden cargar 5 productos demo
- catálogo visible por API y frontend
- productos consultables por código y descripción

### Riesgos
- mezclar datos demo con datos reales
- asumir unicidad rígida de código demasiado pronto

## Slice 2. Inventario actual y movimientos

### Objetivo
Tener saldo vigente por producto y trazabilidad de entradas, salidas y ajustes.

### Alcance
- entidad InventarioActual
- entidad MovimientoInventario
- registrar entrada manual
- registrar ajuste manual
- consultar saldo y movimientos

### Dependencias
- catálogo base
- reglas de validación de saldo

### Criterios de aceptación
- una entrada incrementa saldo
- un ajuste modifica saldo con traza
- se puede consultar saldo y movimientos por producto

### Riesgos
- inconsistencias de concurrencia si no se controla actualización de saldo

## Slice 3. Venta rápida

### Objetivo
Registrar ventas simples y descontar inventario en tiempo real.

### Alcance
- entidad Venta
- entidad VentaDetalle
- validación de disponibilidad
- descuento automático de inventario
- reversa por cancelación

### Dependencias
- catálogo
- inventario actual
- movimientos

### Criterios de aceptación
- venta exitosa genera cabecera, detalle y movimiento
- saldo disminuye correctamente
- cancelación revierte saldo y deja trazabilidad

### Riesgos
- vender productos sin precio o con saldo inválido si no se valida correctamente

## Slice 4. Carga inicial por plantilla

### Objetivo
Permitir carga formal de inventario inicial real desde CSV.

### Alcance
- parsing de `inventario_inicial_template.csv`
- preview de validaciones
- aplicación controlada de carga inicial
- creación u homologación básica de productos

### Dependencias
- catálogo
- inventario
- movimientos

### Criterios de aceptación
- se puede subir archivo válido
- el sistema reporta errores de filas inválidas
- el sistema aplica carga y deja saldos correctos

### Riesgos
- duplicados o sobreescritura accidental si no se protege bien la carga inicial

## Slice 5. Reset demo seguro

### Objetivo
Permitir volver a un estado demo limpio sin tocar datos reales.

### Alcance
- endpoint o comando de reset demo
- restricción por entorno demo/sandbox
- auditoría del reset
- recarga de semilla demo opcional

### Dependencias
- separación clara entre modo demo y modo real
- bitácora mínima

### Criterios de aceptación
- solo admin puede ejecutar reset
- reset no funciona fuera de demo
- los datos demo quedan reiniciados limpiamente

### Riesgos
- daño real si se implementa sin protección por entorno

## Slice 6. Reportes mínimos

### Objetivo
Dar visibilidad operativa básica del MVP.

### Alcance
- inventario actual
- movimientos recientes
- ventas por periodo
- productos con datos incompletos o anómalos

### Dependencias
- slices 1 a 4

### Criterios de aceptación
- reportes consultables por API
- frontend puede listar información clave

### Riesgos
- mezclar lógica de lectura agregada con transacciones si no se separa bien
