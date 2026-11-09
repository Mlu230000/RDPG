using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [Required]
        [Column("event_id")]
        public int EventId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Column("distance_km", TypeName = "decimal(5,2)")]
        public decimal DistanceKm { get; set; }

        [Column("min_age")]
        public int? MinAge { get; set; }

        [Column("max_age")]
        public int? MaxAge { get; set; }

        [Required]
        [Column("entry_fee", TypeName = "decimal(8,2)")]
        public decimal EntryFee { get; set; } = 0.00m;

        [Column("max_participants")]
        public int? MaxParticipants { get; set; }

        [ForeignKey("EventId")]
        [JsonIgnore]
        public virtual Event? Event { get; set; }

        public virtual Route? Route { get; set; }

        [JsonIgnore]
        public virtual ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
