using Microsoft.AspNetCore.Identity;

namespace Email_Project.Entities
{
    public class AppUser :IdentityUser
    {
        public String Name { get; set; }
        public String Surname { get; set; }
        public String? ImageUrl { get; set; }
        public String? About { get; set; }
    }
}
