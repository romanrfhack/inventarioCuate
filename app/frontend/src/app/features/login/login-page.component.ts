import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="container" style="min-height:100vh;display:grid;place-items:center;">
      <section class="card" style="width:min(420px,100%);">
        <h1 style="margin-top:0;">Acceso demo</h1>
        <p>Usuarios sugeridos: admin.demo / Demo123! y operador.demo / Demo123!</p>
        <form [formGroup]="form" (ngSubmit)="submit()" class="grid">
          <label>
            Usuario
            <input formControlName="userName" />
          </label>
          <label>
            Contraseña
            <input type="password" formControlName="password" />
          </label>
          <button [disabled]="loading">Entrar</button>
          <p *ngIf="error" style="color:#b91c1c;">{{ error }}</p>
        </form>
      </section>
    </div>
  `
})
export class LoginPageComponent {
  readonly form = this.fb.nonNullable.group({
    userName: ['admin.demo', Validators.required],
    password: ['Demo123!', Validators.required]
  });
  loading = false;
  error = '';

  constructor(private readonly fb: FormBuilder, private readonly authService: AuthService, private readonly router: Router) {}

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    const { userName, password } = this.form.getRawValue();
    this.authService.login(userName, password).subscribe({
      next: () => this.router.navigateByUrl('/dashboard'),
      error: () => {
        this.loading = false;
        this.error = 'No fue posible iniciar sesión';
      },
      complete: () => {
        this.loading = false;
      }
    });
  }
}
