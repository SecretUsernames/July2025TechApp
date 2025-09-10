using July2025Capstone.Client.Pages;
using July2025Capstone;
using July2025Capstone.Data;
using July2025Capstone.Models;
using July2025Capstone.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace July2025Capstone.Tests
{
    public class MedicationDosesTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // fresh DB per test
                .Options;

            return new ApplicationDbContext(options);
        }

        private July2025Capstone.Controllers.MedicationDoseController GetController(ApplicationDbContext context)
        {
            var logger = new Mock<ILogger<July2025Capstone.Controllers.MedicationDoseController>>();
            return new July2025Capstone.Controllers.MedicationDoseController(context, logger.Object);
        }



        [Fact]
        public async Task GetDosesForMedication_Returns_EmptyList_When_NoDosesExist()
        {
            // Arrange
            var db = GetInMemoryDbContext();
            var controller = GetController(db);

            // Act
            var result = await controller.GetDosesForMedication(1);

            // Assert
            var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
            var doses = Assert.IsAssignableFrom<List<July2025Capstone.Models.MedicationDose>>(okResult.Value);
            Assert.Empty(doses);
        }

        [Fact]
        public async Task GetDosesForMedication_Returns_Doses()
        {
            // Arrange
            var db = GetInMemoryDbContext();
            db.MedicationDoses.Add(new July2025Capstone.Models.MedicationDose { MedicationId = 1, DayOfWeek = 0, TimeOfDay = TimeOfDay.Morning });
            await db.SaveChangesAsync();

            var controller = GetController(db);

            // Act
            var result = await controller.GetDosesForMedication(1);

            // Assert
            var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
            var doses = Assert.IsAssignableFrom<List<July2025Capstone.Models.MedicationDose>>(okResult.Value);
            Assert.Single(doses);
        }

        [Fact]
        public async Task ToggleDose_ReturnsBadRequest_WhenDayOutOfRange()
        {
            // Arrange
            var db = GetInMemoryDbContext();
            var controller = GetController(db);

            var request = new ToggleDoseRequest
            {
                MedicationId = 1,
                DayOfWeek = 8, // invalid
                TimeOfDay = TimeOfDay.Morning
            };

            // Act
            var result = await controller.ToggleDose(request);

            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        }
    }
}
