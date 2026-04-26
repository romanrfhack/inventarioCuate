import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { ProductItem } from '../../core/models/catalog.models';
import { InventoryMovementDetail, InventoryMovementFilters, InventoryMovementListItem, InventoryMovementResult, InventorySummaryItem } from '../../core/models/inventory.models';

@Component({
  standalone: true,
  imports: [CommonModule, DatePipe, ReactiveFormsModule],
  template: `
    <section style="display:grid;gap:16px;">
      <section class="card" style="display:grid;gap:8px;">
        <div>
          <h1 style="margin:0;">Inventario operativo</h1>
          <p style="margin:8px 0 0;color:#6b7280;">Registra entradas, ajustes y ahora consulta historial con filtros y detalle básico.</p>
        </div>
        <div *ngIf="error()" class="badge warn" style="width:max-content;">{{ error() }}</div>
        <div *ngIf="info()" class="badge ok" style="width:max-content;">{{ info() }}</div>
      </section>

      <section class="grid grid-2">
        <form class="card" [formGroup]="entryForm" (ngSubmit)="submitEntry()" style="display:grid;gap:12px;">
          <div>
            <h2 style="margin:0;">Entrada manual</h2>
            <p style="margin:8px 0 0;color:#6b7280;">Suma existencias cuando llega mercancía o material físico.</p>
          </div>
          <label style="display:grid;gap:6px;">
            <span>Producto</span>
            <select formControlName="productId">
              <option value="">Selecciona...</option>
              <option *ngFor="let product of products()" [value]="product.id">{{ product.description }} · stock {{ product.stock }}</option>
            </select>
          </label>
          <label style="display:grid;gap:6px;">
            <span>Cantidad</span>
            <input type="number" min="0.01" step="0.01" formControlName="quantity" />
          </label>
          <label style="display:grid;gap:6px;">
            <span>Motivo</span>
            <input type="text" maxlength="200" formControlName="reason" placeholder="Ej. ingreso por mostrador o acomodo físico" />
          </label>
          <button type="submit" [disabled]="entryForm.invalid || saving()">Registrar entrada</button>
        </form>

        <form class="card" [formGroup]="adjustmentForm" (ngSubmit)="submitAdjustment()" style="display:grid;gap:12px;">
          <div>
            <h2 style="margin:0;">Ajuste manual</h2>
            <p style="margin:8px 0 0;color:#6b7280;">Usa positivo o negativo, nunca cero. Se bloquea si deja stock negativo.</p>
          </div>
          <label style="display:grid;gap:6px;">
            <span>Producto</span>
            <select formControlName="productId">
              <option value="">Selecciona...</option>
              <option *ngFor="let product of products()" [value]="product.id">{{ product.description }} · stock {{ product.stock }}</option>
            </select>
          </label>
          <label style="display:grid;gap:6px;">
            <span>Cantidad delta</span>
            <input type="number" step="0.01" formControlName="quantityDelta" placeholder="Ej. -2 o 3" />
          </label>
          <label style="display:grid;gap:6px;">
            <span>Motivo</span>
            <input type="text" maxlength="200" formControlName="reason" placeholder="Ej. merma, conteo físico, corrección" />
          </label>
          <button type="submit" [disabled]="adjustmentForm.invalid || saving()">Registrar ajuste</button>
        </form>
      </section>

      <section *ngIf="lastMovement() as movement" class="card" style="background:#f9fafb;display:grid;gap:8px;">
        <h2 style="margin:0;">Último movimiento aplicado</h2>
        <div><strong>{{ movement.description }}</strong> · {{ getMovementLabel(movement.movementType) }} · {{ movement.createdAt | date:'short' }}</div>
        <div>Cantidad: {{ movement.quantity }} · Stock resultante: {{ movement.resultingStock }}</div>
        <div style="color:#6b7280;">Motivo: {{ movement.reason }}</div>
      </section>

      <section class="card">
        <div style="display:flex;justify-content:space-between;gap:12px;align-items:center;flex-wrap:wrap;">
          <div>
            <h2 style="margin:0;">Inventario actual</h2>
            <p style="margin:8px 0 0;color:#6b7280;">Vista simple para verificar impacto inmediato.</p>
          </div>
          <button type="button" class="secondary" (click)="loadData()" [disabled]="saving() || loadingMovements()">Actualizar</button>
        </div>
        <table>
          <thead>
            <tr>
              <th>Producto</th>
              <th>Existencia</th>
              <th>Actualizado</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of summary()">
              <td>{{ item.description }}</td>
              <td>{{ item.currentStock }}</td>
              <td>{{ item.updatedAt | date:'short' }}</td>
            </tr>
          </tbody>
        </table>
      </section>

      <section class="card" style="display:grid;gap:12px;">
        <div style="display:flex;justify-content:space-between;gap:12px;align-items:center;flex-wrap:wrap;">
          <div>
            <h2 style="margin:0;">Historial de movimientos</h2>
            <p style="margin:8px 0 0;color:#6b7280;">Consulta simple por producto, tipo, motivo y fecha. Muestra los 200 movimientos más recientes.</p>
          </div>
          <button type="button" class="secondary" (click)="loadMovements()" [disabled]="loadingMovements()">Actualizar historial</button>
        </div>

        <form [formGroup]="filtersForm" (ngSubmit)="applyFilters()" style="display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:12px;align-items:end;">
          <label style="display:grid;gap:6px;">
            <span>Producto</span>
            <select formControlName="productId">
              <option value="">Todos</option>
              <option *ngFor="let product of products()" [value]="product.id">{{ product.description }}</option>
            </select>
          </label>
          <label style="display:grid;gap:6px;">
            <span>Tipo</span>
            <select formControlName="movementType">
              <option value="">Todos</option>
              <option value="carga_inicial">Carga inicial</option>
              <option value="entrada_manual">Entrada manual</option>
              <option value="ajuste_manual">Ajuste manual</option>
              <option value="venta">Venta</option>
              <option value="venta_cancelacion">Cancelación de venta</option>
            </select>
          </label>
          <label style="display:grid;gap:6px;">
            <span>Motivo</span>
            <input type="text" formControlName="reason" placeholder="Texto parcial" />
          </label>
          <label style="display:grid;gap:6px;">
            <span>Desde</span>
            <input type="date" formControlName="dateFrom" />
          </label>
          <label style="display:grid;gap:6px;">
            <span>Hasta</span>
            <input type="date" formControlName="dateTo" />
          </label>
          <div style="display:flex;gap:8px;flex-wrap:wrap;grid-column:1/-1;">
            <button type="submit" class="secondary">Filtrar</button>
            <button type="button" class="secondary" (click)="clearFilters()">Limpiar</button>
          </div>
        </form>

        <div *ngIf="loadingMovements()" style="color:#6b7280;">Cargando movimientos...</div>

        <table *ngIf="movements().length; else noMovements">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Producto</th>
              <th>Tipo</th>
              <th>Cantidad</th>
              <th>Stock</th>
              <th>Motivo</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <ng-container *ngFor="let movement of movements()">
              <tr>
                <td>{{ movement.createdAt | date:'short' }}</td>
                <td>
                  <strong>{{ movement.description }}</strong>
                  <div style="color:#6b7280;font-size:12px;">{{ movement.sourceType }}{{ movement.sourceId ? ' · ' + movement.sourceId : '' }}</div>
                </td>
                <td>{{ getMovementLabel(movement.movementType) }}</td>
                <td>{{ movement.quantity }}</td>
                <td>{{ movement.resultingStock ?? 'n/d' }}</td>
                <td>{{ movement.reason || 'Sin motivo' }}</td>
                <td>
                  <button type="button" class="secondary" (click)="toggleMovementDetail(movement)">
                    {{ selectedMovementId() === movement.movementId ? 'Ocultar detalle' : 'Ver detalle' }}
                  </button>
                </td>
              </tr>
              <tr *ngIf="selectedMovementId() === movement.movementId">
                <td colspan="7" style="background:#f9fafb;">
                  <div *ngIf="loadingMovementDetail()" style="padding:12px 0;color:#6b7280;">Cargando detalle...</div>
                  <div *ngIf="!loadingMovementDetail() && selectedMovementDetail() as detail" style="padding:12px 0;display:grid;gap:8px;">
                    <div><strong>{{ detail.description }}</strong> · {{ getMovementLabel(detail.movementType) }} · {{ detail.createdAt | date:'medium' }}</div>
                    <div>Cantidad: {{ detail.quantity }} · Stock resultante: {{ detail.resultingStock ?? 'n/d' }}</div>
                    <div>Motivo: {{ detail.reason || 'Sin motivo' }}</div>
                    <div>Origen: {{ detail.sourceType }}{{ detail.sourceId ? ' · ' + detail.sourceId : '' }}</div>
                    <div>Usuario: {{ detail.userId || 'sin usuario' }} · Turno: {{ detail.shiftId || 'sin turno' }}</div>
                  </div>
                </td>
              </tr>
            </ng-container>
          </tbody>
        </table>

        <ng-template #noMovements>
          <p style="margin:0;color:#6b7280;">No hay movimientos para los filtros seleccionados.</p>
        </ng-template>
      </section>
    </section>
  `
})
export class InventoryPageComponent implements OnInit {
  readonly products = signal<ProductItem[]>([]);
  readonly summary = signal<InventorySummaryItem[]>([]);
  readonly movements = signal<InventoryMovementListItem[]>([]);
  readonly saving = signal(false);
  readonly loadingMovements = signal(false);
  readonly loadingMovementDetail = signal(false);
  readonly error = signal('');
  readonly info = signal('');
  readonly lastMovement = signal<InventoryMovementResult | null>(null);
  readonly selectedMovementId = signal('');
  readonly selectedMovementDetail = signal<InventoryMovementDetail | null>(null);

  readonly entryForm = this.fb.nonNullable.group({
    productId: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(0.01)]],
    reason: ['', [Validators.required, Validators.maxLength(200)]]
  });

  readonly adjustmentForm = this.fb.nonNullable.group({
    productId: ['', Validators.required],
    quantityDelta: [0, [Validators.required, Validators.pattern(/^-?\d+(\.\d+)?$/)]],
    reason: ['', [Validators.required, Validators.maxLength(200)]]
  });

  readonly filtersForm = this.fb.nonNullable.group({
    productId: [''],
    movementType: [''],
    reason: [''],
    dateFrom: [''],
    dateTo: ['']
  });

  constructor(private readonly api: ApiService, private readonly fb: FormBuilder) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.api.getProducts().subscribe((items) => this.products.set(items));
    this.api.getInventorySummary().subscribe((items) => this.summary.set(items));
    this.loadMovements();
  }

  loadMovements(): void {
    this.loadingMovements.set(true);
    this.api.getInventoryMovements(this.buildFilters()).subscribe({
      next: (items) => {
        this.movements.set(items);
        if (this.selectedMovementId() && !items.some((item) => item.movementId === this.selectedMovementId())) {
          this.selectedMovementId.set('');
          this.selectedMovementDetail.set(null);
        }
      },
      error: () => this.error.set('No fue posible consultar el historial de movimientos.'),
      complete: () => this.loadingMovements.set(false)
    });
  }

  applyFilters(): void {
    this.loadMovements();
  }

  clearFilters(): void {
    this.filtersForm.reset({
      productId: '',
      movementType: '',
      reason: '',
      dateFrom: '',
      dateTo: ''
    });
    this.loadMovements();
  }

  toggleMovementDetail(movement: InventoryMovementListItem): void {
    if (this.selectedMovementId() === movement.movementId) {
      this.selectedMovementId.set('');
      this.selectedMovementDetail.set(null);
      return;
    }

    this.selectedMovementId.set(movement.movementId);
    this.loadingMovementDetail.set(true);
    this.selectedMovementDetail.set(null);

    this.api.getInventoryMovementDetail(movement.movementId).subscribe({
      next: (detail) => this.selectedMovementDetail.set(detail),
      error: (errorResponse) => {
        this.error.set(errorResponse?.error?.message ?? 'No fue posible consultar el detalle del movimiento.');
        this.selectedMovementId.set('');
      },
      complete: () => this.loadingMovementDetail.set(false)
    });
  }

  submitEntry(): void {
    if (this.entryForm.invalid) {
      this.entryForm.markAllAsTouched();
      return;
    }

    this.runMutation(() => this.api.registerInventoryEntry({
      productId: this.entryForm.getRawValue().productId,
      quantity: Number(this.entryForm.getRawValue().quantity),
      reason: this.entryForm.getRawValue().reason.trim()
    }), 'Entrada manual registrada.');
  }

  submitAdjustment(): void {
    if (this.adjustmentForm.invalid) {
      this.adjustmentForm.markAllAsTouched();
      return;
    }

    const quantityDelta = Number(this.adjustmentForm.getRawValue().quantityDelta);
    if (quantityDelta === 0) {
      this.error.set('El ajuste debe ser distinto de cero.');
      return;
    }

    this.runMutation(() => this.api.registerInventoryAdjustment({
      productId: this.adjustmentForm.getRawValue().productId,
      quantityDelta,
      reason: this.adjustmentForm.getRawValue().reason.trim()
    }), 'Ajuste manual registrado.');
  }

  getMovementLabel(movementType: string): string {
    switch (movementType) {
      case 'carga_inicial': return 'Carga inicial';
      case 'entrada_manual': return 'Entrada manual';
      case 'ajuste_manual': return 'Ajuste manual';
      case 'venta': return 'Venta';
      case 'venta_cancelacion': return 'Cancelación de venta';
      case 'reset_demo': return 'Reset demo';
      default: return movementType;
    }
  }

  private buildFilters(): InventoryMovementFilters {
    const raw = this.filtersForm.getRawValue();
    return {
      productId: raw.productId,
      movementType: raw.movementType,
      reason: raw.reason,
      dateFrom: raw.dateFrom,
      dateTo: raw.dateTo
    };
  }

  private runMutation(factory: () => Observable<InventoryMovementResult>, successMessage: string): void {
    this.saving.set(true);
    this.error.set('');
    this.info.set('');

    factory().subscribe({
      next: (movement) => {
        this.lastMovement.set(movement);
        this.info.set(`${successMessage} Stock actual: ${movement.resultingStock}.`);
        this.entryForm.patchValue({ quantity: 1, reason: '' });
        this.adjustmentForm.patchValue({ quantityDelta: 0, reason: '' });
        this.loadData();
        this.saving.set(false);
      },
      error: (errorResponse) => {
        this.error.set(errorResponse?.error?.message ?? 'No fue posible registrar el movimiento.');
        this.saving.set(false);
      }
    });
  }
}
