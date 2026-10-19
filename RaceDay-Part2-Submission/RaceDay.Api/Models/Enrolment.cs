using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Models
{
    [Table("Enrolments")]
    public class Enrolment
    {
        [Key]
        [Column("enrolment_id")]
        public int EnrolmentId { get; set; }

        [Required]
        [Column("participant_id")]
        public int ParticipantId { get; set; }

        [Required]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [MaxLength(10)]
        [Column("race_number")]
        public string? RaceNumber { get; set; }

        [Column("enrolment_date")]
        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        [Column("payment_status")]
        public string PaymentStatus { get; set; } = "Pending"; // 'Pending', 'Paid', 'Refunded'

        [ForeignKey("ParticipantId")]
        public virtual User? Participant { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual Result? Result { get; set; }
    }
}
