
using July2025Capstone.Data;
using July2025Capstone.Models;

using July2025Capstone.Shared;

namespace July2025Capstone.Services
{
    public interface IMedicationService
    {
        Task<List<MedicationDTO>> GetAllMedicationsAsync(string userName);
        Task<bool> AddMedication(Medication medication);
    }
}
