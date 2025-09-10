using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using July2025Capstone.Client.Pages;
using July2025Capstone.Shared.Models;
using July2025Capstone.Controllers;
using July2025Capstone.Data;
using July2025Capstone.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace July2025Capstone.Tests
{
    public class MedicationDoseControllerTest
    {
        public class ApplicationDbContext : DbContext
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            //public DbSet<MedicationDose> MedicationDoses { get; set; }
        }

        private July2025Capstone.Data.ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<July2025Capstone.Data.ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            var context = new July2025Capstone.Data.ApplicationDbContext(options);

            // Seed data
            context.MedicationDoses.AddRange(
                new July2025Capstone.Models.MedicationDose
                {
                    MedicationId = 100,
                    TimeOfDay = TimeOfDay.Morning,
                    DayOfWeek = 1, // Monday
                    Taken = false,
                    TakenAt = null
                },
                new July2025Capstone.Models.MedicationDose
                {
                    MedicationId = 100,
                    TimeOfDay = TimeOfDay.Evening,
                    DayOfWeek = 3, // Wednesday
                    Taken = true,
                    TakenAt = DateTime.UtcNow.AddDays(-1)
                },
                new July2025Capstone.Models.MedicationDose
                {
                    MedicationId = 200,
                    TimeOfDay = TimeOfDay.Bedtime,
                    DayOfWeek = 5, // Friday
                    Taken = false,
                    TakenAt = null
                }
            );

            context.SaveChanges();

            return context;
        }


        [Fact]
        public async Task GetDosesForMedication_ReturnsCorrectDoses()
            {
                // Arrange
                var context = GetInMemoryDbContext();
                var logger = NullLogger<MedicationDoseController>.Instance;
                var controller = new MedicationDoseController(context, logger);

                // Act
                var result = await controller.GetDosesForMedication(100);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var doses = Assert.IsType<List<July2025Capstone.Models.MedicationDose>>(okResult.Value);
                Assert.Equal(2, doses.Count);
                Assert.All(doses, d => Assert.Equal(100, d.MedicationId));
            }


        }
}
