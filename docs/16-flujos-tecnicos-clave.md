# 16. Flujos técnicos clave

## 1. Cargar productos demo

1. validar que el entorno sea demo o sandbox
2. leer `data/demo/productos_demo.csv`
3. mapear filas a DTOs de producto
4. crear productos inexistentes o reinicializar dataset demo según estrategia
5. dejar bitácora de seed demo

## 2. Cargar inventario demo

1. tomar `existencia_inicial` de productos demo
2. crear o actualizar `InventarioActual`
3. generar eventos o movimientos de carga demo
4. opcionalmente aplicar `movimientos_demo.csv`
5. confirmar saldos resultantes

## 3. Consultar catálogo

1. frontend envía búsqueda o filtros
2. API consulta productos y estado de revisión
3. backend responde DTO resumido
4. frontend muestra tabla con código, descripción, marca, precio y saldo

## 4. Registrar entrada de inventario

1. usuario captura producto, cantidad y motivo
2. API valida cantidad y existencia del producto
3. backend crea movimiento tipo entrada
4. backend actualiza saldo en `InventarioActual`
5. se devuelve saldo resultante

## 5. Registrar venta rápida

1. frontend envía items de venta
2. API valida producto, precio y stock según regla activa
3. backend abre transacción
4. crea cabecera `Venta`
5. crea `VentaDetalle`
6. crea movimientos tipo venta
7. actualiza `InventarioActual`
8. confirma transacción y responde folio y saldo afectado

## 6. Validar existencia resultante

1. consultar saldo actual del producto
2. consultar últimos movimientos asociados
3. recalcular saldo esperado si aplica
4. comparar saldo persistido contra operación registrada
5. devolver estado consistente o incidencia

## 7. Cargar inventario inicial real desde plantilla

1. usuario admin sube CSV
2. API parsea columnas esperadas
3. genera preview con filas válidas e inválidas
4. usuario confirma aplicación
5. backend crea evento de `CargaInventarioInicial`
6. crea u homologa productos
7. inicializa `InventarioActual`
8. genera movimientos de carga inicial
9. persiste bitácora de ejecución

## 8. Ejecutar reset demo seguro

1. validar rol admin
2. validar entorno demo/sandbox
3. exigir confirmación explícita
4. abrir transacción o rutina segura
5. eliminar o reinicializar ventas demo, movimientos demo, ajustes demo y saldos demo
6. conservar usuarios, configuración y bitácora
7. opcionalmente resembrar demo
8. registrar auditoría del reset
