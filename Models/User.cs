using Microsoft.AspNetCore.Identity;

namespace NotePad.Models
{
    public class User : IdentityUser<int>
    {
        public string Verification { get; set; }
        public bool isVerified { get; set; }
        public DateTime? VerificationCodeExpiration { get; set; }

    }
}
