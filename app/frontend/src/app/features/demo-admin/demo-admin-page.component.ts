import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="card grid">
      <div>
        <h1 style="margin-top:0;">Demo / Admin</h1>
        <p>Reset queda protegido por entorno Demo y confirmación exacta <code>RESET DEMO</code>.</p>
      </div>
      <div class="grid grid-3">
        <article class="card">
          <h3>Status</h3>
          <pre>{{ status() | json }}</pre>
        </article>
        <article class="card">
          <h3>Seed demo</h3>
          <p>Permite rehidratar datos demo controlados.</p>
          <button (click)="seed()">Ejecutar seed</button>
        </article>
        <article class="card">
          <h3>Auditoría</h3>
          <p>La API registra cada reset en <code>DemoResetAudits</code>.</p>
        </article>
      </div>
    </section>
  `
})
export class DemoAdminPageComponent implements OnInit {
  readonly status = signal<unknown>(null);

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.reload();
  }

  seed() {
    this.api.triggerDemoSeed().subscribe(() => this.reload());
  }

  reload() {
    this.api.getDemoStatus().subscribe((status) => this.status.set(status));
  }
}
