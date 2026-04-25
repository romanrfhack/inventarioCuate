import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { ProductItem } from '../../core/models/catalog.models';
import { QuickSaleResponse } from '../../core/models/sales.models';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, DatePipe],
  template: `
    <section class="card" style="display:grid;gap:16px;">
      <div>
        <h1 style="margin:0;">Venta rápida</h1>
        <p style="margin:8px 0 0;color:#6b7280;">Captura mínima para descontar inventario en tiempo real.</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" style="display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;align-items:end;">
        <label style="grid-column:span 2;display:grid;gap:6px;">
          <span>Producto</span>
          <select formControlName="productId">
            <option value="">Selecciona...</option>
            <option *ngFor="let product of products()" [value]="product.id">
              {{ product.description }} · stock {{ product.stock }} · {{ product.salePrice ?? 'sin precio' }}
            </option>
          </select>
        </label>

        <label style="display:grid;gap:6px;">
          <span>Cantidad</span>
          <input type="number" min="0.01" step="0.01" formControlName="quantity" />
        </label>

        <label style="display:grid;gap:6px;">
          <span>Precio unitario</span>
          <input type="number" min="0.01" step="0.01" formControlName="unitPrice" />
        </label>

        <div style="grid-column:1/-1;display:flex;gap:12px;align-items:center;flex-wrap:wrap;">
          <button type="submit" [disabled]="form.invalid || saving()">Registrar venta</button>
          <button type="button" class="secondary" (click)="useSuggestedPrice()" [disabled]="!selectedProduct()">Usar precio sugerido</button>
          <span *ngIf="selectedProduct() as product" style="color:#6b7280;">Existencia actual: {{ product.stock }} · Precio vigente: {{ product.salePrice ?? 'sin precio' }}</span>
        </div>
      </form>

      <div *ngIf="error()" class="badge warn" style="width:max-content;">{{ error() }}</div>

      <section *ngIf="lastSale() as sale" class="card" style="background:#f9fafb;">
        <h2 style="margin-top:0;">Última venta registrada</h2>
        <p style="margin:0 0 12px;"><strong>{{ sale.folio }}</strong> · {{ sale.createdAt | date:'short' }} · Total {{ sale.total | currency:'MXN':'symbol-narrow' }}</p>
        <table>
          <thead>
            <tr>
              <th>Producto</th>
              <th>Cantidad</th>
              <th>Precio</th>
              <th>Importe</th>
              <th>Stock restante</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of sale.items">
              <td>{{ item.description }}</td>
              <td>{{ item.quantity }}</td>
              <td>{{ item.unitPrice | currency:'MXN':'symbol-narrow' }}</td>
              <td>{{ item.lineTotal | currency:'MXN':'symbol-narrow' }}</td>
              <td>{{ item.remainingStock }}</td>
            </tr>
          </tbody>
        </table>
      </section>
    </section>
  `
})
export class QuickSalePageComponent implements OnInit {
  readonly products = signal<ProductItem[]>([]);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly lastSale = signal<QuickSaleResponse | null>(null);
  readonly selectedProduct = computed(() => this.products().find(x => x.id === this.form.controls.productId.value) ?? null);

  readonly form = this.fb.nonNullable.group({
    productId: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(0.01)]],
    unitPrice: [0, [Validators.required, Validators.min(0.01)]]
  });

  constructor(private readonly fb: FormBuilder, private readonly api: ApiService) {}

  ngOnInit(): void {
    this.loadProducts();
    this.form.controls.productId.valueChanges.subscribe(() => this.useSuggestedPrice());
  }

  useSuggestedPrice(): void {
    const product = this.selectedProduct();
    if (!product?.salePrice) {
      return;
    }

    this.form.controls.unitPrice.setValue(product.salePrice);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { productId, quantity, unitPrice } = this.form.getRawValue();
    this.saving.set(true);
    this.error.set('');

    this.api.createQuickSale([{ productId, quantity, unitPrice }]).subscribe({
      next: (sale) => {
        this.lastSale.set(sale);
        this.form.controls.quantity.setValue(1);
        this.loadProducts(productId);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'No fue posible registrar la venta.');
      },
      complete: () => this.saving.set(false)
    });
  }

  private loadProducts(selectedProductId?: string): void {
    this.api.getProducts().subscribe((products) => {
      this.products.set(products);
      const selectedId = selectedProductId ?? this.form.controls.productId.value ?? products[0]?.id ?? '';
      this.form.controls.productId.setValue(selectedId);
      const product = products.find(x => x.id === selectedId);
      if (product?.salePrice) {
        this.form.controls.unitPrice.setValue(product.salePrice);
      }
    });
  }
}
