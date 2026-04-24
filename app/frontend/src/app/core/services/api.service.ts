import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InventorySummaryItem, ProductItem } from '../models/catalog.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly apiBase = 'http://localhost:5098/api';

  constructor(private readonly http: HttpClient) {}

  getProducts(): Observable<ProductItem[]> {
    return this.http.get<ProductItem[]>(`${this.apiBase}/catalog/products`);
  }

  getInventorySummary(): Observable<InventorySummaryItem[]> {
    return this.http.get<InventorySummaryItem[]>(`${this.apiBase}/inventory/summary`);
  }

  getDemoStatus() {
    return this.http.get<{ environment: string; allowReset: boolean; productCount: number; userCount: number; pendingInitialLoads: number }>(`${this.apiBase}/demo-admin/status`);
  }

  triggerDemoSeed() {
    return this.http.post(`${this.apiBase}/demo-admin/seed`, {});
  }

  previewInitialLoad() {
    return this.http.post<{ loadId: string; confirmationToken: string; summary: unknown }>(`${this.apiBase}/initial-load/preview`, {});
  }
}
