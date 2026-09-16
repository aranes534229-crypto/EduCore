using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

public class Announcement
{
    public int Id { get; set; }

    [Required] public string Title { get; set; } = "";
    [Required] public string Body { get; set; } = "";

    // Snapshot of the author's display name, not a live user reference.
    public string AuthorName { get; set; } = "";
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
}