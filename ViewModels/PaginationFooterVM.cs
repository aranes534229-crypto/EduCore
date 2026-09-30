namespace EduCore.ViewModels;

/// <summary>Model for the shared _PaginationFooter partial. RouteValues carries the page's
/// filter values (q, status, etc.); the partial merges page/pageSize per link.</summary>
public class PaginationFooterVM
{
    public string Action { get; set; } = "";
    public string? Controller { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string AriaLabel { get; set; } = "Pagination";
    public IDictionary<string, object?> RouteValues { get; set; } = new Dictionary<string, object?>();
    public int[] PageSizeValues { get; } = new[] { 5, 10, 25, 50 };
}
