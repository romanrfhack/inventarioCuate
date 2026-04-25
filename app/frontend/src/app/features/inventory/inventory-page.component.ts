import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { ProductItem } from '../../core/models/catalog.models';
import { InventoryMovementResult, InventorySummaryItem } from '../../core/models/inventory.models';

@Component({
  standalone: true,
  imports: [CommonModule, DatePipe, ReactiveFormsModule],
  template: `
    <section style="display:grid;gap:16px;">
      <section class="card" style="display:grid;gap:8px;">
        <div>
          <h1 style="margin:0;">Inventario operativo</h1>
          <p style="margin:8px 0 0;color:#6b7280;">Registra entradas manuales y ajustes con motivo obligatorio, actualización inmediata y trazabilidad básica.</p>
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
        <div><strong>{{ movement.description }}</strong> · {{ movement.movementType }} · {{ movement.createdAt | date:'short' }}</div>
        <div>Cantidad: {{ movement.quantity }} · Stock resultante: {{ movement.resultingStock }}</div>
        <div style="color:#6b7280;">Motivo: {{ movement.reason }}</div>
      </section>

      <section class="card">
        <div style="display:flex;justify-content:space-between;gap:12px;align-items:center;flex-wrap:wrap;">
          <div>
            <h2 style="margin:0;">Inventario actual</h2>
            <p style="margin:8px 0 0;color:#6b7280;">Vista simple para verificar impacto inmediato.</p>
          </div>
          <button type="button" class="secondary" (click)="loadData()" [disabled]="saving()">Actualizar</button>
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
    </section>
  `
})
export class InventoryPageComponent implements OnInit {
  readonly products = signal<ProductItem[]>([]);
  readonly summary = signal<InventorySummaryItem[]>([]);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly info = signal('');
  readonly lastMovement = signal<InventoryMovementResult | null>(null);

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

  constructor(private readonly api: ApiService, private readonly fb: FormBuilder) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.api.getProducts().subscribe((items) => this.products.set(items));
    this.api.getInventorySummary().subscribe((items) => this.summary.set(items));
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
