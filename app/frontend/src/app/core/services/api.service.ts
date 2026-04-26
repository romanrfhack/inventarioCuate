import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProductItem } from '../models/catalog.models';
import { InventoryMovementDetail, InventoryMovementFilters, InventoryMovementListItem, InventoryMovementResult, InventorySummaryItem, RegisterInventoryAdjustmentRequest, RegisterInventoryEntryRequest } from '../models/inventory.models';
import { InitialLoadApplyResponse, InitialLoadListItem, InitialLoadPreviewSummary } from '../models/initial-load.models';
import { CancelSaleResponse, QuickSaleRequestItem, QuickSaleResponse, SaleDetail, SaleListItem, SalesFilter } from '../models/sales.models';
import { OperationsReport } from '../models/reports.models';
import { SupplierCatalogImportApplyResponse, SupplierCatalogImportListItem, SupplierCatalogImportPreview } from '../models/supplier-catalog.models';

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

  registerInventoryEntry(request: RegisterInventoryEntryRequest): Observable<InventoryMovementResult> {
    return this.http.post<InventoryMovementResult>(`${this.apiBase}/inventory/entries`, request);
  }

  registerInventoryAdjustment(request: RegisterInventoryAdjustmentRequest): Observable<InventoryMovementResult> {
    return this.http.post<InventoryMovementResult>(`${this.apiBase}/inventory/adjustments`, request);
  }

  getInventoryMovements(filters?: InventoryMovementFilters): Observable<InventoryMovementListItem[]> {
    let params = new HttpParams();

    if (filters?.productId?.trim()) {
      params = params.set('productId', filters.productId.trim());
    }

    if (filters?.movementType?.trim()) {
      params = params.set('movementType', filters.movementType.trim());
    }

    if (filters?.reason?.trim()) {
      params = params.set('reason', filters.reason.trim());
    }

    if (filters?.dateFrom?.trim()) {
      params = params.set('dateFrom', filters.dateFrom.trim());
    }

    if (filters?.dateTo?.trim()) {
      params = params.set('dateTo', filters.dateTo.trim());
    }

    return this.http.get<InventoryMovementListItem[]>(`${this.apiBase}/inventory/movements`, { params });
  }

  getInventoryMovementDetail(movementId: string): Observable<InventoryMovementDetail> {
    return this.http.get<InventoryMovementDetail>(`${this.apiBase}/inventory/movements/${movementId}`);
  }

  getDemoStatus() {
    return this.http.get<{ environment: string; allowReset: boolean; productCount: number; userCount: number; pendingInitialLoads: number }>(`${this.apiBase}/demo-admin/status`);
  }

  triggerDemoSeed() {
    return this.http.post(`${this.apiBase}/demo-admin/seed`, {});
  }

  previewInitialLoad(fileName: string, csvContent: string): Observable<InitialLoadPreviewSummary> {
    return this.http.post<InitialLoadPreviewSummary>(`${this.apiBase}/initial-load/preview`, { fileName, csvContent });
  }

  applyInitialLoad(loadId: string, confirmationToken: string): Observable<InitialLoadApplyResponse> {
    return this.http.post<InitialLoadApplyResponse>(`${this.apiBase}/initial-load/apply/${loadId}`, { confirmationToken });
  }

  getInitialLoads(): Observable<InitialLoadListItem[]> {
    return this.http.get<InitialLoadListItem[]>(`${this.apiBase}/initial-load`);
  }

  getInitialLoadDetail(loadId: string): Observable<InitialLoadPreviewSummary> {
    return this.http.get<InitialLoadPreviewSummary>(`${this.apiBase}/initial-load/${loadId}`);
  }

  createQuickSale(items: QuickSaleRequestItem[]): Observable<QuickSaleResponse> {
    return this.http.post<QuickSaleResponse>(`${this.apiBase}/sales/quick`, { items });
  }

  getSales(filters?: SalesFilter): Observable<SaleListItem[]> {
    let params = new HttpParams();

    if (filters?.folio?.trim()) {
      params = params.set('folio', filters.folio.trim());
    }

    if (filters?.status?.trim()) {
      params = params.set('status', filters.status.trim());
    }

    if (filters?.dateFrom?.trim()) {
      params = params.set('dateFrom', filters.dateFrom.trim());
    }

    if (filters?.dateTo?.trim()) {
      params = params.set('dateTo', filters.dateTo.trim());
    }

    return this.http.get<SaleListItem[]>(`${this.apiBase}/sales`, { params });
  }

  getSaleDetail(saleId: string): Observable<SaleDetail> {
    return this.http.get<SaleDetail>(`${this.apiBase}/sales/${saleId}`);
  }

  cancelSale(saleId: string): Observable<CancelSaleResponse> {
    return this.http.post<CancelSaleResponse>(`${this.apiBase}/sales/${saleId}/cancel`, {});
  }

  getOperationsReport(): Observable<OperationsReport> {
    return this.http.get<OperationsReport>(`${this.apiBase}/reports/operations`);
  }

  previewSupplierCatalogImport(supplierName: string, fileName: string, csvContent: string): Observable<SupplierCatalogImportPreview> {
    return this.http.post<SupplierCatalogImportPreview>(`${this.apiBase}/supplier-catalog-imports/preview`, { supplierName, fileName, csvContent });
  }

  applySupplierCatalogImport(batchId: string, confirmationToken: string): Observable<SupplierCatalogImportApplyResponse> {
    return this.http.post<SupplierCatalogImportApplyResponse>(`${this.apiBase}/supplier-catalog-imports/${batchId}/apply`, { confirmationToken });
  }

  getSupplierCatalogImports(): Observable<SupplierCatalogImportListItem[]> {
    return this.http.get<SupplierCatalogImportListItem[]>(`${this.apiBase}/supplier-catalog-imports`);
  }

  getSupplierCatalogImportDetail(batchId: string): Observable<SupplierCatalogImportPreview> {
    return this.http.get<SupplierCatalogImportPreview>(`${this.apiBase}/supplier-catalog-imports/${batchId}`);
  }
}
