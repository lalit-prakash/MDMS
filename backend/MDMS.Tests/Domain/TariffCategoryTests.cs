using MDMS.Domain.Entities;

namespace MDMS.Tests.Domain;

public class TariffCategoryTests
{
    [Fact]
    public void Constructor_BlankCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TariffCategory("  ", "Domestic"));
    }

    [Fact]
    public void Constructor_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TariffCategory("DOM", "  "));
    }

    [Fact]
    public void Rename_UpdatesNameAndDescription()
    {
        var category = new TariffCategory("DOM", "Domestic", "Household consumers");

        category.Rename("Domestic (Revised)", "Updated description");

        Assert.Equal("Domestic (Revised)", category.Name);
        Assert.Equal("Updated description", category.Description);
        Assert.Equal("DOM", category.Code); // code is immutable once set
    }
}
