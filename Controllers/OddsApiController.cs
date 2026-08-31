using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Parlays.Models;
using Parlays.Services;

namespace Parlays.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OddsApiController : ControllerBase
    {
        private readonly IOddsDataService _oddsService;
        private readonly IParlayCalculatorService _calculator;

        public OddsApiController(IOddsDataService oddsService, IParlayCalculatorService calculator)
        {
            _oddsService = oddsService;
            _calculator = calculator;
        }

        [HttpGet("matches")]
        public IActionResult GetMatches([FromQuery] string? sport, [FromQuery] string? status)
        {
            var matches = _oddsService.GetAllMatches();

            if (!string.IsNullOrEmpty(sport) && sport.ToLower() != "all")
            {
                matches = matches.Where(m => m.Sport.ToString().Equals(sport, StringComparison.OrdinalIgnoreCase) ||
                                             m.SportName.Equals(sport, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
            {
                if (Enum.TryParse<EventStatus>(status, true, out var parsedStatus))
                {
                    matches = matches.Where(m => m.Status == parsedStatus).ToList();
                }
            }

            return Ok(matches);
        }

        [HttpGet("match/{id}")]
        public IActionResult GetMatch(string id)
        {
            var match = _oddsService.GetMatchById(id);
            if (match == null) return NotFound(new { message = "Partido no encontrado" });
            return Ok(match);
        }

        [HttpGet("history")]
        public IActionResult GetHistory([FromQuery] string matchId, [FromQuery] string optionId)
        {
            var ticks = _oddsService.GetHistoricalTicks(matchId, optionId);
            return Ok(ticks);
        }

        [HttpGet("alerts")]
        public IActionResult GetAlerts()
        {
            var alerts = _oddsService.GetRecentAlerts();
            return Ok(alerts);
        }

        [HttpPost("calculate-parlay")]
        public IActionResult CalculateParlay([FromBody] ParlayCalculationRequest request)
        {
            var result = _calculator.CalculateParlay(request);
            return Ok(result);
        }

        [HttpGet("convert")]
        public IActionResult ConvertOdds([FromQuery] string format, [FromQuery] decimal value)
        {
            if (format.ToLower() == "american")
            {
                int am = (int)value;
                decimal dec = _calculator.AmericanToDecimal(am);
                decimal prob = _calculator.CalculateImpliedProbability(am);
                return Ok(new { american = am, @decimal = dec, probability = prob });
            }
            else
            {
                decimal dec = value;
                int am = _calculator.DecimalToAmerican(dec);
                decimal prob = dec > 0 ? Math.Round(100m / dec, 2) : 0;
                return Ok(new { american = am, @decimal = dec, probability = prob });
            }
        }
    }
}
