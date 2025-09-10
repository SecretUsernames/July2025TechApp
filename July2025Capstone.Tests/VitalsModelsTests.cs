using Xunit;
using July2025Capstone.Models;
using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Tests
{
    public class VitalsModelsTests
    {
        [Fact]
        public void VitalBloodPressure_Properties_CanBeSet()
        {
            // Arrange & Act
            var bloodPressure = new VitalBloodPressure
            {
                Id = 1,
                UserId = "test-user",
                Systolic = 120,
                Diastolic = 80,
                DateMeasured = new DateTime(2025, 1, 1)
            };

            // Assert
            Assert.Equal(1, bloodPressure.Id);
            Assert.Equal("test-user", bloodPressure.UserId);
            Assert.Equal(120, bloodPressure.Systolic);
            Assert.Equal(80, bloodPressure.Diastolic);
            Assert.Equal(new DateTime(2025, 1, 1), bloodPressure.DateMeasured);
        }

        [Fact]
        public void VitalGlucose_Properties_CanBeSet()
        {
            // Arrange & Act
            var glucose = new VitalGlucose
            {
                Id = 1,
                UserId = "test-user",
                GlucoseValue = 100.5m,
                DateMeasured = new DateTime(2025, 1, 1)
            };

            // Assert
            Assert.Equal(1, glucose.Id);
            Assert.Equal("test-user", glucose.UserId);
            Assert.Equal(100.5m, glucose.GlucoseValue);
            Assert.Equal(new DateTime(2025, 1, 1), glucose.DateMeasured);
        }

        [Fact]
        public void VitalWeight_Properties_CanBeSet()
        {
            // Arrange & Act
            var weight = new VitalWeight
            {
                Id = 1,
                UserId = "test-user",
                WeightValue = 175.5m,
                Unit = WeightUnit.Pounds,
                DateMeasured = new DateTime(2025, 1, 1)
            };

            // Assert
            Assert.Equal(1, weight.Id);
            Assert.Equal("test-user", weight.UserId);
            Assert.Equal(175.5m, weight.WeightValue);
            Assert.Equal(WeightUnit.Pounds, weight.Unit);
            Assert.Equal(new DateTime(2025, 1, 1), weight.DateMeasured);
        }

        [Fact]
        public void WeightUnit_HasExpectedValues()
        {
            // Assert
            Assert.Equal(0, (int)WeightUnit.Pounds);
            Assert.Equal(1, (int)WeightUnit.Kilograms);
        }

        [Theory]
        [InlineData(120, 80, true)]   // Normal BP
        [InlineData(60, 40, true)]    // Minimum valid values
        [InlineData(300, 200, true)]  // Maximum valid values
        [InlineData(59, 80, false)]   // Systolic too low
        [InlineData(301, 80, false)]  // Systolic too high
        [InlineData(120, 39, false)]  // Diastolic too low
        [InlineData(120, 201, false)] // Diastolic too high
        public void VitalBloodPressure_ValidationRanges_WorkCorrectly(int systolic, int diastolic, bool shouldBeValid)
        {
            // Arrange
            var bloodPressure = new VitalBloodPressure
            {
                UserId = "test-user",
                Systolic = systolic,
                Diastolic = diastolic,
                DateMeasured = DateTime.Now
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(bloodPressure, new ValidationContext(bloodPressure), validationResults, true);

            // Assert
            Assert.Equal(shouldBeValid, isValid);
        }
    }
}
