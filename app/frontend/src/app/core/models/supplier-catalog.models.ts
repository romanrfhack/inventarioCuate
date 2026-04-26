export interface SupplierCatalogImportRow {
  sourceRow: number;
  supplierProductCode?: string | null;
  description: string;
  brand?: string | null;
  cost?: number | null;
  suggestedSalePrice?: number | null;
  matchType: string;
  actionType: string;
  rowStatus: string;
  matchedProductId?: string | null;
  proposedCost?: number | null;
  proposedSalePrice?: number | null;
  applySelected: boolean;
  reviewReason?: string | null;
}

export interface SupplierCatalogImportPreview {
  batchId: string;
  supplierName: string;
  status: string;
  confirmationToken: string;
  totalRows: number;
  readyRows: number;
  warningRows: number;
  conflictRows: number;
  newProducts: number;
  matchedProducts: number;
  rows: SupplierCatalogImportRow[];
}

export interface SupplierCatalogImportApplyResponse {
  batchId: string;
  status: string;
  updatedProducts: number;
  createdProducts: number;
  skippedRows: number;
  conflictRows: number;
}

export interface SupplierCatalogImportListItem {
  batchId: string;
  supplierName: string;
  fileName: string;
  status: string;
  createdAt: string;
  totalRows: number;
  readyRows: number;
  warningRows: number;
  conflictRows: number;
  appliedRows: number;
}
