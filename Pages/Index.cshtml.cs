using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Parlays.Models;
using Parlays.Services;

namespace Parlays.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IOddsDataService _oddsService;

        public IndexModel(IOddsDataService oddsService)
        {
            _oddsService = oddsService;
        }

        public List<MatchEvent> Matches { get; set; } = new();
        public List<SharpAlert> RecentAlerts { get; set; } = new();

        public int LiveCount => Matches.FindAll(m => m.Status == EventStatus.Live).Count;
        public int SharpActivityCount => Matches.FindAll(m => m.HasSharpActivity).Count;

        public void OnGet()
        {
            Matches = _oddsService.GetAllMatches();
            RecentAlerts = _oddsService.GetRecentAlerts();
        }
    }
}
