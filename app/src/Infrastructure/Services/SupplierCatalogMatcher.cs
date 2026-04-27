using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Domain.Entities;
using RefaccionariaCuate.Infrastructure.Persistence;

namespace RefaccionariaCuate.Infrastructure.Services;

public sealed class SupplierCatalogMatcher(ApplicationDbContext dbContext)
{
    public async Task MatchAsync(IEnumerable<SupplierCatalogImportDetail> details, CancellationToken cancellationToken)
    {
        var products = await dbContext.Products.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var detail in details)
        {
            var reasons = SplitReasons(detail.RevisionReason);
            var normalizedCode = Normalize(detail.SupplierProductCode);
            var normalizedDescription = Normalize(detail.Description);

            if (string.IsNullOrWhiteSpace(detail.Description))
            {
                Mark(detail, "dato_incompleto", "review", "requiere_revision", [.. reasons, "descripcion_obligatoria"]);
                continue;
            }

            var codeMatches = string.IsNullOrWhiteSpace(normalizedCode)
                ? []
                : products.Where(x => Normalize(x.PrimaryCode) == normalizedCode).ToList();
            var descriptionMatches = products.Where(x => Normalize(x.Description) == normalizedDescription).ToList();

            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                detail.RequiresRevision = true;
                reasons.Add("sin_codigo");
            }

            if (codeMatches.Count > 1)
            {
                Mark(detail, "conflicto_codigo", "review", "requiere_revision", [.. reasons, "codigo_repetido_en_catalogo_local"]);
                continue;
            }

            if (codeMatches.Count == 1)
            {
                var product = codeMatches[0];
                detail.MatchedProductId = product.Id;
                detail.ProposedCost = detail.Cost ?? product.CurrentCost;
                detail.ProposedSalePrice = detail.SuggestedSalePrice ?? product.CurrentSalePrice;

                if (descriptionMatches.Count == 1 && descriptionMatches[0].Id != product.Id)
                {
                    Mark(detail, "conflicto_codigo", "review", "requiere_revision", [.. reasons, "codigo_y_descripcion_apuntan_a_productos_distintos"]);
                    continue;
                }

                detail.ActionType = HasCatalogChanges(detail, product) ? "update" : "noop";
                detail.MatchType = "match_codigo";
                detail.RowStatus = detail.RequiresRevision ? "requiere_revision" : "match_codigo";
                detail.ReviewReason = JoinReasons([.. reasons, detail.RequiresRevision ? "match_claro_con_revision_manual" : null, detail.ActionType == "noop" ? "sin_cambios" : null]);
                detail.ApplySelected = !detail.RequiresRevision && detail.ActionType == "update";
                continue;
            }

            if (descriptionMatches.Count > 1)
            {
                Mark(detail, "requiere_revision", "review", "requiere_revision", [.. reasons, "descripcion_ambigua"]);
                continue;
            }

            if (!CanCreate(detail))
            {
                Mark(detail, "dato_incompleto", "review", "dato_incompleto", [.. reasons, "faltan_datos_para_alta_controlada"]);
                continue;
            }

            detail.MatchType = "producto_nuevo";
            detail.ActionType = detail.RequiresRevision ? "review" : "create";
            detail.RowStatus = detail.RequiresRevision ? "requiere_revision" : "producto_nuevo";
            detail.ProposedCost = detail.Cost;
            detail.ProposedSalePrice = detail.SuggestedSalePrice;
            detail.ReviewReason = JoinReasons([.. reasons, "producto_nuevo"]);
            detail.ApplySelected = !detail.RequiresRevision;
        }
    }

    private static bool CanCreate(SupplierCatalogImportDetail detail)
    {
        return !string.IsNullOrWhiteSpace(detail.SupplierProductCode)
            && !string.IsNullOrWhiteSpace(detail.Description)
            && (detail.Cost.HasValue || detail.SuggestedSalePrice.HasValue);
    }

    private static bool HasCatalogChanges(SupplierCatalogImportDetail detail, Product product)
    {
        return detail.Cost != product.CurrentCost
            || detail.SuggestedSalePrice != product.CurrentSalePrice
            || (string.IsNullOrWhiteSpace(product.Brand) && !string.IsNullOrWhiteSpace(detail.Brand))
            || (string.IsNullOrWhiteSpace(product.Unit) && !string.IsNullOrWhiteSpace(detail.Unit))
            || (!product.PiecesPerBox.HasValue && detail.PiecesPerBox.HasValue)
            || (string.IsNullOrWhiteSpace(product.Compatibility) && !string.IsNullOrWhiteSpace(detail.Compatibility))
            || (string.IsNullOrWhiteSpace(product.Line) && !string.IsNullOrWhiteSpace(detail.Line))
            || (string.IsNullOrWhiteSpace(product.Family) && !string.IsNullOrWhiteSpace(detail.Family))
            || (string.IsNullOrWhiteSpace(product.SubFamily) && !string.IsNullOrWhiteSpace(detail.SubFamily))
            || (string.IsNullOrWhiteSpace(product.Category) && !string.IsNullOrWhiteSpace(detail.Category));
    }

    private static void Mark(SupplierCatalogImportDetail detail, string matchType, string actionType, string rowStatus, List<string> reasons)
    {
        detail.MatchType = matchType;
        detail.ActionType = actionType;
        detail.RowStatus = rowStatus;
        detail.ApplySelected = false;
        detail.RequiresRevision = true;
        detail.ReviewReason = JoinReasons(reasons);
    }

    private static List<string> SplitReasons(string? reasons)
    {
        return string.IsNullOrWhiteSpace(reasons)
            ? []
            : reasons.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();
    }

    private static string? JoinReasons(IEnumerable<string?> reasons)
    {
        var clean = reasons.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).Distinct().ToList();
        return clean.Count == 0 ? null : string.Join(';', clean);
    }

    private static string? JoinReasons(params string?[] reasons)
    {
        return JoinReasons(reasons.AsEnumerable());
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    }
}
