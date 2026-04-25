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
