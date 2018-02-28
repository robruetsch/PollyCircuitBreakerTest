using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PollyTestUI.Models;
using PollyTestUI.Services;

namespace PollyTestUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPollyServiceClient _pollyClient;
        public HomeController(IPollyServiceClient pollyClient)
        {
            _pollyClient = pollyClient;           
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> About()
        {

            ViewData["Message"] = await _pollyClient.GetSiteName();

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
