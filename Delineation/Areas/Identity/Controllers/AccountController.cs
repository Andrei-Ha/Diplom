using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CustomIdentity.ViewModels;
using CustomIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Delineation.Models;
using System.Security.Claims;
using Delineation.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.Sqlite;

namespace CustomIdentity.Areas.Identity.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly string _defaultConnection;
        private readonly EmailService _emailService;
        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _defaultConnection = _configuration.GetConnectionString("DefaultConnection");
            _emailService = emailService;
        }
        private object GetUsersList()
        {
            List<SelList> myList = new List<SelList>();
            using (SqliteConnection con = new SqliteConnection(_defaultConnection))
            {
                using (SqliteCommand cmd = con.CreateCommand())
                {
                    con.Open();
                    cmd.CommandText = "select m_linom,fio,login from mail where m_linom!=777777 order by fio";
                    SqliteDataReader reader = cmd.ExecuteReader();
                    using (reader)
                    {
                        while (reader.Read())
                        {
                            myList.Add(new SelList() { Id = reader["M_LINOM"].ToString(), Text = "" + reader["FIO"].ToString() + " - " + reader["LOGIN"].ToString() + "@brestenergo.by" });
                        }
                    }
                }
            }
            return new SelectList(myList, "Id", "Text");
        }
        [HttpGet]
        public IActionResult Register()
        {
            ViewData["Person"] = GetUsersList();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = new User() { Email = model.Email, UserName = model.UserName, Linom = model.Linom };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "D_accepter");
                    // !!!save Linom of User into Claims with name ClaimTypes.Sid
                    var claim = new Claim(ClaimTypes.Sid, model.Linom.ToString());
                    await _userManager.AddClaimAsync(user, claim);

                    // генерация токена для пользователя
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action(
                        "ConfirmEmail",
                        "Account",
                        new { userId = user.Id, code = code },
                        protocol: HttpContext.Request.Scheme);
                    await _emailService.SendEmailAsync(model.Email, "Подтверждение вашего e-mail в программе \"Акты разграничений\"",
                        $"Подтвердите регистрацию в программе \"Акты разграничений\", перейдя по ссылке: <a href='{callbackUrl}'>Ссылка</a>. \n Если Вы не регистрировались в программе, можете удалить это сообщение!");

                    return Content("Для завершения регистрации проверьте электронную почту и перейдите по ссылке, указанной в письме");

                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            ViewData["Person"] = GetUsersList();
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            List<User> List_D_operator = new List<User>();
            List<User> List_D_accepter = new List<User>();
            List<User> List_All = _userManager.Users.OrderBy(p => p.UserName).ToList();
            // формирование списка всех пользователей, которые имеют отношение к задаче Delineation (префикс "D_")
            foreach(User user in List_All)
            {
                var roles = await _userManager.GetRolesAsync(user);
                foreach (string role in roles)
                {
                    if (role == "D_operator")
                        List_D_operator.Add(user);
                    if (role == "D_accepter")
                        List_D_accepter.Add(user);
                }
            }
            ViewBag.list_operator = List_D_operator;
            ViewBag.list_accepter = List_D_accepter;
            ViewBag.list_other = List_All.Except<User>(List_D_operator).Except<User>(List_D_accepter).ToList<User>();
            return View(new LoginViewModel() { ReturnUrl = returnUrl });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // check. is email confirmed
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError(string.Empty, "Вы не подтвердили свой Email!");
                        return View(model);
                    }

                    var passwordIsCorrect = await _userManager.CheckPasswordAsync(user, model.Password);
                    Claim customClaim = null;
                    if (passwordIsCorrect)
                    {
                        using SqliteConnection con = new SqliteConnection(_defaultConnection);
                        using SqliteCommand cmd = con.CreateCommand();
                        con.Open();
                        cmd.CommandText = "Select M_SLUZHBA from MAIL where M_LINOM = " + user.Linom;
                        var podr = cmd.ExecuteScalar();
                        if (podr != null)
                        {
                            customClaim = new Claim("Podr", podr.ToString());

                        }

                        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(user);
                        if (customClaim != null)
                        {
                            if (claimsPrincipal?.Identity is ClaimsIdentity claimsIdentity)
                            {
                                claimsIdentity.AddClaim(customClaim);
                            }

                        }
                        await _signInManager.Context.SignInAsync(IdentityConstants.ApplicationScheme,
                                claimsPrincipal, new AuthenticationProperties { IsPersistent = model.RememberMe });

                        // проверяем, принадлежит ли URL приложению
                        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return Redirect(model.ReturnUrl);
                        }
                        else
                            return RedirectToAction("Index", "Home", new { Area = "" });
                    }
                }
                ModelState.AddModelError(string.Empty, "Неправильный логин или пароль!");
            }
            return View(model);
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return View("Error");
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
                return RedirectToAction("Index", "Home",new { Area = "", user = user.UserName });
            else
                return View("Error");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            //удаляем аутентификационные куки
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home", new { Area = "" });
        }
        public IActionResult Index()
        {
            //ViewBag.userName = _signInManager.
            return View();
        }
        public IActionResult AccessDenied() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    //if user with this email is missing in Database, we hide this information
                    return View("ForgotPassword");
                }
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                await _emailService.SendEmailAsync(model.Email, "Reset Password",
                    $"Для сброса пароля пройдите по ссылке: <a href='{callbackUrl}'>link</a>");
                return Content("Для сброса пороля перейдите по ссылке в письме,отправленном вам на email.");
            }
            return View(model);
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            return code == null ? View("Error") : View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return View("ResetPasswordConfirmation");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return View("ResetPasswordConfirmation");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }
    }
}