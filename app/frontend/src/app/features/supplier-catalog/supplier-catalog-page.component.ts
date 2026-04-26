import { Component, computed, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { SupplierCatalogImportApplyResponse, SupplierCatalogImportListItem, SupplierCatalogImportPreview, SupplierCatalogProfile } from '../../core/models/supplier-catalog.models';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  template: `
    <section class="grid" style="gap:24px;">
      <section class="card">
        <h1 style="margin-top:0;">Importación de catálogos de proveedor</h1>
        <p>Slice inicial para archivos conocidos. No toca inventario local, solo preview y actualización controlada del catálogo.</p>

        <div *ngIf="successMessage()" style="margin-bottom:12px;padding:12px;background:#dcfce7;color:#166534;border-radius:8px;">{{ successMessage() }}</div>
        <div *ngIf="errorMessage()" style="margin-bottom:12px;padding:12px;background:#fee2e2;color:#991b1b;border-radius:8px;">{{ errorMessage() }}</div>

        <div class="grid" style="gap:12px;">
          <div>
            <label><strong>Perfil</strong></label>
            <select [(ngModel)]="selectedProfileKey" (ngModelChange)="syncSupplierFromProfile()">
              <option *ngFor="let profile of profiles()" [value]="profile.key">{{ profile.supplierName }} ({{ profile.key }})</option>
            </select>
          </div>
          <div>
            <label><strong>Proveedor</strong></label>
            <input [(ngModel)]="supplierName" placeholder="Alessia" />
          </div>
          <div>
            <label><strong>Archivo</strong></label>
            <input type="file" accept=".xlsx,.xlsm,.xls" (change)="onFileSelected($event)" />
          </div>
          <div *ngIf="selectedProfile() as profile" style="font-size:0.9rem;color:#475569;">
            Hoja sugerida: <strong>{{ profile.preferredSheet }}</strong><br />
            Hojas compatibles: {{ profile.candidateSheets.join(', ') }}
          </div>
          <div style="display:flex;gap:12px;flex-wrap:wrap;">
            <button (click)="runPreview()" [disabled]="loadingPreview() || !selectedFile()">{{ loadingPreview() ? 'Generando preview...' : 'Generar preview' }}</button>
            <button class="secondary" (click)="refreshBatches()">Refrescar importaciones</button>
          </div>
        </div>
      </section>

      <section class="card" *ngIf="preview() as currentPreview">
        <h2 style="margin-top:0;">Preview del lote</h2>
        <div class="grid grid-3">
          <div><strong>Proveedor</strong><br />{{ currentPreview.supplierName }}</div>
          <div><strong>Perfil</strong><br />{{ currentPreview.importProfile }}</div>
          <div><strong>Archivo</strong><br />{{ currentPreview.fileName }}</div>
          <div><strong>Match código</strong><br />{{ currentPreview.matchCodigoRows }}</div>
          <div><strong>Producto nuevo</strong><br />{{ currentPreview.productoNuevoRows }}</div>
          <div><strong>Revisión</strong><br />{{ currentPreview.datoIncompletoRows + currentPreview.requiereRevisionRows }}</div>
        </div>

        <div style="margin-top:16px;display:flex;gap:12px;align-items:center;flex-wrap:wrap;">
          <label style="display:flex;align-items:center;gap:8px;">
            <input type="checkbox" [(ngModel)]="confirmApply" /> Confirmo aplicar sin tocar existencias locales
          </label>
          <button (click)="applyPreview()" [disabled]="!canApply(currentPreview) || applying()">{{ applying() ? 'Aplicando...' : 'Aplicar lote' }}</button>
        </div>

        <div *ngIf="applyResult() as result" style="margin-top:16px;padding:12px;background:#ecfeff;border-radius:8px;">
          Actualizados: {{ result.updatedProducts }}, nuevos: {{ result.createdProducts }}, revisión pendiente: {{ result.requiereRevisionRows }}
        </div>

        <div style="margin-top:16px;overflow:auto;max-height:540px;">
          <table>
            <thead>
              <tr>
                <th>Fila</th><th>Hoja</th><th>Código</th><th>Descripción</th><th>Estado</th><th>Acción</th><th>Costo</th><th>Precio</th><th>Disp.</th><th>Detalle</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let row of currentPreview.rows">
                <td>{{ row.sourceRow }}</td>
                <td>{{ row.sourceSheet }}</td>
                <td>{{ row.supplierProductCode || '—' }}</td>
                <td>{{ row.description }}</td>
                <td><span class="badge" [ngClass]="badgeClass(row.rowStatus)">{{ row.rowStatus }}</span></td>
                <td>{{ row.actionType }}</td>
                <td>{{ row.proposedCost ?? row.cost ?? '—' }}</td>
                <td>{{ row.proposedSalePrice ?? row.suggestedSalePrice ?? '—' }}</td>
                <td>{{ row.supplierStockText ?? row.supplierAvailability ?? '—' }}</td>
                <td>{{ row.reviewReason || row.revisionReason || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="card">
        <h2 style="margin-top:0;">Importaciones previas</h2>
        <div style="overflow:auto;">
          <table>
            <thead>
              <tr>
                <th>Fecha</th><th>Proveedor</th><th>Perfil</th><th>Archivo</th><th>Estado</th><th>Total</th><th>Match código</th><th>Nuevos</th><th>Revisión</th><th>Aplicadas</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let batch of batches()" (click)="viewBatch(batch.batchId)" style="cursor:pointer;">
                <td>{{ batch.createdAt | date:'yyyy-MM-dd HH:mm:ss' }}</td>
                <td>{{ batch.supplierName }}</td>
                <td>{{ batch.importProfile }}</td>
                <td>{{ batch.fileName }}</td>
                <td><span class="badge" [ngClass]="badgeClass(batch.status)">{{ batch.status }}</span></td>
                <td>{{ batch.totalRows }}</td>
                <td>{{ batch.matchCodigoRows }}</td>
                <td>{{ batch.productoNuevoRows }}</td>
                <td>{{ batch.datoIncompletoRows + batch.requiereRevisionRows }}</td>
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
  supplierName = 'Alessia';
  selectedProfileKey = 'alessia';
  confirmApply = false;

  readonly preview = signal<SupplierCatalogImportPreview | null>(null);
  readonly applyResult = signal<SupplierCatalogImportApplyResponse | null>(null);
  readonly batches = signal<SupplierCatalogImportListItem[]>([]);
  readonly profiles = signal<SupplierCatalogProfile[]>([]);
  readonly selectedFile = signal<File | null>(null);
  readonly loadingPreview = signal(false);
  readonly applying = signal(false);
  readonly successMessage = signal('');
  readonly errorMessage = signal('');
  readonly selectedProfile = computed(() => this.profiles().find(x => x.key === this.selectedProfileKey) ?? null);

  constructor(private readonly api: ApiService) {
    this.api.getSupplierCatalogProfiles().subscribe({
      next: response => {
        this.profiles.set(response);
        if (response.length > 0) {
          this.selectedProfileKey = response[0].key;
          this.syncSupplierFromProfile();
        }
      }
    });
    this.refreshBatches();
  }

  syncSupplierFromProfile() {
    const profile = this.selectedProfile();
    if (profile) this.supplierName = profile.supplierName;
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile.set(file);
  }

  runPreview() {
    const file = this.selectedFile();
    if (!file) return;
    this.loadingPreview.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');
    this.applyResult.set(null);

    this.api.previewSupplierCatalogImport(this.supplierName, this.selectedProfileKey, file).subscribe({
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
    this.api.getSupplierCatalogImports().subscribe({ next: response => this.batches.set(response) });
  }

  viewBatch(batchId: string) {
    this.api.getSupplierCatalogImportDetail(batchId).subscribe({ next: response => this.preview.set(response) });
  }

  canApply(preview: SupplierCatalogImportPreview) { return this.confirmApply && preview.rows.some(row => row.applySelected); }

  badgeClass(status: string) {
    switch (status) {
      case 'match_codigo':
      case 'producto_nuevo':
      case 'preview_lista':
      case 'applied':
        return 'ok';
      case 'dato_incompleto':
      case 'requiere_revision':
      case 'preview_con_revision':
      case 'applied_with_pending_review':
        return 'warn';
      default:
        return 'secondary';
    }
  }
}
