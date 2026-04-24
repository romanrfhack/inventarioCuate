# 22. Verificación técnica baseline

## Estado

Documento de verificación técnica del baseline ejecutable del scaffold.

## Nota previa

Antes de cerrar esta verificación se cambió el baseline técnico del backend a .NET 10.

## Entorno encontrado

- sistema operativo: Ubuntu 24.04.4 LTS
- Node.js disponible
- npm disponible
- `dotnet` no instalado al momento de esta revisión
- `docker` no instalado al momento de esta revisión

## Dependencias faltantes detectadas

- `dotnet-sdk-10.0`
- tooling de EF Core para .NET 10 (`dotnet-ef`)
- motor de contenedores para correr SQL Server demo (`docker.io`)
- plugin/soporte de compose para levantar `docker-compose.demo.yml`

## Dependencias instaladas

Pendiente en este punto de la verificación. No se ejecutó instalación todavía porque primero se alineó el scaffold a .NET 10.

## Ajustes realizados al scaffold

- `app/src/Api/RefaccionariaCuate.Api.csproj` -> `net10.0`
- `app/src/Application/RefaccionariaCuate.Application.csproj` -> `net10.0`
- `app/src/Domain/RefaccionariaCuate.Domain.csproj` -> `net10.0`
- `app/src/Infrastructure/RefaccionariaCuate.Infrastructure.csproj` -> `net10.0`
- paquetes EF Core actualizados a `10.0.0`
- `Microsoft.AspNetCore.Authentication.JwtBearer` actualizado a `10.0.0`
- `Microsoft.Extensions.Options.ConfigurationExtensions` actualizado a `10.0.0`
- documentación del scaffold actualizada a .NET 10

## Disponibilidad de SDK en este host

`apt-cache policy` confirma disponibilidad de:
- `dotnet-sdk-10.0` candidato `10.0.107-0ubuntu1~24.04.1`

## Impacto en paquetes o tooling

- EF Core debe usarse en versión 10.x para alinear runtime y tooling
- la generación de migraciones debe ejecutarse con `dotnet-ef` compatible con SDK 10
- no se detectó necesidad de cambiar Angular o frontend por este ajuste

## Comandos ejecutados hasta ahora

- inspección de estructura del repo
- inspección de disponibilidad de `dotnet-sdk-10.0`
- revisión de referencias `net8.0` / paquetes 8.x
- actualización de proyectos a `net10.0`
- actualización documental base

## Resultado de build backend

Pendiente, bloqueado por falta de instalación de `dotnet-sdk-10.0`.

## Resultado de build frontend

Ya estaba verificado previamente como exitoso. No fue afectado por el cambio a .NET 10.

## Resultado de migración inicial

Pendiente, bloqueado por falta de `dotnet-sdk-10.0` y `dotnet-ef`.

## Resultado de arranque API

Pendiente, bloqueado por falta de `dotnet-sdk-10.0`.

## Resultado de arranque BD demo

Pendiente, bloqueado por falta de `docker` y compose.

## Resultado de prueba mínima de autenticación

Pendiente, depende de arranque API + BD.

## Resultado de prueba mínima de seed/reset demo

Pendiente, depende de arranque API + BD.

## Bloqueadores reales remanentes

### 1. `dotnet` no instalado
- error exacto: `dotnet: command not found`
- causa probable: SDK no instalado en el host
- impacto: no se puede restaurar, compilar, correr API ni generar migraciones
- siguiente acción mínima: instalar `dotnet-sdk-10.0`

### 2. `docker` no instalado
- error exacto: `docker: command not found`
- causa probable: motor de contenedores no instalado en el host
- impacto: no se puede levantar SQL Server demo con el compose del proyecto
- siguiente acción mínima: instalar `docker.io` y soporte compose

## Comando mínimo propuesto para destrabar la verificación

```bash
apt-get update && apt-get install -y dotnet-sdk-10.0 dotnet-ef docker.io docker-compose-v2
```

## Nota sobre el comando

- si `docker-compose-v2` no existiera con ese nombre en este host, debe sustituirse por el paquete disponible equivalente sin agregar software innecesario
- el objetivo sigue siendo instalar lo mínimo para:
  - compilar backend
  - generar migración inicial
  - correr API
  - levantar SQL Server demo
