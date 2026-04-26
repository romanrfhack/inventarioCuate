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

export interface InventoryMovementFilters {
  productId?: string;
  movementType?: string;
  reason?: string;
  dateFrom?: string;
  dateTo?: string;
}

export interface InventoryMovementListItem {
  movementId: string;
  productId: string;
  description: string;
  movementType: string;
  quantity: number;
  resultingStock?: number | null;
  reason?: string | null;
  sourceType: string;
  sourceId?: string | null;
  createdAt: string;
}

export interface InventoryMovementDetail extends InventoryMovementListItem {
  userId?: string | null;
  shiftId?: string | null;
}
