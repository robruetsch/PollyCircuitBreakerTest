using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PollyDataTestApi.Services;

namespace PollyDataTestApi.Controllers
{
    [Route("api/[controller]")]
    public class TestController : Controller
    {
        private readonly IErrorCounter _errorCounter;

        public TestController(IErrorCounter errorCounter)
        {
            _errorCounter = errorCounter;
        }

        [Route("Site")]
        public IActionResult GetSite()
        {
            //Simple counter that is a singleton to throw bad requests to test the circuit is open, 
            //and how it acts on the half open state.
            if(_errorCounter.GetCounter() <= 4)
            {
                _errorCounter.Increment();
                return BadRequest();
            }

            var thisSite = "PollyDataTestApi";
            return Ok(thisSite);
        }
    }
}