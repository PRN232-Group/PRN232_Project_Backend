using Com.FPTU.Prn232SE1819.Api.Services.Common;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class RoleRanksTests
{
    [Theory]
    [InlineData("Customer", 1)]
    [InlineData("Sales", 2)]
    [InlineData("Manager", 3)]
    [InlineData("Admin", 4)]
    [InlineData("Production", 0)] // role đã bỏ
    [InlineData(null, 0)]
    public void Get_returns_expected_rank(string? role, int expected)
        => Assert.Equal(expected, RoleRanks.Get(role));

    [Theory]
    [InlineData("Admin", "Manager", true)]
    [InlineData("Admin", "Customer", true)]
    [InlineData("Manager", "Sales", true)]
    [InlineData("Sales", "Customer", true)]
    [InlineData("Customer", "Sales", false)]
    [InlineData("Manager", "Admin", false)]
    [InlineData("Sales", "Sales", false)]
    public void CanManage_hierarchy(string actor, string target, bool expected)
        => Assert.Equal(expected, RoleRanks.CanManage(actor, target));
}
