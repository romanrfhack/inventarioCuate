import { Component, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { SupplierCatalogImportApplyResponse, SupplierCatalogImportListItem, SupplierCatalogImportPreview } from '../../core/models/supplier-catalog.models';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  template: `
    <section class="grid" style="gap:24px;">
      <section class="card">
        <h1 style="margin-top:0;">Importación de catálogo proveedor</h1>
        <p>Sube el catálogo del proveedor, revisa coincidencias, conflictos y aplica solo altas o actualizaciones controladas.</p>

        <div *ngIf="successMessage()" style="margin-bottom:12px;padding:12px;background:#dcfce7;color:#166534;border-radius:8px;">{{ successMessage() }}</div>
        <div *ngIf="errorMessage()" style="margin-bottom:12px;padding:12px;background:#fee2e2;color:#991b1b;border-radius:8px;">{{ errorMessage() }}</div>

        <div class="grid" style="gap:12px;">
          <div>
            <label><strong>Proveedor</strong></label>
            <input [(ngModel)]="supplierName" placeholder="Proveedor Demo" />
          </div>
          <div>
            <label><strong>Nombre de archivo</strong></label>
            <input [(ngModel)]="fileName" placeholder="catalogo_proveedor.csv" />
          </div>
          <div>
            <label><strong>Seleccionar CSV</strong></label>
            <input type="file" accept=".csv,text/csv" (change)="onFileSelected($event)" />
          </div>
          <div>
            <label><strong>Contenido CSV</strong></label>
            <textarea [(ngModel)]="csvContent" rows="12" style="width:100%;padding:12px;border-radius:8px;border:1px solid #d1d5db;font-family:monospace;"></textarea>
          </div>
          <div style="display:flex;gap:12px;flex-wrap:wrap;">
            <button (click)="runPreview()" [disabled]="loadingPreview()">{{ loadingPreview() ? 'Generando preview...' : 'Generar preview' }}</button>
            <button class="secondary" (click)="loadTemplate()">Cargar template base</button>
            <button class="secondary" (click)="refreshBatches()">Refrescar lotes</button>
          </div>
        </div>
      </section>

      <section class="card" *ngIf="preview() as currentPreview">
        <h2 style="margin-top:0;">Preview del lote</h2>
        <div class="grid grid-3">
          <div><strong>Proveedor</strong><br />{{ currentPreview.supplierName }}</div>
          <div><strong>Estado</strong><br /><span class="badge" [ngClass]="badgeClass(currentPreview.status)">{{ currentPreview.status }}</span></div>
          <div><strong>Token</strong><br /><code>{{ currentPreview.confirmationToken }}</code></div>
          <div><strong>Listas</strong><br />{{ currentPreview.readyRows }}</div>
          <div><strong>Warnings</strong><br />{{ currentPreview.warningRows }}</div>
          <div><strong>Conflictos</strong><br />{{ currentPreview.conflictRows }}</div>
        </div>

        <div style="margin-top:16px;display:flex;gap:12px;align-items:center;flex-wrap:wrap;">
          <label style="display:flex;align-items:center;gap:8px;">
            <input type="checkbox" [(ngModel)]="confirmApply" /> Confirmo aplicación controlada
          </label>
          <button (click)="applyPreview()" [disabled]="!canApply(currentPreview) || applying()">{{ applying() ? 'Aplicando...' : 'Aplicar lote' }}</button>
        </div>

        <div *ngIf="applyResult() as result" style="margin-top:16px;padding:12px;background:#ecfeff;border-radius:8px;">
          <strong>Resultado:</strong>
          <pre style="white-space:pre-wrap;">{{ result | json }}</pre>
        </div>

        <div style="margin-top:16px;overflow:auto;">
          <table>
            <thead>
              <tr>
                <th>Fila</th>
                <th>Código proveedor</th>
                <th>Descripción</th>
                <th>Tipo match</th>
                <th>Acción</th>
                <th>Estatus</th>
                <th>Costo propuesto</th>
                <th>Precio propuesto</th>
                <th>Detalle</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let row of currentPreview.rows">
                <td>{{ row.sourceRow }}</td>
                <td>{{ row.supplierProductCode || '—' }}</td>
                <td>{{ row.description }}</td>
                <td>{{ row.matchType }}</td>
                <td>{{ row.actionType }}</td>
                <td><span class="badge" [ngClass]="badgeClass(row.rowStatus)">{{ row.rowStatus }}</span></td>
                <td>{{ row.proposedCost ?? '—' }}</td>
                <td>{{ row.proposedSalePrice ?? '—' }}</td>
                <td>{{ row.reviewReason || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="card">
        <h2 style="margin-top:0;">Lotes previos</h2>
        <div style="overflow:auto;">
          <table>
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Proveedor</th>
                <th>Archivo</th>
                <th>Estado</th>
                <th>Total</th>
                <th>Listas</th>
                <th>Warnings</th>
                <th>Conflictos</th>
                <th>Aplicadas</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let batch of batches()" (click)="viewBatch(batch.batchId)" style="cursor:pointer;">
                <td>{{ batch.createdAt | date:'yyyy-MM-dd HH:mm:ss' }}</td>
                <td>{{ batch.supplierName }}</td>
                <td>{{ batch.fileName }}</td>
                <td><span class="badge" [ngClass]="badgeClass(batch.status)">{{ batch.status }}</span></td>
                <td>{{ batch.totalRows }}</td>
                <td>{{ batch.readyRows }}</td>
                <td>{{ batch.warningRows }}</td>
                <td>{{ batch.conflictRows }}</td>
                <td>{{ batch.appliedRows }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </section>
  `
})
export class SupplierCatalogPageComponent {
  supplierName = 'Proveedor Demo';
  fileName = 'catalogo_proveedor.csv';
  csvContent = '';
  confirmApply = false;

  readonly preview = signal<SupplierCatalogImportPreview | null>(null);
  readonly applyResult = signal<SupplierCatalogImportApplyResponse | null>(null);
  readonly batches = signal<SupplierCatalogImportListItem[]>([]);
  readonly loadingPreview = signal(false);
  readonly applying = signal(false);
  readonly successMessage = signal('');
  readonly errorMessage = signal('');

  constructor(private readonly api: ApiService) {
    this.refreshBatches();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.fileName = file.name;
    file.text().then(text => this.csvContent = text);
  }

  loadTemplate() {
    this.csvContent = 'codigo,descripcion,marca,costo,precio_sugerido,unidad\n750100000001,Balata delantera sedan,Genérica,240.00,360.00,pz\nPROV-NEW-01,Bomba de gasolina compacta,MotorPro,420.00,620.00,pz\n,Filtro de aceite compacto,FiltroMax,65.00,110.00,pz\n';
  }

  runPreview() {
    this.loadingPreview.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');
    this.applyResult.set(null);

    this.api.previewSupplierCatalogImport(this.supplierName, this.fileName, this.csvContent).subscribe({
      next: response => {
        this.preview.set(response);
        this.confirmApply = false;
        this.loadingPreview.set(false);
        this.successMessage.set('Preview generado correctamente.');
        this.refreshBatches();
      },
      error: error => {
        this.loadingPreview.set(false);
        this.errorMessage.set(error?.error?.message || 'No se pudo generar el preview.');
      }
    });
  }

  applyPreview() {
    const currentPreview = this.preview();
    if (!currentPreview || !this.canApply(currentPreview)) return;

    this.applying.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.api.applySupplierCatalogImport(currentPreview.batchId, currentPreview.confirmationToken).subscribe({
      next: response => {
        this.applyResult.set(response);
        this.applying.set(false);
        this.successMessage.set('Lote aplicado correctamente.');
        this.refreshBatches();
        this.viewBatch(currentPreview.batchId);
      },
      error: error => {
        this.applying.set(false);
        this.errorMessage.set(error?.error?.message || 'No se pudo aplicar el lote.');
      }
    });
  }

  refreshBatches() {
    this.api.getSupplierCatalogImports().subscribe({
      next: response => this.batches.set(response),
      error: () => this.errorMessage.set('No se pudieron consultar los lotes previos.')
    });
  }

  viewBatch(batchId: string) {
    this.api.getSupplierCatalogImportDetail(batchId).subscribe({
      next: response => this.preview.set(response),
      error: () => this.errorMessage.set('No se pudo consultar el detalle del lote.')
    });
  }

  canApply(preview: SupplierCatalogImportPreview) {
    return this.confirmApply && preview.rows.some(row => row.applySelected);
  }

  badgeClass(status: string) {
    switch (status) {
      case 'ready':
      case 'preview_ready':
      case 'applied':
        return 'ok';
      case 'warning':
      case 'preview_with_conflicts':
      case 'applied_with_pending_conflicts':
      case 'conflict':
        return 'warn';
      default:
        return 'secondary';
    }
  }
}
