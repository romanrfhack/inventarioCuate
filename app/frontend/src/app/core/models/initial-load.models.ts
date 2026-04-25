export interface InitialLoadPreviewRow {
  sourceRow: number;
  code?: string | null;
  description: string;
  initialStock: number;
  rowStatus: string;
  reviewReason?: string | null;
}

export interface InitialLoadPreviewSummary {
  loadId: string;
  status: string;
  confirmationToken: string;
  totalRows: number;
  validRows: number;
  invalidRows: number;
  warningRows: number;
  rows: InitialLoadPreviewRow[];
}

export interface InitialLoadApplyResponse {
  loadId: string;
  status: string;
  createdProducts: number;
  matchedProducts: number;
  createdInventoryBalances: number;
  createdMovements: number;
  warningRows: number;
}

export interface InitialLoadListItem {
  loadId: string;
  fileName: string;
  status: string;
  loadType: string;
  createdAt: string;
  totalRows: number;
  validRows: number;
  invalidRows: number;
  warningRows: number;
}
