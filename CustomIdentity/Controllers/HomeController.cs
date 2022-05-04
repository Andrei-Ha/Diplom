using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CustomIdentity.Models;
using Microsoft.AspNetCore.Authorization;

namespace CustomIdentity.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index([FromQuery] string user)
        {
            object oUser = user; 
            return View(oUser);
        }
        //public async Task<IActionResult> SendEmail()
        //{
        //    EmailService emailService = new EmailService();
        //    await emailService.SendEmailAsync("asgoreglyad@brestenergo.by", "andrei", "This is test Mail from net.core!");
        //    return RedirectToAction("Index", "Home");
        //}

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
