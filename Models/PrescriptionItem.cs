using System.ComponentModel.DataAnnotations;

namespace Clinic_Application_Doctor_Management.Models{
    public class PrescriptionItem{
        public int Id { get; set; }

        [Required]
        public int PrescriptionId { get; set; }
        public Prescription? Prescription { get; set; }

        [Required, StringLength(100)]
        public string MedicineName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Dosage { get; set; } // e.g., 500mg

        [StringLength(50)]
        public string? Frequency { get; set; } // e.g., Twice daily

        [StringLength(50)]
        public string? Duration { get; set; } // e.g., 7 days

        [StringLength(200)]
        public string? Instructions { get; set; }
    }
}