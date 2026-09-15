using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class CheckoutViewModel
    {
        public int AppointmentId { get; set; }
        public int BillId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; } = string.Empty;

        [Display(Name = "Total Fee")]
        public decimal TotalFee { get; set; }

        [Display(Name = "Already Paid")]
        public decimal AlreadyPaid { get; set; }

        [Display(Name = "Balance Due")]
        public decimal BalanceDue => TotalFee - AlreadyPaid;

        [Required(ErrorMessage = "Please enter the amount you want to pay.")]
        [Range(0.01, 1000000, ErrorMessage = "Amount must be greater than 0.")]
        [Display(Name = "Amount to Pay Now")]
        public decimal AmountNow { get; set; }

        [Required(ErrorMessage = "Please choose a payment method.")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        public bool IsReceptionistFlow { get; set; }
    }
}