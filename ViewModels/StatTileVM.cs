namespace EduCore.ViewModels;

/// <summary>Reusable stat tile model for the _StatTile partial. ColClass / ValueClass are
/// optional so a missing value degrades to a default instead of a runtime binder error.</summary>
public class StatTileVM
{
    public string? Label { get; set; }
    public object? Value { get; set; }
    public string? ColClass { get; set; }
    public string? ValueClass { get; set; }
}
