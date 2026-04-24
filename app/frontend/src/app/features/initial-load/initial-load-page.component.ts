import { Component, signal } from '@angular/core';
import { CommonModule, JsonPipe } from '@angular/common';
import { ApiService } from '../../core/services/api.service';

@Component({
  standalone: true,
  imports: [CommonModule, JsonPipe],
  template: `
    <section class="card">
      <h1 style="margin-top:0;">Carga inicial</h1>
      <p>Este scaffold deja el preview y la confirmación fuerte preparados. El parser CSV y la aplicación transaccional real quedan para el siguiente slice.</p>
      <button (click)="preview()">Generar preview demo</button>
      <div *ngIf="result()" style="margin-top:16px;">
        <p><strong>LoadId:</strong> {{ result()?.loadId }}</p>
        <p><strong>Token de confirmación:</strong> {{ result()?.confirmationToken }}</p>
        <pre>{{ result()?.summary | json }}</pre>
      </div>
    </section>
  `
})
export class InitialLoadPageComponent {
  readonly result = signal<{ loadId: string; confirmationToken: string; summary: unknown } | null>(null);

  constructor(private readonly api: ApiService) {}

  preview() {
    this.api.previewInitialLoad().subscribe((response) => this.result.set(response));
  }
}
