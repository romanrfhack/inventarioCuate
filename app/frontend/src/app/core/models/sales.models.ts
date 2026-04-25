export interface QuickSaleRequestItem {
  productId: string;
  quantity: number;
  unitPrice?: number | null;
}

export interface QuickSaleResponseItem {
  productId: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  remainingStock: number;
}

export interface QuickSaleResponse {
  saleId: string;
  folio: string;
  total: number;
  createdAt: string;
  items: QuickSaleResponseItem[];
}

export interface SalesFilter {
  folio?: string;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
}

export interface SaleListItem {
  saleId: string;
  folio: string;
  status: string;
  total: number;
  createdAt: string;
  itemCount: number;
  totalQuantity: number;
  items: SaleListDetailItem[];
}

export interface SaleListDetailItem {
  productId: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface SaleDetail {
  saleId: string;
  folio: string;
  status: string;
  total: number;
  createdAt: string;
  itemCount: number;
  totalQuantity: number;
  items: SaleDetailItem[];
}

export interface SaleDetailItem {
  productId: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface CancelSaleResponse {
  saleId: string;
  folio: string;
  status: string;
  cancelledAt: string;
  items: CancelledSaleItem[];
}

export interface CancelledSaleItem {
  productId: string;
  description: string;
  restoredQuantity: number;
  resultingStock: number;
}
