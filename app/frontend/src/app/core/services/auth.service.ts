import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { LoginResponse } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'cuate.auth';
  readonly session = signal<LoginResponse['user'] | null>(this.readUser());

  constructor(private readonly http: HttpClient) {}

  login(userName: string, password: string) {
    return this.http.post<LoginResponse>('http://localhost:5098/api/auth/login', { userName, password }).pipe(
      tap((response) => {
        localStorage.setItem(this.storageKey, JSON.stringify(response));
        this.session.set(response.user);
      })
    );
  }

  logout() {
    localStorage.removeItem(this.storageKey);
    this.session.set(null);
  }

  get token(): string | null {
    const raw = localStorage.getItem(this.storageKey);
    return raw ? (JSON.parse(raw) as LoginResponse).accessToken : null;
  }

  private readUser(): LoginResponse['user'] | null {
    const raw = localStorage.getItem(this.storageKey);
    return raw ? (JSON.parse(raw) as LoginResponse).user : null;
  }
}
