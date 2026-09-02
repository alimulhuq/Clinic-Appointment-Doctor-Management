using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Application_Doctor_Management.Models{
    public class Patient{
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress, StringLength(100)]
        public string? Email { get; set; }

        public int Age { get; set; }

        [Required, StringLength(20)]
        public string Gender { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? MedicalHistory { get; set; }

        [StringLength(100)]
        public string? Allergies { get; set; }

        [StringLength(50)]
        public string? BloodGroup { get; set; }

        [StringLength(100)]
        public string? EmergencyContact { get; set; }

        [StringLength(100)]
        public string? EmergencyContactPhone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }
}