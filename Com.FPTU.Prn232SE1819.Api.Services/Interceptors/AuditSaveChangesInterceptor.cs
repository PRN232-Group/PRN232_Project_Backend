using System.Globalization;
using System.Security.Claims;
using System.Text;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Com.FPTU.Prn232SE1819.Api.Services.Interceptors;

/// <summary>
/// Ghi SystemLog cho hành vi nghiệp vụ chính.
/// Bỏ bảng con / cascade (concept children, OrderItem, ProductSpec, …)
/// và bỏ “đổi giả” do format số (32,00 → 32).
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    /// <summary>Không audit trực tiếp — luôn đi kèm entity cha.</summary>
    private static readonly HashSet<string> SkipEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(SystemLog),
        nameof(EmailOtp),
        // Product
        nameof(ProductSpec),
        // Concept children (xóa/sửa concept → cascade nhiều dòng)
        nameof(InteriorDesignImage),
        nameof(InteriorDesignHighlight),
        nameof(InteriorDesignSpec),
        nameof(InteriorDesignMaterial),
        nameof(InteriorDesignPackage),
        nameof(InteriorDesignProduct),
        // Order / quotation lines
        nameof(OrderItem),
        nameof(QuotationProduct),
        nameof(QuotationRequestProduct),
        nameof(DesignRequestProduct),
        nameof(DesignRequestAttachment),
        // Permission rows (đã có log nghiệp vụ UPDATE_PERMISSIONS)
        nameof(RolePermission),
    };

    private static readonly HashSet<string> RedactedProps = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash",
        "ResetToken",
        "OtpCode",
        "OtpHash",
    };

    private static readonly HashSet<string> IgnoredChangeProps = new(StringComparer.OrdinalIgnoreCase)
    {
        "UpdatedAt",
        "CreatedAt",
    };

    private readonly IHttpContextAccessor _http;

    public AuditSaveChangesInterceptor(IHttpContextAccessor http) => _http = http;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AppendAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AppendAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendAuditLogs(DbContext? context)
    {
        if (context is null) return;

        var actorUserId = ResolveActorUserId();
        var now = VnDateTime.Now;

        var changed = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var entries = changed.Where(e => !ShouldSkip(e.Metadata.ClrType.Name)).ToList();

        // Chỉ đụng bảng con (ReplaceChildren SaveChanges riêng) → 1 log cha tổng hợp
        if (entries.Count == 0)
        {
            TryLogParentFromChildrenOnly(context, changed, actorUserId, now);
            return;
        }

        foreach (var entry in entries)
        {
            var detail = BuildDetail(entry);
            // Modified nhưng không có field thật sự đổi → bỏ (tránh log rác)
            if (entry.State == EntityState.Modified && detail is null)
                continue;

            var entityName = entry.Metadata.ClrType.Name;
            var action = entry.State switch
            {
                EntityState.Added => $"CREATE_{ToActionSuffix(entityName)}",
                EntityState.Deleted => $"DELETE_{ToActionSuffix(entityName)}",
                _ => $"UPDATE_{ToActionSuffix(entityName)}",
            };

            // Gắn tóm tắt số dòng con bị đụng trong cùng SaveChanges (không tạo log riêng)
            if (entry.State is EntityState.Deleted or EntityState.Modified or EntityState.Added)
            {
                var childHint = SummarizeRelatedChildren(context, entityName, entry);
                if (!string.IsNullOrEmpty(childHint))
                    detail = string.IsNullOrWhiteSpace(detail)
                        ? childHint
                        : $"{detail} · {childHint}";
            }

            context.Set<SystemLog>().Add(new SystemLog
            {
                ActorUserId = actorUserId,
                Action = action,
                Entity = entityName,
                EntityId = GetEntityId(entry),
                Detail = detail,
                CreatedAt = now,
            });
        }
    }

    private static bool ShouldSkip(string clrName)
    {
        if (SkipEntities.Contains(clrName)) return true;
        // Concept children: InteriorDesignImage / Spec / … (không gồm InteriorDesign)
        if (clrName.StartsWith("InteriorDesign", StringComparison.Ordinal)
            && !string.Equals(clrName, nameof(InteriorDesign), StringComparison.Ordinal))
            return true;
        return false;
    }

    /// <summary>
    /// Khi SaveChanges chỉ còn child (sau khi parent đã Unchanged hoặc create 2-phase):
    /// ghi 1 dòng UPDATE_INTERIOR_DESIGN / UPDATE_PRODUCT thay vì im lặng hoặc spam.
    /// </summary>
    private static void TryLogParentFromChildrenOnly(
        DbContext context,
        List<EntityEntry> changed,
        int? actorUserId,
        DateTime now)
    {
        var designChildren = changed
            .Where(e =>
            {
                var n = e.Metadata.ClrType.Name;
                return n.StartsWith("InteriorDesign", StringComparison.Ordinal)
                    && !string.Equals(n, nameof(InteriorDesign), StringComparison.Ordinal);
            })
            .ToList();

        if (designChildren.Count > 0)
        {
            var designId = designChildren
                .Select(e => e.Property("InteriorDesignId").CurrentValue
                    ?? e.Property("InteriorDesignId").OriginalValue)
                .OfType<object>()
                .Select(v => Convert.ToInt32(v, CultureInfo.InvariantCulture))
                .FirstOrDefault();

            var added = designChildren.Count(c => c.State == EntityState.Added);
            var deleted = designChildren.Count(c => c.State == EntityState.Deleted);
            var modified = designChildren.Count(c => c.State == EntityState.Modified);
            var parts = new List<string>();
            if (added > 0) parts.Add($"+{added} dòng con");
            if (deleted > 0) parts.Add($"−{deleted} dòng con");
            if (modified > 0) parts.Add($"~{modified} dòng con");

            context.Set<SystemLog>().Add(new SystemLog
            {
                ActorUserId = actorUserId,
                Action = "UPDATE_INTERIOR_DESIGN",
                Entity = nameof(InteriorDesign),
                EntityId = designId > 0 ? designId.ToString(CultureInfo.InvariantCulture) : null,
                Detail = parts.Count == 0
                    ? "Cập nhật bảng con concept."
                    : string.Join(", ", parts),
                CreatedAt = now,
            });
            return;
        }

        var specs = changed
            .Where(e => e.Metadata.ClrType.Name == nameof(ProductSpec))
            .ToList();
        if (specs.Count == 0) return;

        var productId = specs
            .Select(e => e.Property("ProductId").CurrentValue
                ?? e.Property("ProductId").OriginalValue)
            .OfType<object>()
            .Select(v => Convert.ToInt32(v, CultureInfo.InvariantCulture))
            .FirstOrDefault();

        context.Set<SystemLog>().Add(new SystemLog
        {
            ActorUserId = actorUserId,
            Action = "UPDATE_PRODUCT",
            Entity = nameof(Product),
            EntityId = productId > 0 ? productId.ToString(CultureInfo.InvariantCulture) : null,
            Detail = $"~{specs.Count} thông số SP",
            CreatedAt = now,
        });
    }

    /// <summary>
    /// Khi xóa/sửa InteriorDesign hoặc Product: ghi 1 dòng kèm “(+N bảng con)” thay vì N log.
    /// </summary>
    private static string? SummarizeRelatedChildren(DbContext context, string parentName, EntityEntry parent)
    {
        IEnumerable<EntityEntry> children;
        if (parentName == nameof(InteriorDesign))
        {
            children = context.ChangeTracker.Entries()
                .Where(e =>
                    e != parent
                    && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                    && e.Metadata.ClrType.Name.StartsWith("InteriorDesign", StringComparison.Ordinal)
                    && e.Metadata.ClrType.Name != nameof(InteriorDesign));
        }
        else if (parentName == nameof(Product))
        {
            children = context.ChangeTracker.Entries()
                .Where(e =>
                    e != parent
                    && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                    && e.Metadata.ClrType.Name == nameof(ProductSpec));
        }
        else if (parentName == nameof(Order))
        {
            children = context.ChangeTracker.Entries()
                .Where(e =>
                    e != parent
                    && e.State is EntityState.Added or EntityState.Deleted
                    && e.Metadata.ClrType.Name == nameof(OrderItem));
        }
        else
        {
            return null;
        }

        var list = children.ToList();
        if (list.Count == 0) return null;

        var added = list.Count(c => c.State == EntityState.Added);
        var deleted = list.Count(c => c.State == EntityState.Deleted);
        var modified = list.Count(c => c.State == EntityState.Modified);
        var parts = new List<string>();
        if (added > 0) parts.Add($"+{added} dòng con");
        if (deleted > 0) parts.Add($"−{deleted} dòng con");
        if (modified > 0) parts.Add($"~{modified} dòng con");
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private int? ResolveActorUserId()
    {
        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return null;

        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        return int.TryParse(id, out var uid) ? uid : null;
    }

    private static string ToActionSuffix(string entityName)
    {
        var sb = new StringBuilder(entityName.Length + 4);
        for (var i = 0; i < entityName.Length; i++)
        {
            var c = entityName[i];
            if (i > 0 && char.IsUpper(c)) sb.Append('_');
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    private static string? GetEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return null;

        var parts = key.Properties.Select(p =>
        {
            var prop = entry.Property(p.Name);
            var val = entry.State == EntityState.Deleted
                ? prop.OriginalValue
                : prop.CurrentValue;
            return val?.ToString() ?? "";
        });
        var id = string.Join(",", parts);
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }

    private static string? BuildDetail(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
            return $"Created {entry.Metadata.ClrType.Name}.";

        if (entry.State == EntityState.Deleted)
            return $"Deleted {entry.Metadata.ClrType.Name}.";

        var changes = new List<string>();
        foreach (var prop in entry.Properties)
        {
            if (!prop.IsModified) continue;
            if (IgnoredChangeProps.Contains(prop.Metadata.Name)) continue;

            var name = prop.Metadata.Name;
            if (RedactedProps.Contains(name))
            {
                changes.Add($"{name}: [redacted]");
                continue;
            }

            if (ValuesEquivalent(prop.OriginalValue, prop.CurrentValue))
                continue;

            var from = FormatValue(prop.OriginalValue);
            var to = FormatValue(prop.CurrentValue);
            changes.Add($"{name}: {from} → {to}");
        }

        return changes.Count == 0 ? null : string.Join("; ", changes);
    }

    private static bool ValuesEquivalent(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Equals(b)) return true;

        // 32.00m vs 32 → coi như không đổi
        if (TryToDecimal(a, out var da) && TryToDecimal(b, out var db))
            return da == db;

        if (a is bool ba && b is bool bb) return ba == bb;

        var sa = Convert.ToString(a, CultureInfo.InvariantCulture);
        var sb = Convert.ToString(b, CultureInfo.InvariantCulture);
        return string.Equals(sa, sb, StringComparison.Ordinal);
    }

    private static bool TryToDecimal(object value, out decimal number)
    {
        switch (value)
        {
            case decimal d:
                number = d;
                return true;
            case double dbl:
                number = (decimal)dbl;
                return true;
            case float f:
                number = (decimal)f;
                return true;
            case int i:
                number = i;
                return true;
            case long l:
                number = l;
                return true;
            case short s:
                number = s;
                return true;
            case byte by:
                number = by;
                return true;
            default:
                return decimal.TryParse(
                    Convert.ToString(value, CultureInfo.InvariantCulture),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out number);
        }
    }

    private static string FormatValue(object? value)
    {
        if (value is null) return "null";
        if (value is string s)
            return s.Length > 80 ? s[..80] + "…" : s;
        if (value is DateTime dt)
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        if (value is IFormattable f)
            return f.ToString(null, CultureInfo.InvariantCulture) ?? "null";
        return value.ToString() ?? "null";
    }
}
