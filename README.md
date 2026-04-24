# Refaccionaria CUATE - Inventario Operativo

## Estado actual

Este repositorio corresponde a la fase 1 del proyecto Refaccionaria CUATE - Inventario Operativo.

En esta fase no se está construyendo todavía la aplicación completa. El foco actual es:
- ordenar el repositorio
- documentar contexto y alcance inicial
- auditar el Excel operativo actual
- generar artefactos provisionales de normalización para preparar el diseño posterior

## Estructura

- `docs/`: documentación funcional y técnica inicial
- `data/raw/`: insumos fuente sin alterar
- `data/processed/`: salidas de análisis y archivos derivados provisionales
- `scripts/inspect_excel/`: scripts de inspección del Excel actual
- `app/`: placeholder para implementación futura

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
- No se fija todavía stack final de backend o frontend.
- `app/` permanece como placeholder hasta cerrar requerimientos y modelo operativo.

## Ejecución del script de inspección

```bash
python3 scripts/inspect_excel/inspect_excel.py
```

El script no depende de `pandas` ni de `openpyxl`; inspecciona el `.xlsm` directamente como paquete OpenXML.
