namespace RefaccionariaCuate.Api.Contracts.Reports;

public sealed record OperationsReportResponse(
    OperationsReportSummaryResponse Summary,
    IReadOnlyCollection<OperationsInventoryItemResponse> Inventory,
    IReadOnlyCollection<OperationsRecentSaleResponse> RecentSales,
    IReadOnlyCollection<OperationsProductAnomalyResponse> ProductAnomalies,
    IReadOnlyCollection<OperationsProfitableProductResponse> ProfitableProducts);

public sealed record OperationsReportSummaryResponse(
    int TotalProducts,
    int ProductsWithStock,
    int ProductsWithoutStock,
    int ProductsWithNegativeStock,
    decimal TotalStockUnits,
    decimal InventoryCostValue,
    decimal InventoryRetailValue,
    int ConfirmedSalesCount,
    decimal ConfirmedSalesTotal,
    decimal ConfirmedSalesGrossProfit,
    DateOnly? LatestSaleDate);

public sealed record OperationsInventoryItemResponse(
    Guid ProductId,
    string InternalKey,
    string? PrimaryCode,
    string Description,
    string? Brand,
    decimal CurrentStock,
    decimal? CurrentCost,
    decimal? CurrentSalePrice,
    decimal EstimatedCostValue,
    decimal EstimatedRetailValue,
    bool RequiresReview,
    IReadOnlyCollection<string> Flags,
    DateTimeOffset UpdatedAt);

public sealed record OperationsRecentSaleResponse(
    Guid SaleId,
    string Folio,
    string Status,
    decimal Total,
    decimal TotalQuantity,
    int ItemCount,
    decimal? GrossProfit,
    DateTimeOffset CreatedAt);

public sealed record OperationsProductAnomalyResponse(
    Guid ProductId,
    string InternalKey,
    string Description,
    decimal CurrentStock,
    bool RequiresImmediateAttention,
    IReadOnlyCollection<string> Reasons);

public sealed record OperationsProfitableProductResponse(
    Guid ProductId,
    string InternalKey,
    string Description,
    decimal QuantitySold,
    decimal SalesAmount,
    decimal GrossProfit,
    int SaleLines);
