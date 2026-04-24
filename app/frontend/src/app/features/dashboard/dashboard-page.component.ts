import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="grid grid-3">
      <article class="card">
        <h3>Estado técnico</h3>
        <p>Scaffold listo para slices de catálogo, inventario y carga inicial.</p>
      </article>
      <article class="card">
        <h3>Productos demo</h3>
        <strong>{{ demoStatus()?.productCount ?? '...' }}</strong>
      </article>
      <article class="card">
        <h3>Cargas pendientes</h3>
        <strong>{{ demoStatus()?.pendingInitialLoads ?? '...' }}</strong>
      </article>
    </section>
    <section class="card" style="margin-top:16px;">
      <h2 style="margin-top:0;">Entorno</h2>
      <pre>{{ demoStatus() | json }}</pre>
    </section>
  `
})
export class DashboardPageComponent implements OnInit {
  readonly demoStatus = signal<{ environment: string; allowReset: boolean; productCount: number; userCount: number; pendingInitialLoads: number } | null>(null);

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.api.getDemoStatus().subscribe((status) => this.demoStatus.set(status));
  }
}
