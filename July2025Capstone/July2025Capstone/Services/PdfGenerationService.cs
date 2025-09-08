using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using July2025Capstone.Models;

namespace July2025Capstone.Services
{
    public interface IPdfGenerationService
    {
        byte[] GenerateCheckInPdf(Patient patient);
    }

    public class PdfGenerationService : IPdfGenerationService
    {
        public byte[] GenerateCheckInPdf(Patient patient)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("PATIENT CHECK-IN FORM")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        // Personal Information Section
                        column.Item().Text("PERSONAL INFORMATION").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
                        column.Item().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                        
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Name: {patient.FirstName} {patient.LastName}").FontSize(12);
                                col.Item().Text($"Date of Birth: {patient.DateOfBirth:MM/dd/yyyy}").FontSize(12);
                                col.Item().Text($"Phone: {patient.Phone}").FontSize(12);
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Email: {patient.Email}").FontSize(12);
                                col.Item().Text($"Gender: {(patient.Gender == 0 ? "Male" : patient.Gender == 1 ? "Female" : "Other")}").FontSize(12);
                            });
                        });

                        // Address Section
                        if (patient.Address != null)
                        {
                            column.Item().PaddingTop(15).Text("ADDRESS").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
                            column.Item().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            column.Item().Text($"{patient.Address.Street}").FontSize(12);
                            column.Item().Text($"{patient.Address.City}, {patient.Address.State} {patient.Address.PostalCode}").FontSize(12);
                            column.Item().Text($"{patient.Address.Country}").FontSize(12);
                        }

                        // Insurance Section
                        if (patient.InsurancePolicies.Any())
                        {
                            column.Item().PaddingTop(15).Text("INSURANCE POLICIES").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
                            column.Item().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            
                            foreach (var insurance in patient.InsurancePolicies)
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Provider: {insurance.Provider}").FontSize(12);
                                    row.RelativeItem().Text($"Policy #: {insurance.PolicyNumber}").FontSize(12);
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Group #: {insurance.GroupNumber}").FontSize(12);
                                    row.RelativeItem().Text($"Policyholder: {insurance.PolicyholderName} ({insurance.RelationshipToPatient})").FontSize(12);
                                });
                                column.Item().PaddingBottom(10);
                            }
                        }

                        // Emergency Contacts Section
                        if (patient.EmergencyContacts.Any())
                        {
                            column.Item().PaddingTop(15).Text("EMERGENCY CONTACTS").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
                            column.Item().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            
                            foreach (var contact in patient.EmergencyContacts)
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Name: {contact.FirstName} {contact.LastName}").FontSize(12);
                                    row.RelativeItem().Text($"Relationship: {contact.Relationship}").FontSize(12);
                                });
                                column.Item().Text($"Phone: {contact.Phone}").FontSize(12);
                                column.Item().PaddingBottom(10);
                            }
                        }

                        // Medications Section
                        if (patient.Medications.Any())
                        {
                            column.Item().PaddingTop(15).Text("CURRENT MEDICATIONS").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
                            column.Item().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            
                            foreach (var medication in patient.Medications)
                            {
                                var dosageUnit = medication.DosageUnit switch
                                {
                                    Models.DosageUnit.Milligrams => "mg",
                                    Models.DosageUnit.Micrograms => "mcg",
                                    Models.DosageUnit.Grams => "g",
                                    Models.DosageUnit.Milliliters => "mL",
                                    Models.DosageUnit.Liters => "L",
                                    Models.DosageUnit.Units => "IU",
                                    Models.DosageUnit.Other => medication.CustomDosageUnit ?? "units",
                                    _ => "units"
                                };

                                var frequency = medication.Frequency switch
                                {
                                    Models.MedicationFrequency.OnceDaily => "Once daily",
                                    Models.MedicationFrequency.TwiceDaily => "Twice daily",
                                    Models.MedicationFrequency.ThreeDaily => "Three times daily",
                                    Models.MedicationFrequency.FourDaily => "Four times daily",
                                    Models.MedicationFrequency.AsNeeded => "As needed",
                                    _ => "Unknown"
                                };

                                column.Item().Text($"• {medication.Name} - {medication.DosageStrength:0.##} {dosageUnit} - {frequency}").FontSize(12);
                            }
                        }

                        // Allergies Section
                        if (patient.Allergies.Any())
                        {
                            column.Item().PaddingTop(15).Text("ALLERGIES").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
                            column.Item().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            
                            foreach (var allergy in patient.Allergies)
                            {
                                column.Item().Text($"• {allergy.Allergen}").FontSize(12);
                            }
                        }
                        else
                        {
                            column.Item().PaddingTop(15).Text("ALLERGIES").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
                            column.Item().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            column.Item().Text("No known allergies").FontSize(12);
                        }

                        // Lifestyle Information
                        if (patient.Lifestyle != null)
                        {
                            column.Item().PaddingTop(15).Text("LIFESTYLE INFORMATION").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
                            column.Item().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            
                            var tobacco = patient.Lifestyle.TobaccoUse switch
                            {
                                0 => "No",
                                1 => "Yes",
                                2 => "Former user",
                                _ => "Unknown"
                            };
                            var alcohol = patient.Lifestyle.AlcoholUse switch
                            {
                                0 => "No",
                                1 => "Yes",
                                2 => "Occasionally",
                                _ => "Unknown"
                            };

                            column.Item().Text($"Tobacco Use: {tobacco}").FontSize(12);
                            column.Item().Text($"Alcohol Use: {alcohol}").FontSize(12);
                            column.Item().Text($"Recreational Drugs: {(patient.Lifestyle.RecreationalDrugs ? "Yes" : "No")}").FontSize(12);
                        }

                        // Visit Information
                        if (patient.VisitIntakes.Any())
                        {
                            var visitIntake = patient.VisitIntakes.First();
                            column.Item().PaddingTop(15).Text("REASON FOR VISIT").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
                            column.Item().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            column.Item().Text($"Primary Reason: {visitIntake.PrimaryReason}").FontSize(12);
                            column.Item().Text($"Previously Treated: {(visitIntake.TreatedBefore ? "Yes" : "No")}").FontSize(12);
                        }

                        // Consent Information
                        if (patient.Consent != null)
                        {
                            column.Item().PaddingTop(15).Text("CONSENT INFORMATION").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
                            column.Item().PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            column.Item().Text($"Consent Provided: Yes").FontSize(12);
                            column.Item().Text($"Signed By: {patient.Consent.SignatureName}").FontSize(12);
                            column.Item().Text($"Date Signed: {patient.Consent.SignedAt:MM/dd/yyyy 'at' h:mm tt}").FontSize(12);
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated on {DateTime.Now:MMMM dd, yyyy} at {DateTime.Now:h:mm tt}")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            return document.GeneratePdf();
        }
    }
}