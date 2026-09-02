using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels{
    public class PrescriptionItemViewModel{
        public string MedicineName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public string? Instructions { get; set; }
    }

    public class PrescriptionViewModel{
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public string? Notes { get; set; }
        public List<PrescriptionItemViewModel> Items { get; set; } = new List<PrescriptionItemViewModel>();
    }
}