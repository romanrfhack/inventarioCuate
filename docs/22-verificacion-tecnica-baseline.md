# 22. Verificación técnica baseline

## Estado

Cierre técnico del baseline ejecutable del scaffold del proyecto.

## Fuente de verdad usada para este cierre

Además de la inspección local del repositorio, este cierre incorpora como evidencia válida el estado manual ya confirmado en el VPS.

## Entorno encontrado

- sistema operativo: Ubuntu 24.04.4 LTS
- Node.js y npm operativos
- .NET 10 SDK instalado y operativo
- `dotnet-ef` instalado y operativo
- Docker y Docker Compose instalados y operativos
- `global.json` alineado a `10.0.107`

## Dependencias faltantes detectadas

No quedaron dependencias técnicas faltantes para compilar y ejecutar el baseline del scaffold.

## Dependencias instaladas

Instaladas previamente en el VPS y validadas manualmente:
- .NET 10 SDK
- dotnet-ef
- Docker
- Docker Compose

## Ajustes realizados al scaffold

- backend alineado a `.NET 10` (`net10.0`)
- EF Core alineado a `10.0.0`
- documentación del scaffold actualizada a .NET 10
- precisión explícita agregada para:
  - `Product.CurrentCost`
  - `Product.CurrentSalePrice`
- flujo `initial-load/apply` endurecido para exigir preview fresco y evitar reaplicación ambigua sobre estados no válidos
- token de confirmación de preview cambiado a token aleatorio por request en vez de valor fijo

## Comandos ejecutados

### Verificados manualmente en el VPS
- build de la solución
- generación/aplicación de migración inicial
- arranque de SQL Server demo
- arranque de la API en entorno Demo
- login con `admin.demo`
- llamada a `demo-admin/status`
- llamada a `demo-admin/seed`
- validación de protección de `demo-admin/reset`
- ejecución de reset real
- validación posterior de status con `productCount = 0`
- seed posterior con `productCount = 3`

### Inspección y ajustes complementarios en este cierre
- revisión de proyectos y referencias a .NET 10
- revisión del `DbContext`
- revisión del flujo `initial-load/preview` y `initial-load/apply`
- endurecimiento mínimo del flujo de apply
- actualización documental de cierre técnico

## Resultado de build backend

Validado como exitoso manualmente en el VPS.

## Resultado de build frontend

Validado como exitoso.

## Resultado de migración inicial

Validado como exitoso.

## Resultado de arranque API

Validado como exitoso en:
- `http://localhost:5098`

## Resultado de arranque BD demo

Validado como exitoso.

## Resultado de prueba mínima de autenticación

Validado como exitoso.

Evidencia funcional confirmada:
- login con `admin.demo` respondió correctamente
- la configuración mínima de autenticación quedó operativa

## Resultado de prueba mínima de seed/reset demo

Validado como exitoso.

Evidencia confirmada:
- `demo-admin/status` respondió
- `demo-admin/seed` respondió
- protección de reset validada
- reset real validado
- status posterior al reset dejó `productCount = 0`
- seed posterior volvió a dejar `productCount = 3`

## Qué quedó validado realmente

- scaffold backend compila
- scaffold frontend compila
- SQL Server demo arranca
- la API arranca en entorno Demo
- autenticación mínima funciona
- seed demo funciona
- reset demo protegido funciona
- reset demo real funciona
- migración inicial existe y aplica correctamente
- el baseline técnico ya es ejecutable y verificable

## Qué sigue pendiente

### Pendiente principal
- validar de forma limpia el happy path de `initial-load/apply` con un preview fresco completo y evidencia final de punta a punta

### Alcance del pendiente
- no bloquea el baseline ejecutable del scaffold
- sí conviene cerrarlo antes de entrar fuerte al slice funcional de carga inicial real

## Warnings no bloqueantes

### 1. Vulnerabilidad reportada en `System.Security.Cryptography.Xml`

Estado:
- warning no bloqueante

Hallazgo:
- la dependencia aparece de forma transitiva en restore/assets
- no se detectó uso directo de APIs XML criptográficas en el código del proyecto
- la fuente probable es la cadena de dependencias de autenticación/JWT o librerías relacionadas del ecosistema Microsoft

Recomendación mínima:
- revisar con `dotnet list package --include-transitive --vulnerable` en el VPS para confirmar el árbol exacto
- si existe versión transitiva superior segura compatible, actualizarla
- si no bloquea el runtime real ni hay explotación aplicable al uso actual, tratarlo como hardening corto posterior, no como bloqueo del baseline

### 2. Precisión decimal no explícita en `CurrentCost` y `CurrentSalePrice`

Estado:
- warning atendido en este cierre

Acción tomada:
- se agregó configuración explícita con `HasPrecision(18, 2)` en `ApplicationDbContext`

## Riesgos remanentes

- el flujo de carga inicial todavía está en modo scaffold técnico, no en parser real de negocio
- `initial-load/apply` ya está mejor protegido, pero aún no ejecuta una aplicación transaccional completa de inventario real
- el warning transitorio de `System.Security.Cryptography.Xml` debe revisarse como parte de endurecimiento de dependencias
- reset demo funciona, pero requiere seguir cubriéndose con pruebas para evitar regresiones de seguridad

## Ajuste mínimo aplicado al flujo initial-load/apply

Se hizo un endurecimiento mínimo para que el flujo sea comprobable de manera más limpia:
- `preview` genera token aleatorio por solicitud
- `apply` solo acepta cargas en estado `previewed`
- si la carga ya cambió de estado, devuelve conflicto y obliga a generar un preview fresco

Impacto:
- evita reaplicaciones ambiguas sobre previews viejos o ya mutados
- no introduce todavía lógica de negocio pesada

## Bloqueadores reales remanentes

No hay bloqueadores técnicos fuertes para empezar implementación funcional.

## Conclusión

El Paso 3C puede considerarse cerrado con baseline ejecutable y verificable.

El único pendiente relevante antes de profundizar en carga inicial real es terminar la validación limpia del happy path de `initial-load/apply` sobre un preview fresco de punta a punta.
