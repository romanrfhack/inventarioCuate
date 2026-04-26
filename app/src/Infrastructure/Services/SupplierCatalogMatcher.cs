using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Infrastructure.Persistence;

namespace RefaccionariaCuate.Infrastructure.Services;

public sealed class SupplierCatalogMatcher(ApplicationDbContext dbContext)
{
    public async Task MatchAsync(IEnumerable<Domain.Entities.SupplierCatalogImportDetail> details, CancellationToken cancellationToken)
    {
        var products = await dbContext.Products.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var detail in details)
        {
            if (string.Equals(detail.RowStatus, "invalid", StringComparison.Ordinal))
            {
                detail.MatchType = "invalid";
                detail.ActionType = "review";
                continue;
            }

            var reasons = SplitReasons(detail.ReviewReason);
            var codeMatches = !string.IsNullOrWhiteSpace(detail.SupplierProductCode)
                ? products.Where(x => x.PrimaryCode == detail.SupplierProductCode).ToList()
                : [];

            var normalizedDescription = Normalize(detail.Description);
            var normalizedBrand = Normalize(detail.Brand);
            var descriptionMatches = products.Where(x =>
                    Normalize(x.Description) == normalizedDescription &&
                    (string.IsNullOrWhiteSpace(normalizedBrand) || Normalize(x.Brand) == normalizedBrand))
                .ToList();

            if (codeMatches.Count > 1)
            {
                detail.MatchType = "ambiguous_code";
                detail.ActionType = "review";
                detail.RowStatus = "conflict";
                reasons.Add("codigo_coincide_con_multiples_productos");
            }
            else if (codeMatches.Count == 1)
            {
                var product = codeMatches[0];
                if (descriptionMatches.Count > 0 && descriptionMatches.All(x => x.Id != product.Id))
                {
                    detail.MatchType = "conflict";
                    detail.ActionType = "review";
                    detail.RowStatus = "conflict";
                    reasons.Add("codigo_y_descripcion_apuntan_a_productos_distintos");
                }
                else
                {
                    detail.MatchType = "existing_by_code";
                    detail.ActionType = HasAnyUpdate(detail, product) ? "update" : "noop";
                    detail.RowStatus = detail.ActionType == "noop" ? "warning" : "ready";
                    detail.MatchedProductId = product.Id;
                    detail.ProposedCost = detail.Cost ?? product.CurrentCost;
                    detail.ProposedSalePrice = detail.SuggestedSalePrice ?? product.CurrentSalePrice;
                    if (detail.ActionType == "noop")
                    {
                        reasons.Add("sin_cambios_en_costo_o_precio");
                    }
                }
            }
            else if (descriptionMatches.Count > 1)
            {
                detail.MatchType = "ambiguous_description";
                detail.ActionType = "review";
                detail.RowStatus = "conflict";
                reasons.Add("descripcion_ambigua");
            }
            else if (descriptionMatches.Count == 1)
            {
                var product = descriptionMatches[0];
                detail.MatchType = "existing_by_description";
                detail.ActionType = HasAnyUpdate(detail, product) ? "update" : "noop";
                detail.RowStatus = detail.ActionType == "noop" ? "warning" : "warning";
                detail.MatchedProductId = product.Id;
                detail.ProposedCost = detail.Cost ?? product.CurrentCost;
                detail.ProposedSalePrice = detail.SuggestedSalePrice ?? product.CurrentSalePrice;
                reasons.Add("coincidencia_por_descripcion_requiere_revision");
                if (detail.ActionType == "noop")
                {
                    reasons.Add("sin_cambios_en_costo_o_precio");
                }
            }
            else
            {
                detail.MatchType = "new_product";
                detail.ActionType = "create";
                detail.RowStatus = string.Equals(detail.RowStatus, "warning", StringComparison.Ordinal) ? "warning" : "ready";
                detail.ProposedCost = detail.Cost;
                detail.ProposedSalePrice = detail.SuggestedSalePrice;
                reasons.Add("producto_nuevo");
            }

            detail.ReviewReason = reasons.Count == 0 ? null : string.Join(";", reasons.Distinct());
            detail.ApplySelected = detail.RowStatus is "ready" or "warning" && detail.ActionType is "update" or "create";
        }
    }

    private static bool HasAnyUpdate(Domain.Entities.SupplierCatalogImportDetail detail, Domain.Entities.Product product)
    {
        return (detail.Cost.HasValue && detail.Cost != product.CurrentCost)
            || (detail.SuggestedSalePrice.HasValue && detail.SuggestedSalePrice != product.CurrentSalePrice);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private static List<string> SplitReasons(string? reasons)
    {
        return string.IsNullOrWhiteSpace(reasons)
            ? []
            : reasons.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
