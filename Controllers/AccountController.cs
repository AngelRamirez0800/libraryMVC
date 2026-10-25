using libraryMVC.Models;
using libraryMVC.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace libraryMVC.Controllers
{
    public class AccountController : Controller
    {

        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;


        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            _logger.LogInformation("Displaying the account registration form.");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            _logger.LogInformation("Processing an account registration request.");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            const string defaultRole = RoleNames.User;
            if (!await _roleManager.RoleExistsAsync(defaultRole))
            {
                _logger.LogWarning("Account registration could not continue because the default role is unavailable.");
                ModelState.AddModelError(string.Empty, "The default user role is not available.");
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.UserName.Trim(),
                Email = model.Email.Trim()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Account registration failed validation.");
                AddIdentityErrors(result);
                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, defaultRole);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                _logger.LogWarning("A newly registered account could not be assigned its default role.");
                AddIdentityErrors(roleResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("An account was registered and signed in successfully.");
            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Processing an account sign-out request.");
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Account sign-out completed successfully.");
            return RedirectToAction("Index", "Home");
        }





        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            _logger.LogInformation("Displaying the account sign-in form.");
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            _logger.LogWarning("An access-denied page was displayed.");
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            _logger.LogInformation("Processing an account sign-in request.");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.UserNameOrEmail.Trim())
                ?? await _userManager.FindByEmailAsync(model.UserNameOrEmail.Trim());

            if (user == null)
            {
                _logger.LogWarning("Account sign-in failed because the account was not found.");
                AddCredentialErrors("Invalid credentials.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Account sign-in completed successfully.");
                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            _logger.LogWarning("Account sign-in failed. Locked out: {IsLockedOut}.", result.IsLockedOut);
            AddCredentialErrors(result.IsLockedOut
                ? "This account is temporarily locked. Try again later."
                : "Invalid credentials.");
            return View(model);
        }

        private void AddCredentialErrors(string message)
        {
            ModelState.AddModelError(nameof(LoginViewModel.UserNameOrEmail), message);
            ModelState.AddModelError(nameof(LoginViewModel.Password), message);
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }


    }
}
