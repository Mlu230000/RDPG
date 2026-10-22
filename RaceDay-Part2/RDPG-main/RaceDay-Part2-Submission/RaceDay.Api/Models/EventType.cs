using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Models
{
    [Table("EventTypes")]
    public class EventType
    {
        [Key]
        [Column("event_type_id")]
        public int EventTypeId { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("type_name")]
        public string TypeName { get; set; } = string.Empty;

        [MaxLength(150)]
        [Column("description")]
        public string? Description { get; set; }

        [JsonIgnore]
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
