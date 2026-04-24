# 18. Estrategia de pruebas MVP

## Objetivo

Proteger los flujos críticos del MVP desde los primeros slices y asegurar que demo/reset no contaminen datos reales.

## 1. Pruebas unitarias

Cubrir:
- validación de producto
- reglas de inventario no negativo o alertado, según política activa
- cálculo de totales de venta
- validación de carga inicial
- reglas de reset demo por entorno y rol

## 2. Pruebas de integración

Cubrir:
- persistencia de productos
- actualización de inventario tras entrada manual
- creación de venta con descuento de inventario
- cancelación de venta con reversa
- preview y aplicación de carga inicial
- bloqueo de reset fuera de demo

## 3. Pruebas e2e

Cubrir:
- cargar datos demo
- consultar catálogo
- registrar entrada
- registrar venta
- verificar saldo resultante
- ejecutar reset demo seguro
- cargar archivo de inventario inicial y validar resultado

## 4. Flujos críticos

- seed demo
- reset demo
- creación de producto
- entrada de inventario
- venta rápida
- cancelación de venta
- carga inicial por plantilla
- consulta de inventario actual

## 5. Qué debe quedar protegido desde el primer slice

- no mezclar demo con real
- no permitir reset fuera de entorno autorizado
- no permitir ventas inconsistentes sin error explícito
- no aceptar cargas iniciales con plantilla inválida
- no perder trazabilidad de movimientos

## 6. Cómo validar demo/reset sin contaminar datos reales

- usar configuración de entorno explícita, por ejemplo `IsDemoEnvironment=true`
- usar base de datos separada o dataset aislado en pruebas
- agregar pruebas automáticas que fallen si reset se habilita en entorno no demo
- registrar toda ejecución de seed y reset
- evitar reusar credenciales o conexiones productivas en suites demo/e2e
