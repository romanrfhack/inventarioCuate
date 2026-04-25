export interface InventorySummaryItem {
  productId: string;
  description: string;
  currentStock: number;
  updatedAt: string;
}

export interface RegisterInventoryEntryRequest {
  productId: string;
  quantity: number;
  reason: string;
}

export interface RegisterInventoryAdjustmentRequest {
  productId: string;
  quantityDelta: number;
  reason: string;
}

export interface InventoryMovementResult {
  movementId: string;
  productId: string;
  description: string;
  movementType: string;
  quantity: number;
  resultingStock: number;
  reason: string;
  createdAt: string;
}
