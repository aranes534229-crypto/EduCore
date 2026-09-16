namespace EduCore.Models.Entities;

/// <summary>One post in a teacher↔parent conversation about a child. The thread is implicit —
/// posts about the same StudentId group into a conversation; no separate thread table is needed.</summary>
public class Message
{
    public int Id { get; set; }
    public string SenderId { get; set; } = "";    // ApplicationUser id
    public string RecipientId { get; set; } = ""; // ApplicationUser id
    public int StudentId { get; set; }            // the child the message is about — scopes both sides

    [System.ComponentModel.DataAnnotations.Required]
    public string Body { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool Seen { get; set; }
}