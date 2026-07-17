namespace Com.FPTU.Prn232SE1819.Api.Services.Common;

public static class RoleRanks
{
    private static readonly Dictionary<string, int> Ranks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customer"] = 1,
        ["Sales"] = 2,
        ["Manager"] = 3,
        ["Admin"] = 4,
    };

    public static int Get(string? roleName)
        => roleName != null && Ranks.TryGetValue(roleName, out var r) ? r : 0;

    public static bool CanManage(string actorRole, string targetRole)
        => Get(actorRole) > Get(targetRole);
}
