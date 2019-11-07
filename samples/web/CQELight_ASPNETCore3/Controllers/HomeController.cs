using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CQELight_ASPNETCore3.Models;

namespace CQELight_ASPNETCore3.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUselessService uselessService;

        public HomeController(
            ILogger<HomeController> logger,
            IUselessService uselessService)
        {
            _logger = logger;
            this.uselessService = uselessService;
        }

        public IActionResult Index()
        {
            ViewData["Welcome"] = uselessService.GetData();
            return View();
        }

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
