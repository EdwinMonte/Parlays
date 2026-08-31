using System;
using System.Collections.Generic;

namespace Parlays.Models
{
    public enum SportCategory
    {
        Soccer,
        Basketball,
        AmericanFootball,
        Baseball,
        Tennis,
        MMA,
        Esports
    }

    public enum EventStatus
    {
        Live,
        Upcoming,
        Finished
    }

    public enum OddsTrend
    {
        Stable = 0,
        Up = 1,    // Drift (Momio subió / menos probable)
        Down = -1  // Steam (Momio bajó / entró dinero fuerte)
    }

    public class OddsTick
    {
        public DateTime Timestamp { get; set; }
        public int AmericanOdds { get; set; }
        public decimal DecimalOdds { get; set; }
        public string Bookmaker { get; set; } = "Consensus";
        public string? Note { get; set; }
    }

    public class OddsOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? TargetTeam { get; set; }
        public string? LineParam { get; set; } // e.g. "-1.5", "+2.5", "O 2.5"
        public int OpeningAmerican { get; set; }
        public decimal OpeningDecimal { get; set; }
        public int CurrentAmerican { get; set; }
        public decimal CurrentDecimal { get; set; }
        public int PreviousAmerican { get; set; }
        public decimal PreviousDecimal { get; set; }
        public OddsTrend Trend { get; set; } = OddsTrend.Stable;
        public decimal ImpliedProbability => CurrentAmerican > 0 
            ? Math.Round(100m / (CurrentAmerican + 100m) * 100m, 1)
            : Math.Round((decimal)Math.Abs(CurrentAmerican) / (Math.Abs(CurrentAmerican) + 100m) * 100m, 1);
        public decimal OpeningImpliedProbability => OpeningAmerican > 0
            ? Math.Round(100m / (OpeningAmerican + 100m) * 100m, 1)
            : Math.Round((decimal)Math.Abs(OpeningAmerican) / (Math.Abs(OpeningAmerican) + 100m) * 100m, 1);
        public decimal ShiftPercentage => Math.Round(ImpliedProbability - OpeningImpliedProbability, 1);
        public List<OddsTick> History { get; set; } = new();
    }

    public class MarketOdds
    {
        public string Id { get; set; } = string.Empty;
        public string MarketType { get; set; } = "Moneyline"; // Moneyline, Spread, Totals, BTTS
        public string DisplayName { get; set; } = "Línea de Dinero (1X2)";
        public List<OddsOption> Options { get; set; } = new();
    }

    public class SportsbookQuote
    {
        public string BookmakerId { get; set; } = string.Empty;
        public string BookmakerName { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = "#3b82f6";
        public string LogoUrl { get; set; } = string.Empty;
        public string HomeAmerican { get; set; } = "-110";
        public string DrawAmerican { get; set; } = "+240";
        public string AwayAmerican { get; set; } = "+150";
        public decimal HomeDecimal { get; set; } = 1.91m;
        public decimal DrawDecimal { get; set; } = 3.40m;
        public decimal AwayDecimal { get; set; } = 2.50m;
        public string SpreadText { get; set; } = "-1.5 (-110)";
        public string TotalText { get; set; } = "O 2.5 (-105)";
        public decimal PayoutRate { get; set; } = 96.5m; // Margen de casa
        public bool IsBestHome { get; set; }
        public bool IsBestDraw { get; set; }
        public bool IsBestAway { get; set; }
    }

    public class SharpAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string MatchId { get; set; } = string.Empty;
        public string MatchTitle { get; set; } = string.Empty;
        public string Sport { get; set; } = string.Empty;
        public string MarketName { get; set; } = string.Empty;
        public string SelectionName { get; set; } = string.Empty;
        public string AlertType { get; set; } = "ReverseLineMovement"; // ReverseLineMovement, SteamMove, WhaleVolume
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "high"; // high, medium, info
        public int OldOdds { get; set; }
        public int NewOdds { get; set; }
        public int PublicTicketsPercent { get; set; }
        public int SharpMoneyPercent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class MatchEvent
    {
        public string Id { get; set; } = string.Empty;
        public SportCategory Sport { get; set; }
        public string SportName { get; set; } = "Fútbol";
        public string League { get; set; } = "UEFA Champions League";
        public string LeagueIcon { get; set; } = "trophy";
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public string HomeLogo { get; set; } = string.Empty;
        public string AwayLogo { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public EventStatus Status { get; set; } = EventStatus.Upcoming;
        public string LiveTime { get; set; } = "64'";
        public int HomeScore { get; set; } = 0;
        public int AwayScore { get; set; } = 0;
        public string Stadium { get; set; } = string.Empty;
        public bool IsFeatured { get; set; } = false;
        public bool HasSharpActivity { get; set; } = false;

        // Estadísticas de apuestas (Público vs Smart Money)
        public int PublicTicketsHomePercent { get; set; } = 65;
        public int PublicTicketsAwayPercent { get; set; } = 35;
        public int MoneyHandleHomePercent { get; set; } = 38;
        public int MoneyHandleAwayPercent { get; set; } = 62; // Indicador RLM

        public List<MarketOdds> Markets { get; set; } = new();
        public List<SportsbookQuote> Sportsbooks { get; set; } = new();
        public List<SharpAlert> ActiveAlerts { get; set; } = new();
    }

    public class ParlayLegRequest
    {
        public string MatchId { get; set; } = string.Empty;
        public string MatchTitle { get; set; } = string.Empty;
        public string MarketType { get; set; } = string.Empty;
        public string SelectionId { get; set; } = string.Empty;
        public string SelectionName { get; set; } = string.Empty;
        public int AmericanOdds { get; set; }
        public decimal DecimalOdds { get; set; }
    }

    public class ParlayCalculationRequest
    {
        public decimal Stake { get; set; } = 100m;
        public List<ParlayLegRequest> Legs { get; set; } = new();
    }

    public class ParlayCalculationResult
    {
        public decimal Stake { get; set; }
        public int LegsCount { get; set; }
        public decimal CombinedDecimalOdds { get; set; }
        public int CombinedAmericanOdds { get; set; }
        public decimal RawPayout { get; set; }
        public decimal RawProfit { get; set; }
        public decimal BonusPercentage { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal FinalPayout { get; set; }
        public decimal FinalProfit { get; set; }
        public decimal ImpliedWinProbability { get; set; }
        public decimal ExpectedValuePercentage { get; set; }
        public string HedgeRecommendation { get; set; } = string.Empty;
    }
}
