using System.ComponentModel.DataAnnotations;

namespace AlawnehEway.Models
{
    public class FeeTier
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal MinAmount { get; set; }

        public decimal? MaxAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Fee { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }
    }
}


