using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Models
{
    [Table("Events")]
    public class Event
    {
        [Key]
        [Column("event_id")]
        public int EventId { get; set; }

        [Required]
        [Column("organiser_id")]
        public int OrganiserId { get; set; }

        [Required]
        [Column("event_type_id")]
        public int EventTypeId { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("event_name")]
        public string EventName { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("event_date", TypeName = "date")]
        public DateTime EventDate { get; set; }

        [Column("start_time")]
        public TimeSpan? StartTime { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("location")]
        public string Location { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("province")]
        public string? Province { get; set; }

        [Column("banner_image_url")]
        [MaxLength(500)]
        public string? BannerImageUrl { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Upcoming"; // 'Upcoming', 'Open', 'Closed', 'Completed', 'Cancelled'

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("OrganiserId")]
        public virtual User? Organiser { get; set; }

        [ForeignKey("EventTypeId")]
        public virtual EventType? EventType { get; set; }

        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
