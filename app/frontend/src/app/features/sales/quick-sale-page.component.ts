import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { ProductItem } from '../../core/models/catalog.models';
import { CancelSaleResponse, QuickSaleResponse, SaleDetail, SaleListItem, SalesFilter } from '../../core/models/sales.models';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, DatePipe],
  template: `
    <section class="card" style="display:grid;gap:16px;">
      <div>
        <h1 style="margin:0;">Venta rápida</h1>
        <p style="margin:8px 0 0;color:#6b7280;">Captura incremental con varias partidas, consulta detalle y cancelación segura.</p>
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

      <section *ngIf="lastCancellation() as cancelled" class="card" style="background:#fff7ed;display:grid;gap:10px;">
        <div>
          <h2 style="margin:0;">Última cancelación aplicada</h2>
          <p style="margin:8px 0 0;color:#9a3412;"><strong>{{ cancelled.folio }}</strong> · {{ cancelled.cancelledAt | date:'short' }} · existencias revertidas</p>
        </div>
        <ul style="margin:0;padding-left:18px;display:grid;gap:4px;">
          <li *ngFor="let item of cancelled.items">{{ item.description }} · regresó {{ item.restoredQuantity }} · stock actual {{ item.resultingStock }}</li>
        </ul>
      </section>

      <section class="card" style="display:grid;gap:12px;">
        <div style="display:flex;justify-content:space-between;gap:12px;align-items:center;flex-wrap:wrap;">
          <div>
            <h2 style="margin:0;">Ventas recientes</h2>
            <p style="margin:8px 0 0;color:#6b7280;">Listado básico con filtros, detalle bajo demanda y cancelación segura.</p>
          </div>
          <button type="button" class="secondary" (click)="loadSales()" [disabled]="saving()">Actualizar</button>
        </div>

        <form [formGroup]="filtersForm" (ngSubmit)="applyFilters()" style="display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:12px;align-items:end;">
          <label style="display:grid;gap:6px;">
            <span>Folio</span>
            <input type="text" formControlName="folio" placeholder="VTA-20260425" />
          </label>
          <label style="display:grid;gap:6px;">
            <span>Estatus</span>
            <select formControlName="status">
              <option value="">Todos</option>
              <option value="confirmed">Confirmada</option>
              <option value="cancelled">Cancelada</option>
            </select>
          </label>
          <label style="display:grid;gap:6px;">
            <span>Desde</span>
            <input type="date" formControlName="dateFrom" />
          </label>
          <label style="display:grid;gap:6px;">
            <span>Hasta</span>
            <input type="date" formControlName="dateTo" />
          </label>
          <div style="display:flex;gap:8px;flex-wrap:wrap;">
            <button type="submit" class="secondary">Filtrar</button>
            <button type="button" class="secondary" (click)="clearFilters()">Limpiar</button>
          </div>
        </form>

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
            <ng-container *ngFor="let sale of sales()">
              <tr>
                <td>
                  <strong>{{ sale.folio }}</strong>
                  <div style="color:#6b7280;font-size:12px;">{{ sale.totalQuantity }} pzas/unid acumuladas</div>
                </td>
                <td>{{ sale.createdAt | date:'short' }}</td>
                <td>
                  <span class="badge" [class.ok]="sale.status === 'confirmed'" [class.warn]="sale.status === 'cancelled'">{{ sale.status === 'confirmed' ? 'confirmada' : 'cancelada' }}</span>
                </td>
                <td>{{ sale.itemCount }}</td>
                <td>{{ sale.total | currency:'MXN':'symbol-narrow' }}</td>
                <td>
                  <div style="display:flex;gap:8px;flex-wrap:wrap;">
                    <button type="button" class="secondary" (click)="toggleSaleDetail(sale)">
                      {{ selectedSaleId() === sale.saleId ? 'Ocultar detalle' : 'Ver detalle' }}
                    </button>
                    <button type="button" class="secondary" (click)="cancelSale(sale)" [disabled]="sale.status !== 'confirmed' || cancellingSaleId() === sale.saleId">
                      {{ cancellingSaleId() === sale.saleId ? 'Cancelando...' : 'Cancelar' }}
                    </button>
                  </div>
                </td>
              </tr>
              <tr *ngIf="selectedSaleId() === sale.saleId">
                <td colspan="6" style="background:#f9fafb;">
                  <div *ngIf="loadingSaleDetail()" style="padding:12px 0;color:#6b7280;">Cargando detalle...</div>
                  <div *ngIf="!loadingSaleDetail() && selectedSaleDetail() as detail" style="padding:12px 0;display:grid;gap:12px;">
                    <div style="display:flex;justify-content:space-between;gap:12px;flex-wrap:wrap;">
                      <div>
                        <strong>{{ detail.folio }}</strong>
                        <div style="color:#6b7280;font-size:12px;">{{ detail.createdAt | date:'medium' }}</div>
                      </div>
                      <div style="text-align:right;">
                        <div>{{ detail.itemCount }} partidas</div>
                        <div style="color:#6b7280;font-size:12px;">{{ detail.totalQuantity }} unidades acumuladas</div>
                      </div>
                    </div>
                    <table>
                      <thead>
                        <tr>
                          <th>Producto</th>
                          <th>Cantidad</th>
                          <th>Precio</th>
                          <th>Importe</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr *ngFor="let item of detail.items">
                          <td>{{ item.description }}</td>
                          <td>{{ item.quantity }}</td>
                          <td>{{ item.unitPrice | currency:'MXN':'symbol-narrow' }}</td>
                          <td>{{ item.lineTotal | currency:'MXN':'symbol-narrow' }}</td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </td>
              </tr>
            </ng-container>
          </tbody>
        </table>

        <ng-template #noSales>
          <p style="margin:0;color:#6b7280;">No hay ventas para los filtros seleccionados.</p>
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
  readonly lastCancellation = signal<CancelSaleResponse | null>(null);
  readonly cancellingSaleId = signal('');
  readonly selectedSaleId = signal('');
  readonly selectedSaleDetail = signal<SaleDetail | null>(null);
  readonly loadingSaleDetail = signal(false);
  readonly capturedTotal = computed(() => this.items.controls.reduce((total, control) => {
    const quantity = Number(control.get('quantity')?.value ?? 0);
    const unitPrice = Number(control.get('unitPrice')?.value ?? 0);
    return total + (quantity * unitPrice);
  }, 0));

  readonly form = this.fb.group({
    items: this.fb.array([this.createItemGroup()])
  });

  readonly filtersForm = this.fb.nonNullable.group({
    folio: [''],
    status: [''],
    dateFrom: [''],
    dateTo: ['']
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
    this.lastCancellation.set(null);

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
        this.lastCancellation.set(result);
        this.info.set(`Venta ${result.folio} cancelada y existencias revertidas.`);
        if (this.selectedSaleId() === sale.saleId) {
          this.toggleSaleDetail(sale, true);
        }
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

  applyFilters(): void {
    this.loadSales();
  }

  clearFilters(): void {
    this.filtersForm.reset({
      folio: '',
      status: '',
      dateFrom: '',
      dateTo: ''
    });
    this.loadSales();
  }

  toggleSaleDetail(sale: SaleListItem, forceReload = false): void {
    if (!forceReload && this.selectedSaleId() === sale.saleId) {
      this.selectedSaleId.set('');
      this.selectedSaleDetail.set(null);
      return;
    }

    this.selectedSaleId.set(sale.saleId);
    this.loadingSaleDetail.set(true);
    this.selectedSaleDetail.set(null);

    this.api.getSaleDetail(sale.saleId).subscribe({
      next: (detail) => this.selectedSaleDetail.set(detail),
      error: (err) => {
        this.error.set(err?.error?.message ?? 'No fue posible consultar el detalle de la venta.');
        this.selectedSaleId.set('');
      },
      complete: () => this.loadingSaleDetail.set(false)
    });
  }

  loadSales(): void {
    this.api.getSales(this.buildFilters()).subscribe((sales) => {
      this.sales.set(sales);
      if (this.selectedSaleId() && !sales.some((item) => item.saleId === this.selectedSaleId())) {
        this.selectedSaleId.set('');
        this.selectedSaleDetail.set(null);
      }
    });
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

  private buildFilters(): SalesFilter {
    const raw = this.filtersForm.getRawValue();
    return {
      folio: raw.folio,
      status: raw.status,
      dateFrom: raw.dateFrom,
      dateTo: raw.dateTo
    };
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
