using Xunit;
using July2025Capstone.Models;
using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Tests
{
    public class AnalyticsControllerTests
    {
        [Fact]
        public void VitalBloodPressure_ValidData_PassesValidation()
        {
            // Arrange
            var bloodPressure = new VitalBloodPressure
            {
                UserId = "test-user",
                Systolic = 120,
                Diastolic = 80,
                DateMeasured = DateTime.Now
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(bloodPressure, new ValidationContext(bloodPressure), validationResults, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void VitalBloodPressure_InvalidSystolic_FailsValidation()
        {
            // Arrange
            var bloodPressure = new VitalBloodPressure
            {
                UserId = "test-user",
                Systolic = 400, // Invalid - too high
                Diastolic = 80,
                DateMeasured = DateTime.Now
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(bloodPressure, new ValidationContext(bloodPressure), validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("Systolic pressure must be between 60 and 300"));
        }

        [Fact]
        public void VitalBloodPressure_InvalidDiastolic_FailsValidation()
        {
            // Arrange
            var bloodPressure = new VitalBloodPressure
            {
                UserId = "test-user",
                Systolic = 120,
                Diastolic = 300, // Invalid - too high
                DateMeasured = DateTime.Now
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(bloodPressure, new ValidationContext(bloodPressure), validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("Diastolic pressure must be between 40 and 200"));
        }

        [Fact]
        public void VitalGlucose_ValidData_PassesValidation()
        {
            // Arrange
            var glucose = new VitalGlucose
            {
                UserId = "test-user",
                GlucoseValue = 100m,
                DateMeasured = DateTime.Now
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(glucose, new ValidationContext(glucose), validationResults, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void VitalWeight_ValidData_PassesValidation()
        {
            // Arrange
            var weight = new VitalWeight
            {
                UserId = "test-user",
                WeightValue = 170m,
                Unit = WeightUnit.Pounds,
                DateMeasured = DateTime.Now
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(weight, new ValidationContext(weight), validationResults, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(validationResults);
        }

        [Theory]
        [InlineData(WeightUnit.Pounds)]
        [InlineData(WeightUnit.Kilograms)]
        public void VitalWeight_AllWeightUnits_AreValid(WeightUnit unit)
        {
            // Arrange
            var weight = new VitalWeight
            {
                UserId = "test-user",
                WeightValue = 70m,
                Unit = unit,
                DateMeasured = DateTime.Now
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(weight, new ValidationContext(weight), validationResults, true);

            // Assert
            Assert.True(isValid);
        }
    }
}
