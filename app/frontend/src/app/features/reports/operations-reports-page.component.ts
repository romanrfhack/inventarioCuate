import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { OperationsReport } from '../../core/models/reports.models';

@Component({
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe],
  template: `
    <section class="card" style="display:grid;gap:16px;">
      <div style="display:flex;justify-content:space-between;gap:12px;align-items:start;flex-wrap:wrap;">
        <div>
          <h1 style="margin:0;">Reportes operativos</h1>
          <p style="margin:8px 0 0;color:#6b7280;">Visibilidad mínima sobre inventario, ventas recientes, anomalías y utilidad bruta provisional.</p>
        </div>
        <button type="button" class="secondary" (click)="load()">Actualizar</button>
      </div>

      <div *ngIf="error()" class="badge warn" style="width:max-content;">{{ error() }}</div>

      <section *ngIf="report() as data" class="grid grid-3">
        <article class="card"><h3>Productos</h3><strong>{{ data.summary.totalProducts }}</strong><div style="color:#6b7280;">{{ data.summary.productsWithStock }} con stock, {{ data.summary.productsWithoutStock }} en cero</div></article>
        <article class="card"><h3>Ventas confirmadas</h3><strong>{{ data.summary.confirmedSalesTotal | currency:'MXN':'symbol-narrow' }}</strong><div style="color:#6b7280;">{{ data.summary.confirmedSalesCount }} ventas, utilidad {{ data.summary.confirmedSalesGrossProfit | currency:'MXN':'symbol-narrow' }}</div></article>
        <article class="card"><h3>Inventario valorizado</h3><strong>{{ data.summary.inventoryRetailValue | currency:'MXN':'symbol-narrow' }}</strong><div style="color:#6b7280;">Costo estimado {{ data.summary.inventoryCostValue | currency:'MXN':'symbol-narrow' }}</div></article>
      </section>

      <section *ngIf="report() as data" class="grid grid-2">
        <article class="card">
          <h2 style="margin-top:0;">Anomalías detectadas</h2>
          <p *ngIf="!data.productAnomalies.length" style="margin:0;color:#6b7280;">Sin anomalías con las reglas actuales.</p>
          <ul *ngIf="data.productAnomalies.length" style="margin:0;padding-left:18px;display:grid;gap:8px;">
            <li *ngFor="let item of data.productAnomalies.slice(0, 8)">
              <strong>{{ item.internalKey }}</strong> · {{ item.description }} · stock {{ item.currentStock }}
              <div style="color:#6b7280;">{{ item.reasons.join(', ') }}</div>
            </li>
          </ul>
        </article>

        <article class="card">
          <h2 style="margin-top:0;">Productos más rentables</h2>
          <p *ngIf="!data.profitableProducts.length" style="margin:0;color:#6b7280;">Aún no hay base suficiente con costo vigente para calcular utilidad.</p>
          <table *ngIf="data.profitableProducts.length">
            <thead><tr><th>Producto</th><th>Cantidad</th><th>Venta</th><th>Utilidad</th></tr></thead>
            <tbody>
              <tr *ngFor="let item of data.profitableProducts">
                <td>{{ item.description }}</td>
                <td>{{ item.quantitySold }}</td>
                <td>{{ item.salesAmount | currency:'MXN':'symbol-narrow' }}</td>
                <td>{{ item.grossProfit | currency:'MXN':'symbol-narrow' }}</td>
              </tr>
            </tbody>
          </table>
        </article>
      </section>

      <section *ngIf="report() as data" class="grid grid-2">
        <article class="card">
          <h2 style="margin-top:0;">Ventas recientes</h2>
          <table>
            <thead><tr><th>Folio</th><th>Fecha</th><th>Estatus</th><th>Total</th><th>Utilidad</th></tr></thead>
            <tbody>
              <tr *ngFor="let sale of data.recentSales">
                <td>{{ sale.folio }}</td>
                <td>{{ sale.createdAt | date:'short' }}</td>
                <td><span class="badge" [class.ok]="sale.status === 'confirmed'" [class.warn]="sale.status !== 'confirmed'">{{ sale.status }}</span></td>
                <td>{{ sale.total | currency:'MXN':'symbol-narrow' }}</td>
                <td>{{ sale.grossProfit == null ? 'n/d' : (sale.grossProfit | currency:'MXN':'symbol-narrow') }}</td>
              </tr>
            </tbody>
          </table>
        </article>

        <article class="card">
          <h2 style="margin-top:0;">Inventario actual con foco operativo</h2>
          <table>
            <thead><tr><th>Clave</th><th>Producto</th><th>Stock</th><th>Precio</th><th>Flags</th></tr></thead>
            <tbody>
              <tr *ngFor="let item of inventoryPreview()">
                <td>{{ item.internalKey }}</td>
                <td>{{ item.description }}</td>
                <td>{{ item.currentStock }}</td>
                <td>{{ item.currentSalePrice == null ? 'n/d' : (item.currentSalePrice | currency:'MXN':'symbol-narrow') }}</td>
                <td>{{ item.flags.length ? item.flags.join(', ') : 'ok' }}</td>
              </tr>
            </tbody>
          </table>
        </article>
      </section>
    </section>
  `
})
export class OperationsReportsPageComponent implements OnInit {
  readonly report = signal<OperationsReport | null>(null);
  readonly error = signal('');
  readonly inventoryPreview = computed(() => this.report()?.inventory.slice(0, 10) ?? []);

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.error.set('');
    this.api.getOperationsReport().subscribe({
      next: (report) => this.report.set(report),
      error: (err) => this.error.set(err?.error?.message ?? 'No fue posible cargar los reportes operativos.')
    });
  }
}
