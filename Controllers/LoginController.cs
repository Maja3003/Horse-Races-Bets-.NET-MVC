using HorseRacing.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class LoginController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public LoginController(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [HttpGet]
    [Route("Login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(Login model)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                // Obsługa sytuacji, gdy konto jest zablokowane
                ModelState.AddModelError("", "Account locked out. Please try again later.");
            }
            else
            {
                // Komunikat w przypadku nieudanej autentykacji
                ModelState.AddModelError("", "Wrong password or account doesn't exist.");
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
