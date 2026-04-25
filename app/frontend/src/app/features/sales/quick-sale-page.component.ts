import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { ProductItem } from '../../core/models/catalog.models';
import { QuickSaleResponse, SaleListItem } from '../../core/models/sales.models';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, DatePipe],
  template: `
    <section class="card" style="display:grid;gap:16px;">
      <div>
        <h1 style="margin:0;">Venta rápida</h1>
        <p style="margin:8px 0 0;color:#6b7280;">Captura incremental con varias partidas, listado reciente y cancelación segura.</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" style="display:grid;gap:16px;">
        <div formArrayName="items" style="display:grid;gap:12px;">
          <section *ngFor="let item of items.controls; let i = index" [formGroupName]="i" class="card" style="padding:12px;background:#f9fafb;display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;align-items:end;">
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
              <button type="button" class="secondary" (click)="useSuggestedPrice(i)" [disabled]="!selectedProduct(i)">Usar precio sugerido</button>
              <button type="button" class="secondary" (click)="removeItem(i)" [disabled]="items.length === 1 || saving()">Quitar partida</button>
              <span *ngIf="selectedProduct(i) as product" style="color:#6b7280;">Existencia actual: {{ product.stock }} · Precio vigente: {{ product.salePrice ?? 'sin precio' }}</span>
            </div>
          </section>
        </div>

        <div style="display:flex;gap:12px;align-items:center;flex-wrap:wrap;">
          <button type="button" class="secondary" (click)="addItem()" [disabled]="saving()">Agregar partida</button>
          <button type="submit" [disabled]="form.invalid || saving()">Registrar venta</button>
          <strong>Total capturado: {{ capturedTotal() | currency:'MXN':'symbol-narrow' }}</strong>
        </div>
      </form>

      <div *ngIf="error()" class="badge warn" style="width:max-content;">{{ error() }}</div>
      <div *ngIf="info()" class="badge ok" style="width:max-content;">{{ info() }}</div>

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

      <section class="card" style="display:grid;gap:12px;">
        <div style="display:flex;justify-content:space-between;gap:12px;align-items:center;flex-wrap:wrap;">
          <div>
            <h2 style="margin:0;">Ventas recientes</h2>
            <p style="margin:8px 0 0;color:#6b7280;">Listado básico de las últimas 50 ventas con cancelación segura.</p>
          </div>
          <button type="button" class="secondary" (click)="loadSales()" [disabled]="saving()">Actualizar</button>
        </div>

        <table *ngIf="sales().length; else noSales">
          <thead>
            <tr>
              <th>Folio</th>
              <th>Fecha</th>
              <th>Estatus</th>
              <th>Partidas</th>
              <th>Total</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let sale of sales()">
              <td>
                <strong>{{ sale.folio }}</strong>
                <div style="color:#6b7280;font-size:12px;">{{ sale.totalQuantity }} pzas/unid acumuladas</div>
              </td>
              <td>{{ sale.createdAt | date:'short' }}</td>
              <td>{{ sale.status }}</td>
              <td>
                {{ sale.itemCount }}
                <ul style="margin:6px 0 0 16px;padding:0;">
                  <li *ngFor="let item of sale.items">{{ item.description }} · {{ item.quantity }} x {{ item.unitPrice | currency:'MXN':'symbol-narrow' }}</li>
                </ul>
              </td>
              <td>{{ sale.total | currency:'MXN':'symbol-narrow' }}</td>
              <td>
                <button type="button" class="secondary" (click)="cancelSale(sale)" [disabled]="sale.status !== 'confirmed' || cancellingSaleId() === sale.saleId">
                  {{ cancellingSaleId() === sale.saleId ? 'Cancelando...' : 'Cancelar' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>

        <ng-template #noSales>
          <p style="margin:0;color:#6b7280;">Aún no hay ventas registradas.</p>
        </ng-template>
      </section>
    </section>
  `
})
export class QuickSalePageComponent implements OnInit {
  readonly products = signal<ProductItem[]>([]);
  readonly sales = signal<SaleListItem[]>([]);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly info = signal('');
  readonly lastSale = signal<QuickSaleResponse | null>(null);
  readonly cancellingSaleId = signal('');
  readonly capturedTotal = computed(() => this.items.controls.reduce((total, control) => {
    const quantity = Number(control.get('quantity')?.value ?? 0);
    const unitPrice = Number(control.get('unitPrice')?.value ?? 0);
    return total + (quantity * unitPrice);
  }, 0));

  readonly form = this.fb.group({
    items: this.fb.array([this.createItemGroup()])
  });

  constructor(private readonly fb: FormBuilder, private readonly api: ApiService) {}

  get items(): FormArray {
    return this.form.controls.items as FormArray;
  }

  ngOnInit(): void {
    this.loadProducts();
    this.loadSales();
  }

  selectedProduct(index: number): ProductItem | null {
    const productId = this.items.at(index).get('productId')?.value as string;
    return this.products().find(x => x.id === productId) ?? null;
  }

  addItem(): void {
    this.items.push(this.createItemGroup());
  }

  removeItem(index: number): void {
    if (this.items.length === 1) {
      return;
    }

    this.items.removeAt(index);
  }

  useSuggestedPrice(index: number): void {
    const product = this.selectedProduct(index);
    if (!product?.salePrice) {
      return;
    }

    this.items.at(index).get('unitPrice')?.setValue(product.salePrice);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const items = this.items.getRawValue().map((item) => ({
      productId: item.productId,
      quantity: Number(item.quantity),
      unitPrice: item.unitPrice ? Number(item.unitPrice) : null
    }));

    this.saving.set(true);
    this.error.set('');
    this.info.set('');

    this.api.createQuickSale(items).subscribe({
      next: (sale) => {
        this.lastSale.set(sale);
        this.info.set(`Venta ${sale.folio} registrada correctamente.`);
        this.form.setControl('items', this.fb.array([this.createItemGroup()]));
        this.loadProducts();
        this.loadSales();
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'No fue posible registrar la venta.');
        this.saving.set(false);
      },
      complete: () => this.saving.set(false)
    });
  }

  cancelSale(sale: SaleListItem): void {
    this.cancellingSaleId.set(sale.saleId);
    this.error.set('');
    this.info.set('');

    this.api.cancelSale(sale.saleId).subscribe({
      next: (result) => {
        this.info.set(`Venta ${result.folio} cancelada y existencias revertidas.`);
        this.loadProducts();
        this.loadSales();
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'No fue posible cancelar la venta.');
        this.cancellingSaleId.set('');
      },
      complete: () => this.cancellingSaleId.set('')
    });
  }

  loadSales(): void {
    this.api.getSales().subscribe((sales) => this.sales.set(sales));
  }

  private loadProducts(): void {
    this.api.getProducts().subscribe((products) => {
      this.products.set(products);
      this.items.controls.forEach((control) => {
        const productId = control.get('productId')?.value as string;
        if (!productId && products[0]) {
          control.get('productId')?.setValue(products[0].id);
          control.get('unitPrice')?.setValue(products[0].salePrice ?? 0);
          return;
        }

        const product = products.find(x => x.id === productId);
        if (product?.salePrice && !control.get('unitPrice')?.value) {
          control.get('unitPrice')?.setValue(product.salePrice);
        }
      });
    });
  }

  private createItemGroup() {
    const group = this.fb.nonNullable.group({
      productId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(0.01)]],
      unitPrice: [0, [Validators.required, Validators.min(0.01)]]
    });

    group.controls.productId.valueChanges.subscribe(() => {
      const product = this.products().find(x => x.id === group.controls.productId.value);
      if (product?.salePrice) {
        group.controls.unitPrice.setValue(product.salePrice);
      }
    });

    return group;
  }
}
