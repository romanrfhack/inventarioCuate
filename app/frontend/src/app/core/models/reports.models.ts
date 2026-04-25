export interface OperationsReport {
  summary: OperationsReportSummary;
  inventory: OperationsInventoryItem[];
  recentSales: OperationsRecentSale[];
  productAnomalies: OperationsProductAnomaly[];
  profitableProducts: OperationsProfitableProduct[];
}

export interface OperationsReportSummary {
  totalProducts: number;
  productsWithStock: number;
  productsWithoutStock: number;
  productsWithNegativeStock: number;
  totalStockUnits: number;
  inventoryCostValue: number;
  inventoryRetailValue: number;
  confirmedSalesCount: number;
  confirmedSalesTotal: number;
  confirmedSalesGrossProfit: number;
  latestSaleDate?: string | null;
}

export interface OperationsInventoryItem {
  productId: string;
  internalKey: string;
  primaryCode?: string | null;
  description: string;
  brand?: string | null;
  currentStock: number;
  currentCost?: number | null;
  currentSalePrice?: number | null;
  estimatedCostValue: number;
  estimatedRetailValue: number;
  requiresReview: boolean;
  flags: string[];
  updatedAt: string;
}

export interface OperationsRecentSale {
  saleId: string;
  folio: string;
  status: string;
  total: number;
  totalQuantity: number;
  itemCount: number;
  grossProfit?: number | null;
  createdAt: string;
}

export interface OperationsProductAnomaly {
  productId: string;
  internalKey: string;
  description: string;
  currentStock: number;
  requiresImmediateAttention: boolean;
  reasons: string[];
}

export interface OperationsProfitableProduct {
  productId: string;
  internalKey: string;
  description: string;
  quantitySold: number;
  salesAmount: number;
  grossProfit: number;
  saleLines: number;
}
