import { Component, effect, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { InitialLoadApplyResponse, InitialLoadListItem, InitialLoadPreviewSummary } from '../../core/models/initial-load.models';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  template: `
    <section class="grid" style="gap:24px;">
      <section class="card">
        <h1 style="margin-top:0;">Carga inicial real</h1>
        <p>Sube o pega el CSV, genera preview, revisa filas y aplica la carga con confirmación segura.</p>

        <div *ngIf="successMessage()" style="margin-bottom:12px;padding:12px;background:#dcfce7;color:#166534;border-radius:8px;">{{ successMessage() }}</div>
        <div *ngIf="errorMessage()" style="margin-bottom:12px;padding:12px;background:#fee2e2;color:#991b1b;border-radius:8px;">{{ errorMessage() }}</div>

        <div class="grid" style="gap:12px;">
          <div>
            <label><strong>Nombre de archivo</strong></label>
            <input [(ngModel)]="fileName" placeholder="inventario_inicial.csv" />
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
            <button class="secondary" (click)="refreshLoads()">Refrescar cargas</button>
          </div>
        </div>
      </section>

      <section class="card" *ngIf="preview() as currentPreview">
        <h2 style="margin-top:0;">Resumen del preview</h2>
        <div class="grid grid-3">
          <div><strong>LoadId</strong><br />{{ currentPreview.loadId }}</div>
          <div><strong>Estado</strong><br /><span class="badge" [ngClass]="badgeClass(currentPreview.status)">{{ currentPreview.status }}</span></div>
          <div><strong>Token</strong><br /><code>{{ currentPreview.confirmationToken }}</code></div>
          <div><strong>Válidas</strong><br />{{ currentPreview.validRows }}</div>
          <div><strong>Warnings</strong><br />{{ currentPreview.warningRows }}</div>
          <div><strong>Inválidas</strong><br />{{ currentPreview.invalidRows }}</div>
        </div>

        <div style="margin-top:16px;display:flex;gap:12px;align-items:center;flex-wrap:wrap;">
          <label style="display:flex;align-items:center;gap:8px;">
            <input type="checkbox" [(ngModel)]="confirmApply" /> Confirmo que quiero aplicar esta carga
          </label>
          <button (click)="applyPreview()" [disabled]="!canApply(currentPreview) || applying()">
            {{ applying() ? 'Aplicando...' : 'Aplicar carga' }}
          </button>
        </div>

        <div *ngIf="applyResult() as applyResult" style="margin-top:16px;padding:12px;background:#ecfeff;border-radius:8px;">
          <strong>Apply ejecutado:</strong>
          <pre style="white-space:pre-wrap;">{{ applyResult | json }}</pre>
        </div>

        <div style="margin-top:16px;overflow:auto;">
          <table>
            <thead>
              <tr>
                <th>Fila</th>
                <th>Código</th>
                <th>Descripción</th>
                <th>Existencia</th>
                <th>Estado</th>
                <th>Detalle</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let row of currentPreview.rows">
                <td>{{ row.sourceRow }}</td>
                <td>{{ row.code || '—' }}</td>
                <td>{{ row.description }}</td>
                <td>{{ row.initialStock }}</td>
                <td><span class="badge" [ngClass]="badgeClass(row.rowStatus)">{{ row.rowStatus }}</span></td>
                <td>{{ row.reviewReason || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="card">
        <h2 style="margin-top:0;">Cargas previas</h2>
        <div style="overflow:auto;">
          <table>
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Archivo</th>
                <th>Estado</th>
                <th>Filas</th>
                <th>Válidas</th>
                <th>Warnings</th>
                <th>Inválidas</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let load of loads()" (click)="viewLoad(load.loadId)" style="cursor:pointer;">
                <td>{{ load.createdAt | date:'yyyy-MM-dd HH:mm:ss' }}</td>
                <td>{{ load.fileName || '—' }}</td>
                <td><span class="badge" [ngClass]="badgeClass(load.status)">{{ load.status }}</span></td>
                <td>{{ load.totalRows }}</td>
                <td>{{ load.validRows }}</td>
                <td>{{ load.warningRows }}</td>
                <td>{{ load.invalidRows }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="card" *ngIf="selectedLoad() as historicalLoad">
        <h2 style="margin-top:0;">Detalle de carga previa</h2>
        <p><strong>LoadId:</strong> {{ historicalLoad.loadId }}</p>
        <p><strong>Estado:</strong> <span class="badge" [ngClass]="badgeClass(historicalLoad.status)">{{ historicalLoad.status }}</span></p>
        <p><strong>Resumen:</strong> {{ historicalLoad.validRows }} válidas, {{ historicalLoad.warningRows }} warnings, {{ historicalLoad.invalidRows }} inválidas</p>
      </section>
    </section>
  `
})
export class InitialLoadPageComponent {
  fileName = 'inventario_inicial.csv';
  csvContent = '';
  confirmApply = false;

  readonly preview = signal<InitialLoadPreviewSummary | null>(null);
  readonly selectedLoad = signal<InitialLoadPreviewSummary | null>(null);
  readonly applyResult = signal<InitialLoadApplyResponse | null>(null);
  readonly loads = signal<InitialLoadListItem[]>([]);
  readonly loadingPreview = signal(false);
  readonly applying = signal(false);
  readonly loadingDetail = signal(false);
  readonly successMessage = signal('');
  readonly errorMessage = signal('');

  constructor(private readonly api: ApiService) {
    effect(() => {
      void this.loads();
    });

    this.refreshLoads();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.fileName = file.name;
    file.text().then(text => {
      this.csvContent = text;
    });
  }

  loadTemplate() {
    this.csvContent = 'codigo,descripcion,marca,proveedor,costo,precio_venta,existencia_inicial,unidad,ubicacion,observaciones\n,Ejemplo: Bujía NGK CR7HSA,NGK,MotoPartes del Centro,38.50,65.00,12,pieza,A1,Referencia de ejemplo\n';
  }

  runPreview() {
    this.loadingPreview.set(true);
    this.applyResult.set(null);
    this.successMessage.set('');
    this.errorMessage.set('');

    this.api.previewInitialLoad(this.fileName, this.csvContent).subscribe({
      next: (response) => {
        this.preview.set(response);
        this.confirmApply = false;
        this.loadingPreview.set(false);
        this.successMessage.set('Preview generado correctamente.');
        this.refreshLoads();
      },
      error: (error) => {
        this.loadingPreview.set(false);
        this.errorMessage.set(error?.error?.message || 'No se pudo generar el preview.');
      }
    });
  }

  applyPreview() {
    const currentPreview = this.preview();
    if (!currentPreview || !this.canApply(currentPreview)) return;

    this.applying.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    this.api.applyInitialLoad(currentPreview.loadId, currentPreview.confirmationToken).subscribe({
      next: (response) => {
        this.applyResult.set(response);
        this.applying.set(false);
        this.successMessage.set('Carga aplicada correctamente.');
        this.refreshLoads();
        this.viewLoad(currentPreview.loadId);
      },
      error: (error) => {
        this.applying.set(false);
        this.errorMessage.set(error?.error?.message || 'No se pudo aplicar la carga.');
      }
    });
  }

  refreshLoads() {
    this.api.getInitialLoads().subscribe({
      next: (response) => this.loads.set(response),
      error: () => this.errorMessage.set('No se pudieron consultar las cargas previas.')
    });
  }

  viewLoad(loadId: string) {
    this.loadingDetail.set(true);
    this.api.getInitialLoadDetail(loadId).subscribe({
      next: (response) => {
        this.selectedLoad.set(response);
        this.loadingDetail.set(false);
      },
      error: () => {
        this.loadingDetail.set(false);
        this.errorMessage.set('No se pudo consultar el detalle de la carga.');
      }
    });
  }

  canApply(preview: InitialLoadPreviewSummary) {
    return this.confirmApply && preview.status === 'previewed' && preview.invalidRows === 0;
  }

  badgeClass(status: string) {
    switch (status) {
      case 'valid':
      case 'previewed':
      case 'applied':
        return 'ok';
      case 'warning':
      case 'ready_for_apply':
        return 'warn';
      default:
        return 'secondary';
    }
  }
}
