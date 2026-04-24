# Refaccionaria CUATE - Inventario Operativo

## Estado actual

Este repositorio ya incluye el scaffold técnico base del MVP.

Estado del trabajo:
- documentación funcional y técnica base cerrada
- scaffold real de backend ASP.NET Core Web API + frontend Angular
- configuración demo con SQL Server, autenticación mínima, seed demo y reset auditado
- slices de negocio todavía pendientes de implementación completa

## Estructura

- `docs/`: documentación funcional y técnica inicial
- `data/raw/`: insumos fuente sin alterar
- `data/processed/`: salidas de análisis y archivos derivados provisionales
- `scripts/inspect_excel/`: scripts de inspección del Excel actual
- `app/src/`: solución backend .NET por capas ligeras (`Api`, `Application`, `Domain`, `Infrastructure`)
- `app/frontend/`: aplicación Angular base navegable
- `app/docker-compose.demo.yml`: SQL Server demo local

## Fuente auditada

- Excel fuente: `data/raw/CUATE NEXT.xlsm`

## Artefactos generados en esta fase

- `docs/00-contexto-negocio.md`
- `docs/01-auditoria-excel.md`
- `docs/02-requerimientos-mvp.md`
- `docs/03-modelo-datos-borrador.md`
- `docs/04-plan-desarrollo.md`
- `data/processed/inventario_normalizado_borrador.csv`
- `data/processed/movimientos_detectados_borrador.csv`
- `scripts/inspect_excel/inspect_excel.py`

## Notas importantes

- La auditoría parte de evidencia real del archivo `.xlsm`.
- Cuando hay inferencias, se documentan explícitamente como inferencias.
- El stack del scaffold quedó fijado en ASP.NET Core Web API + Angular + SQL Server para el MVP.
- Revisa `docs/21-scaffold-tecnico-paso-3b.md` para detalle técnico, ejecución y límites conscientes del scaffold.

## Ejecución del script de inspección

```bash
python3 scripts/inspect_excel/inspect_excel.py
```

El script no depende de `pandas` ni de `openpyxl`; inspecciona el `.xlsm` directamente como paquete OpenXML.
