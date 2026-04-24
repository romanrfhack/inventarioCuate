import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { ProductItem } from '../../core/models/catalog.models';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="card">
      <h1 style="margin-top:0;">Catálogo base</h1>
      <table>
        <thead>
          <tr>
            <th>Clave</th>
            <th>Código</th>
            <th>Descripción</th>
            <th>Marca</th>
            <th>Existencia</th>
            <th>Precio</th>
            <th>Estado</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let product of products()">
            <td>{{ product.internalKey }}</td>
            <td>{{ product.primaryCode || '-' }}</td>
            <td>{{ product.description }}</td>
            <td>{{ product.brand || '-' }}</td>
            <td>{{ product.stock }}</td>
            <td>{{ product.salePrice ?? '-' }}</td>
            <td><span class="badge" [class.warn]="product.requiresReview" [class.ok]="!product.requiresReview">{{ product.requiresReview ? 'Revisar' : 'Ok' }}</span></td>
          </tr>
        </tbody>
      </table>
    </section>
  `
})
export class CatalogPageComponent implements OnInit {
  readonly products = signal<ProductItem[]>([]);

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.api.getProducts().subscribe((products) => this.products.set(products));
  }
}
