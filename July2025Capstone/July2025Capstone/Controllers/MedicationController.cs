using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using July2025Capstone.Shared;
using July2025Capstone.Data;
using July2025Capstone.Services;
using July2025Capstone.Models;

namespace July2025Capstone.Controllers

{
    [ApiController]
    public class MedicationController : Controller
    {
        private readonly IMedicationService _medicationService;

        public MedicationController(IMedicationService medicationService)
        {
            _medicationService = medicationService;
        }

        [HttpGet]
        [Route("api/medications")]
        public async Task<IActionResult> GetMedications()
        {
            List<MedicationDTO> medications = new List<MedicationDTO>();

            var userName = User.Identity.Name;

            //Call medication service to get all meds
            //TODO: Update the below function with the user ID
            medications = await _medicationService.GetAllMedicationsAsync(userName);

            if(medications is null)
            {
                return NotFound("No medications found for the user.");
            }
            else
            {
                //TODO: replace this with DataResponse
                /*
                DataResponse<MedicationDTO> dataRes = new DataResponse<MedicationDTO>
                {
                    Succeeded = true,
                    Message = "Medications found.",
                    Data = medications
                };
                */
                return Ok(medications);
            }
        }

        [HttpPost]
        [Route("api/add-medication")]
        public async Task<ActionResult> AddMedication([FromBody] MedicationDTO medicationDto)
        {
            //TODO: Add user check

            //TODO: Implement a user service to get information about the user

            //TODO: Implement a way to check if the medication exists for the user
            //Medication? medication = await _medicationService.

            var newMed = new Medication
            {
                Name = medicationDto.Name,
                DosageStrength = medicationDto.DosageStrength,
                //How do I map a string dosageUnit to a DosageUnit dosageunit?
                //How do I map a string frequency to a Frequency frequncy?
            };

            await _medicationService.AddMedication(newMed);

            Response res = new Response
            {
                Succeeded = true,
                Message = $"Medication '{newMed.Name}' added to account."
            };

            return Ok(res);
        }
    }
}
