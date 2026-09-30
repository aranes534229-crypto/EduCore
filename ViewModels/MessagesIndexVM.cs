using EduCore.Models.Entities;

namespace EduCore.ViewModels;

public class MessagesIndexVM
{
    public IReadOnlyList<Message> Items { get; set; } = Array.Empty<Message>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public string? Search { get; set; }
    public string? Direction { get; set; }
    public string? Read { get; set; }

    public string UserId { get; set; } = "";
    public Dictionary<string, string> Names { get; set; } = new();
    public Dictionary<int, Student> Students { get; set; } = new();
    public HashSet<int> UnreadIds { get; set; } = new();
    public bool CanCompose { get; set; }

    public int[] PageSizeValues { get; } = new[] { 5, 10, 25, 50 };
}
