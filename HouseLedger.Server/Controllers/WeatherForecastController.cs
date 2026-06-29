using HouseLedger.Server.StaticClasses;
using HouseLedger.Shared;
using Microsoft.AspNetCore.Mvc;

namespace HouseLedger.Server.Controllers
{
    [ApiController]
    [Route(ApiRoutes.WeatherForecast)]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        [HttpGet("forecast", Name = "GetWeatherForecast")]
        public ActionResult<WeatherForecast[]> Get()
        {
            var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

            return Ok(forecasts);


        }
    }
}
