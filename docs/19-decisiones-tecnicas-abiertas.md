# 19. Decisiones técnicas abiertas

## 1. Decisiones técnicas ya maduras

- usar ASP.NET Core Web API como backend
- usar Angular como frontend
- usar base de datos relacional
- construir por slices verticales incrementales
- modelar inventario actual separado de movimientos
- modelar carga inicial como flujo explícito
- restringir reset a demo/sandbox

## 2. Decisiones técnicas provisionales

- usar EF Core como ORM principal
- usar Angular Material para acelerar UI operativa
- manejar autenticación inicial simple con roles básicos
- usar CSV como formato oficial de carga inicial del MVP
- mantener `Turno` como entidad nullable en ventas y movimientos al inicio

## 3. Decisiones que conviene validar contigo antes del scaffold

- SQL Server o PostgreSQL como motor inicial
- si el scaffold debe incluir autenticación desde el primer commit técnico
- si `Turno` entra ya en el primer scaffold o solo queda preparado
- si la carga inicial debe poder aplicarse solo una vez o varias veces con protección
- si el seed demo debe vivir como endpoint, comando interno o ambos
- si el reset demo debe resembrar automáticamente o solo limpiar

## 4. Riesgo si no se validan antes del scaffold

- bajo a medio en motor de base de datos, porque ambos son viables
- medio en autenticación y turnos, porque cambia estructura inicial del scaffold
- medio a alto en carga inicial y reset demo, porque afectan seguridad operativa y contratos tempranos
