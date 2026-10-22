using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password_hash")]
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("phone")]
        public string? Phone { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }

        [JsonIgnore]
        public virtual ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();

        [JsonIgnore]
        public virtual ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
