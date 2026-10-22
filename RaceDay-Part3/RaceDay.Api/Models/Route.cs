using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Models
{
    [Table("Routes")]
    public class Route
    {
        [Key]
        [Column("route_id")]
        public int RouteId { get; set; }

        [Required]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("route_name")]
        public string RouteName { get; set; } = string.Empty;

        [MaxLength(150)]
        [Column("start_point")]
        public string? StartPoint { get; set; }

        [MaxLength(150)]
        [Column("end_point")]
        public string? EndPoint { get; set; }

        [Column("elevation_gain_m")]
        public int? ElevationGainM { get; set; }

        [MaxLength(255)]
        [Column("route_map_url")]
        public string? RouteMapUrl { get; set; }

        [ForeignKey("CategoryId")]
        [JsonIgnore]
        public virtual Category? Category { get; set; }
    }
}
