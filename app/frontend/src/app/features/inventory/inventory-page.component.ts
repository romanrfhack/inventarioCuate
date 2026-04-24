import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { InventorySummaryItem } from '../../core/models/catalog.models';

@Component({
  standalone: true,
  imports: [CommonModule, DatePipe],
  template: `
    <section class="card">
      <h1 style="margin-top:0;">Inventario actual</h1>
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
  `
})
export class InventoryPageComponent implements OnInit {
  readonly summary = signal<InventorySummaryItem[]>([]);

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.api.getInventorySummary().subscribe((items) => this.summary.set(items));
  }
}
