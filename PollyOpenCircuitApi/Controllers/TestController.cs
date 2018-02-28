using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PollyOpenCircuitApi.Controllers
{
    [Route("api/[controller]")]
    public class TestController : Controller
    {
        [Route("Site")]
        public IActionResult GetSite()
        {
            var thisSite = "PollyOpenCircuitApi";
            return Ok(thisSite);
        }
    }
}