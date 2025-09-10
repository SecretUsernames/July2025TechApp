using Azure.Identity;
using July2025Capstone.Data;
using July2025Capstone.Models;
using July2025Capstone.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace July2025Capstone.Services
{
    public class MedicationService : IMedicationService
    {
        private readonly ApplicationDbContext _context;

        //private readonly UserManager<ApplicationUser> _userManager;

        public MedicationService(   ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MedicationDTO>> GetAllMedicationsAsync(string userName)
        {
            //Obtain medications from the DB
            //TODO: Update search so that it is for a specific user (using userId)
            //var medications = await _context
            //.Medications
            //.ToListAsync();
            
            

            var medications = await (from m in _context.Medications
                                     join p in _context.Patients on m.PatientId equals p.Id
                                     join u in _context.Users on p.UserId equals u.Id
                                     where u.NormalizedUserName == userName.ToUpper()
                                     select m).ToListAsync();

            //Map the Medication object to a MedicationDTO object
            var medicationDTOs = medications.Select(m => new MedicationDTO
            {
                Name = m.Name,
                DosageStrength = m.DosageStrength,
                DosageUnit = m.DosageUnit.ToString(),
                Frequency = m.Frequency.ToString()

            }).ToList();

            return medicationDTOs;
        }

        public async Task<bool> AddMedication(Medication medication)
        {
            //Determine if the med to be added exists in the DB
            //Ask Eric what this does
            var existingMeds = await _context.Medications
                .Include(m => m.Name)
                .FirstOrDefaultAsync(m => m.Id == medication.Id);

            if (existingMeds is null)
            {
                try
                {
                    await _context.Medications.AddAsync(medication);
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception e)
                {

                    Console.WriteLine("Error in AddMedication");
                }
            }
            return false;
        }
    }
}
