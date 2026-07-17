namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class InteriorDesignHighlight
{
    public int Id { get; set; }

    public int InteriorDesignId { get; set; }

    public string Text { get; set; } = null!;

    public int SortOrder { get; set; }

    public virtual InteriorDesign InteriorDesign { get; set; } = null!;
}
