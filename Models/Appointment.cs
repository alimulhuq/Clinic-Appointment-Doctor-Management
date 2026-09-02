using System.ComponentModel.DataAnnotations;

namespace Clinic_Application_Doctor_Management.Models{
    public class Appointment{
        public int Id { get; set; }

        // Doctor relationship
        [Required]
        public int DoctorId { get; set; }

        public Doctor? Doctor { get; set; }

        // Patient relationship
        [Required]
        public int PatientId { get; set; }

        public Patient? Patient { get; set; }

        // Appointment information
        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan AppointmentTime { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}