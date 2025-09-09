using Microsoft.AspNetCore.Identity;

namespace July2025Capstone.Data
{
    // Extend IdentityUser with custom fields
    public class ApplicationUser : IdentityUser
    {
        public string Status { get; set; } = "Active";  // Active, Suspended, etc.
        public DateTime? LastLoginUtc { get; set; }     // When the user last logged in
    }
}
