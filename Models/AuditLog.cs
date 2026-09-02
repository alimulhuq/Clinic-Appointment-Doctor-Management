using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Application_Doctor_Management.Models{
    public class AuditLog{
        public int Id { get; set; }

        [StringLength(50)]
        public string? UserId { get; set; }

        [StringLength(50)]
        public string? UserName { get; set; }

        [StringLength(50)]
        public string? Action { get; set; }

        [StringLength(50)]
        public string? Entity { get; set; }

        public int? EntityId { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? IpAddress { get; set; }
    }
}