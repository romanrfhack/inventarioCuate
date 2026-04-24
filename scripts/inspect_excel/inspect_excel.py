#!/usr/bin/env python3
from __future__ import annotations

from collections import Counter
from pathlib import Path
from zipfile import ZipFile
import csv
import re
import xml.etree.ElementTree as ET

NS = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'
REL = '{http://schemas.openxmlformats.org/officeDocument/2006/relationships}'

REPO_ROOT = Path(__file__).resolve().parents[2]
WORKBOOK_PATH = REPO_ROOT / 'data/raw/CUATE NEXT.xlsm'
OUT_INVENTORY = REPO_ROOT / 'data/processed/inventario_normalizado_borrador.csv'
OUT_MOVES = REPO_ROOT / 'data/processed/movimientos_detectados_borrador.csv'
OUT_SUMMARY = REPO_ROOT / 'data/processed/auditoria_resumen_borrador.txt'


def parse_shared_strings(zf: ZipFile) -> list[str]:
    if 'xl/sharedStrings.xml' not in zf.namelist():
        return []
    root = ET.fromstring(zf.read('xl/sharedStrings.xml'))
    return [''.join(t.text or '' for t in si.iter(f'{NS}t')) for si in root.findall(f'{NS}si')]


def read_cell_value(cell: ET.Element, shared_strings: list[str]) -> str:
    cell_type = cell.attrib.get('t')
    value = cell.find(f'{NS}v')
    if cell_type == 's' and value is not None and value.text is not None:
        idx = int(value.text)
        return shared_strings[idx] if idx < len(shared_strings) else ''
    if cell_type == 'inlineStr':
        inline = cell.find(f'{NS}is')
        if inline is not None:
            return ''.join(t.text or '' for t in inline.iter(f'{NS}t'))
    if value is not None and value.text is not None:
        return value.text
    return ''


def column_number(ref: str) -> int:
    match = re.match(r'([A-Z]+)', ref)
    letters = match.group(1) if match else ref
    total = 0
    for char in letters:
        total = total * 26 + ord(char.upper()) - 64
    return total


def as_number(raw: str | None) -> float | None:
    if raw is None:
        return None
    text = str(raw).strip().replace(',', '')
    if not text:
        return None
    try:
        return float(text)
    except ValueError:
        return None


def infer_move_type(column: str) -> str:
    return 'entrada' if column_number(column) % 2 == 1 else 'salida'


def workbook_sheets(zf: ZipFile) -> list[tuple[str, str]]:
    workbook = ET.fromstring(zf.read('xl/workbook.xml'))
    rels = ET.fromstring(zf.read('xl/_rels/workbook.xml.rels'))
    relmap = {rel.attrib['Id']: rel.attrib['Target'] for rel in rels}
    return [
        (sheet.attrib['name'], 'xl/' + relmap[sheet.attrib[f'{REL}id']].lstrip('/'))
        for sheet in workbook.find(f'{NS}sheets')
    ]


def load_rows(zf: ZipFile, xml_path: str, shared_strings: list[str]) -> list[tuple[int, dict[str, str]]]:
    root = ET.fromstring(zf.read(xml_path))
    loaded: list[tuple[int, dict[str, str]]] = []
    for row in root.findall(f'.//{NS}row'):
        row_number = int(row.attrib.get('r', '0'))
        values: dict[str, str] = {}
        for cell in row.findall(f'{NS}c'):
            ref = cell.attrib.get('r', '')
            match = re.match(r'([A-Z]+)', ref)
            if not match:
                continue
            values[match.group(1)] = read_cell_value(cell, shared_strings)
        loaded.append((row_number, values))
    return loaded


def is_candidate_product_row(row_number: int, data: dict[str, str]) -> bool:
    if row_number < 5:
        return False
    if not any(str(data.get(col, '')).strip() for col in ['A', 'B', 'C', 'D', 'E', 'F']):
        return False
    description = str(data.get('A', '')).strip()
    brand = str(data.get('C', '')).strip()
    if description.upper() == 'DESCRIPCION' or brand.upper() == 'MARCA':
        return False
    if description.isdigit() and not str(data.get('B', '')).strip() and not brand:
        return False
    return True


def main() -> None:
    if not WORKBOOK_PATH.exists():
        raise SystemExit(f'No existe el archivo: {WORKBOOK_PATH}')

    inventory_rows: list[dict[str, str]] = []
    move_rows: list[dict[str, str]] = []
    summary_rows: list[dict[str, object]] = []

    with ZipFile(WORKBOOK_PATH) as workbook:
        shared_strings = parse_shared_strings(workbook)
        sheets = workbook_sheets(workbook)

        for sheet_index, (sheet_name, xml_path) in enumerate(sheets, start=1):
            rows = load_rows(workbook, xml_path, shared_strings)
            header_row = rows[0][1] if rows else {}
            product_rows = [(row_number, data) for row_number, data in rows if is_candidate_product_row(row_number, data)]

            with_code = 0
            without_code = 0
            without_cost = 0
            without_price = 0
            negative_stock = 0
            invalid_margin = 0
            code_counter: Counter[str] = Counter()
            present_columns: set[str] = set()

            for row_number, data in product_rows:
                description = str(data.get('A', '')).strip()
                code = str(data.get('B', '')).strip()
                brand = str(data.get('C', '')).strip()
                cost = as_number(data.get('D'))
                price = as_number(data.get('E'))
                total = as_number(data.get('F'))

                if code:
                    with_code += 1
                    code_counter[code] += 1
                else:
                    without_code += 1
                if cost is None:
                    without_cost += 1
                if price is None:
                    without_price += 1
                if total is not None and total < 0:
                    negative_stock += 1
                if cost is not None and price is not None and cost >= price:
                    invalid_margin += 1

                present_columns.update(col for col, value in data.items() if str(value).strip())

                reasons: list[str] = []
                if not code:
                    reasons.append('sin_codigo')
                if cost is None:
                    reasons.append('sin_costo')
                if price is None:
                    reasons.append('sin_precio_venta')
                if total is not None and total < 0:
                    reasons.append('existencia_negativa')
                if cost is not None and price is not None and cost >= price:
                    reasons.append('costo_mayor_o_igual_precio')

                temporary_id = f'H{sheet_index}-R{row_number}'
                inventory_rows.append(
                    {
                        'producto_id_temporal': temporary_id,
                        'descripcion': description,
                        'codigo': code,
                        'marca': brand,
                        'proveedor': '',
                        'costo': '' if cost is None else f'{cost:.2f}',
                        'precio_venta': '' if price is None else f'{price:.2f}',
                        'existencia_actual': '' if total is None else (str(int(total)) if total.is_integer() else str(total)),
                        'hoja_origen': sheet_name,
                        'fila_origen': str(row_number),
                        'requiere_revision': 'true' if reasons else 'false',
                        'motivo_revision': ';'.join(reasons),
                    }
                )

                for column, raw_value in data.items():
                    if column <= 'F':
                        continue
                    quantity = as_number(raw_value)
                    if quantity is None or quantity == 0:
                        continue
                    block = str(header_row.get(column, '')).strip()
                    move_rows.append(
                        {
                            'fecha_o_bloque': f'Diciembre {block}'.strip(),
                            'producto_id_temporal': temporary_id,
                            'codigo': code,
                            'descripcion': description,
                            'tipo_movimiento': infer_move_type(column),
                            'cantidad': str(int(quantity)) if quantity.is_integer() else str(quantity),
                            'hoja_origen': sheet_name,
                            'fila_origen': str(row_number),
                            'columna_origen': column,
                        }
                    )

            summary_rows.append(
                {
                    'sheet': sheet_name,
                    'rows': len(product_rows),
                    'with_code': with_code,
                    'no_code': without_code,
                    'no_cost': without_cost,
                    'no_price': without_price,
                    'duplicate_codes': sum(1 for _, count in code_counter.items() if count > 1),
                    'negative_stock': negative_stock,
                    'bad_margin': invalid_margin,
                    'columns_present': ','.join(sorted(present_columns, key=column_number)),
                }
            )

    OUT_INVENTORY.parent.mkdir(parents=True, exist_ok=True)
    with OUT_INVENTORY.open('w', newline='', encoding='utf-8') as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                'producto_id_temporal',
                'descripcion',
                'codigo',
                'marca',
                'proveedor',
                'costo',
                'precio_venta',
                'existencia_actual',
                'hoja_origen',
                'fila_origen',
                'requiere_revision',
                'motivo_revision',
            ],
        )
        writer.writeheader()
        writer.writerows(inventory_rows)

    with OUT_MOVES.open('w', newline='', encoding='utf-8') as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                'fecha_o_bloque',
                'producto_id_temporal',
                'codigo',
                'descripcion',
                'tipo_movimiento',
                'cantidad',
                'hoja_origen',
                'fila_origen',
                'columna_origen',
            ],
        )
        writer.writeheader()
        writer.writerows(move_rows)

    with OUT_SUMMARY.open('w', encoding='utf-8') as handle:
        for row in summary_rows:
            handle.write(f'{row}\n')
        handle.write(f'total_inventory_rows={len(inventory_rows)}\n')
        handle.write(f'total_move_rows={len(move_rows)}\n')

    print(f'Inventario provisional: {OUT_INVENTORY}')
    print(f'Movimientos detectados: {OUT_MOVES}')
    print(f'Resumen técnico: {OUT_SUMMARY}')


if __name__ == '__main__':
    main()
