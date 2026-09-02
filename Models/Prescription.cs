using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Application_Doctor_Management.Models{
    public class Prescription{
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        public DateTime PrescriptionDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Notes { get; set; }

        public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    }
}