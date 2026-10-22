using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Models
{
    [Table("Results")]
    public class Result
    {
        [Key]
        [Column("result_id")]
        public int ResultId { get; set; }

        [Required]
        [Column("enrolment_id")]
        public int EnrolmentId { get; set; }

        [Column("finish_time")]
        public TimeSpan? FinishTime { get; set; }

        [Column("overall_position")]
        public int? OverallPosition { get; set; }

        [Column("category_position")]
        public int? CategoryPosition { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Finished"; // 'Finished', 'DNF', 'DSQ'

        [ForeignKey("EnrolmentId")]
        [JsonIgnore]
        public virtual Enrolment? Enrolment { get; set; }
    }
}
