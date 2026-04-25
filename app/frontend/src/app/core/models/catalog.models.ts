export interface ProductItem {
  id: string;
  internalKey: string;
  primaryCode?: string | null;
  description: string;
  brand?: string | null;
  stock: number;
  salePrice?: number | null;
  requiresReview: boolean;
}

