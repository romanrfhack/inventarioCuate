export interface SupplierCatalogProfile {
  key: string;
  supplierName: string;
  preferredSheet: string;
  candidateSheets: string[];
}

export interface SupplierCatalogImportRow {
  sourceRow: number;
  sourceSheet: string;
  supplierProductCode?: string | null;
  description: string;
  brand?: string | null;
  unit?: string | null;
  piecesPerBox?: number | null;
  compatibility?: string | null;
  line?: string | null;
  family?: string | null;
  subFamily?: string | null;
  category?: string | null;
  cost?: number | null;
  suggestedSalePrice?: number | null;
  priceLevelsJson?: string | null;
  supplierAvailability?: number | null;
  supplierStockText?: string | null;
  requiresRevision: boolean;
  revisionReason?: string | null;
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
  importProfile: string;
  fileName: string;
  status: string;
  confirmationToken: string;
  totalRows: number;
  matchCodigoRows: number;
  productoNuevoRows: number;
  datoIncompletoRows: number;
  requiereRevisionRows: number;
  appliedRows: number;
  rows: SupplierCatalogImportRow[];
}

export interface SupplierCatalogImportApplyResponse {
  batchId: string;
  status: string;
  updatedProducts: number;
  createdProducts: number;
  skippedRows: number;
  requiereRevisionRows: number;
}

export interface SupplierCatalogImportListItem {
  batchId: string;
  supplierName: string;
  importProfile: string;
  fileName: string;
  status: string;
  createdAt: string;
  totalRows: number;
  matchCodigoRows: number;
  productoNuevoRows: number;
  datoIncompletoRows: number;
  requiereRevisionRows: number;
  appliedRows: number;
}
