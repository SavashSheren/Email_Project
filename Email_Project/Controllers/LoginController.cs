using Email_Project.Dtos;
using Email_Project.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Email_Project.Controllers
{
   
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _singInManager;

        public LoginController(SignInManager<AppUser> singInManager)
        {
            _singInManager = singInManager;
        }

        [HttpGet]
        public IActionResult UserLogin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UserLogin( UserLoginDto userLoginDto)
        {
            var result = await _singInManager.PasswordSignInAsync(userLoginDto.Username, userLoginDto.Password, true, false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Message");
            }
            return View();
        }
        public async Task<IActionResult> Logout()
        {
            await _singInManager.SignOutAsync(); 
            return RedirectToAction("UserLogin", "Login"); 
        }
    }
}
