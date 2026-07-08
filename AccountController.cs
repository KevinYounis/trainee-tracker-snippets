using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TraineeTracker.Web.Models;

namespace TraineeTracker.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                return await RedirectByRoleAsync(user);
            }
        }

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "E-Mail und Passwort dürfen nicht leer sein.");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IstAktiv)
        {
            ModelState.AddModelError("", user == null
                ? "Ungültige Login-Daten."
                : "Dieser Account wurde deaktiviert. Bitte wende dich an den Admin.");
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            password,
            isPersistent: false,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return await RedirectByRoleAsync(user);
        }

        ModelState.AddModelError("", "Ungültige Login-Daten.");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    private async Task<IActionResult> RedirectByRoleAsync(ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return RedirectToAction("UserList", "Admin");
        }

        if (await _userManager.IsInRoleAsync(user, "Mentor"))
        {
            return RedirectToAction("Index", "Mentor");
        }

        if (await _userManager.IsInRoleAsync(user, "Trainee"))
        {
            return RedirectToAction("Index", "TraineeLektion");
        }

        return RedirectToAction("Index", "Home");
    }
}
