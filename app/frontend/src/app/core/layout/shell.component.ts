import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  standalone: true,
  selector: 'app-shell',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header style="background:#111827;color:white;">
      <div class="container" style="display:flex;align-items:center;justify-content:space-between;gap:16px;">
        <div>
          <div style="font-size:20px;font-weight:700;">Refaccionaria Cuate</div>
          <small>MVP operativo, demo controlada</small>
        </div>
        <div *ngIf="auth.session() as session" style="display:flex;align-items:center;gap:12px;">
          <span>{{ session.fullName }} · {{ session.role }}</span>
          <button class="secondary" (click)="logout()">Salir</button>
        </div>
      </div>
    </header>
    <div class="container" style="display:grid;grid-template-columns:220px 1fr;gap:24px;align-items:start;">
      <aside class="card">
        <nav style="display:grid;gap:8px;">
          <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
          <a routerLink="/catalogo" routerLinkActive="active">Catálogo</a>
          <a routerLink="/inventario" routerLinkActive="active">Inventario</a>
          <a routerLink="/reportes" routerLinkActive="active">Reportes</a>
          <a routerLink="/ventas" routerLinkActive="active">Venta rápida</a>
          <a routerLink="/carga-inicial" routerLinkActive="active">Carga inicial</a>
          <a routerLink="/catalogo-proveedor" routerLinkActive="active">Catálogo proveedor</a>
          <a routerLink="/demo-admin" routerLinkActive="active">Demo/Admin</a>
        </nav>
      </aside>
      <main style="min-width:0;">
        <router-outlet />
      </main>
    </div>
  `
})
export class ShellComponent {
  constructor(public readonly auth: AuthService) {}

  logout() {
    this.auth.logout();
  }
}
