using Microsoft.AspNetCore.Identity;

namespace Email_Project.Models
{
    public class CustomIdentityValidator:IdentityErrorDescriber
    {
        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError()
            {
                Code = "PasswordTooShort",
                Description = "PASSPORT TO SHORT!"
            };
        }
    }
}
