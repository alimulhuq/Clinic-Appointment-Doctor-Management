using Microsoft.EntityFrameworkCore;
using Clinic_Application_Doctor_Management.Models;

namespace Clinic_Application_Doctor_Management.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Doctor> Doctors { get; set; }
}