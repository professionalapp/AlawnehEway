using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AlawnehEway.Models
{
    public class ExchangeRate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [Required]
        [Precision(18, 5)]
        [Range(0.00001, double.MaxValue, ErrorMessage = "سعر الصرف يجب أن يكون أكبر من صفر")]
        public decimal Rate { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "USD";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastModifiedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}

