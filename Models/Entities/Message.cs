namespace EduCore.Models.Entities;

/// <summary>One post in a conversation. Two thread shapes: student-scoped (teacher↔parent about a
/// child — posts sharing the same StudentId) and plain staff-to-staff (StudentId null, grouped by
/// the two users). No separate thread table is needed.</summary>
public class Message
{
    public int Id { get; set; }
    public string SenderId { get; set; } = "";    // ApplicationUser id
    public string RecipientId { get; set; } = ""; // ApplicationUser id

    /// <summary>The child the message is about — scopes Faculty/Parent conversations.
    /// Null for plain staff-to-staff messages (Admin/Finance/Registrar).</summary>
    public int? StudentId { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string Body { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool Seen { get; set; }
}