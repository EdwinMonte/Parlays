using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Parlays.Models;

namespace Parlays.Services
{
    public interface IOddsDataService
    {
        List<MatchEvent> GetAllMatches();
        MatchEvent? GetMatchById(string id);
        List<SharpAlert> GetRecentAlerts();
        void AddAlert(SharpAlert alert);
        void UpdateMatch(MatchEvent match);
        List<OddsTick> GetHistoricalTicks(string matchId, string optionId);
    }

    public class OddsDataService : IOddsDataService
    {
        private readonly ConcurrentDictionary<string, MatchEvent> _matches = new();
        private readonly List<SharpAlert> _alerts = new();
        private readonly object _lock = new();

        public OddsDataService()
        {
            InitializeData();
        }

        public List<MatchEvent> GetAllMatches()
        {
            return _matches.Values.OrderByDescending(m => m.Status == EventStatus.Live)
                                  .ThenBy(m => m.StartTime)
                                  .ToList();
        }

        public MatchEvent? GetMatchById(string id)
        {
            _matches.TryGetValue(id, out var match);
            return match;
        }

        public List<SharpAlert> GetRecentAlerts()
        {
            lock (_lock)
            {
                return _alerts.OrderByDescending(a => a.Timestamp).Take(20).ToList();
            }
        }

        public void AddAlert(SharpAlert alert)
        {
            lock (_lock)
            {
                _alerts.Insert(0, alert);
                if (_alerts.Count > 50) _alerts.RemoveAt(_alerts.Count - 1);
            }
        }

        public void UpdateMatch(MatchEvent match)
        {
            _matches[match.Id] = match;
        }

        public List<OddsTick> GetHistoricalTicks(string matchId, string optionId)
        {
            var match = GetMatchById(matchId);
            if (match == null) return new List<OddsTick>();

            foreach (var market in match.Markets)
            {
                var opt = market.Options.FirstOrDefault(o => o.Id == optionId);
                if (opt != null)
                {
                    return opt.History.OrderBy(t => t.Timestamp).ToList();
                }
            }
            return new List<OddsTick>();
        }

        private void InitializeData()
        {
            var now = DateTime.UtcNow;

            // 1. Real Madrid vs Manchester City (UCL - LIVE)
            var m1 = new MatchEvent
            {
                Id = "ucl-rm-mci",
                Sport = SportCategory.Soccer,
                SportName = "Fútbol",
                League = "UEFA Champions League",
                LeagueIcon = "⚽",
                HomeTeam = "Real Madrid",
                AwayTeam = "Manchester City",
                HomeLogo = "👑",
                AwayLogo = "🦈",
                StartTime = now.AddMinutes(-68),
                Status = EventStatus.Live,
                LiveTime = "68' 2T",
                HomeScore = 2,
                AwayScore = 1,
                Stadium = "Santiago Bernabéu",
                IsFeatured = true,
                HasSharpActivity = true,
                PublicTicketsHomePercent = 71,
                PublicTicketsAwayPercent = 29,
                MoneyHandleHomePercent = 44,
                MoneyHandleAwayPercent = 56 // RLM: Public likes RM, Heavy Sharp on City
            };

            m1.Markets.Add(Create1X2Market(
                "ucl-rm-mci-1x2",
                "Real Madrid", "Empate", "Manchester City",
                openingHome: +145, currentHome: -160,
                openingDraw: +260, currentDraw: +280,
                openingAway: +180, currentAway: +380
            ));

            m1.Markets.Add(CreateTotalsMarket(
                "ucl-rm-mci-tot", "3.5",
                openingOver: +110, currentOver: -135,
                openingUnder: -130, currentUnder: +115
            ));

            m1.Sportsbooks = CreateSportsbookQuotes(
                bestHome: "-155 (Bet365)",
                bestDraw: "+295 (Pinnacle)",
                bestAway: "+400 (Caliente)"
            );

            // 2. Arsenal vs Bayern Munich (UCL - Today)
            var m2 = new MatchEvent
            {
                Id = "ucl-ars-bay",
                Sport = SportCategory.Soccer,
                SportName = "Fútbol",
                League = "UEFA Champions League",
                LeagueIcon = "⚽",
                HomeTeam = "Arsenal",
                AwayTeam = "Bayern Múnich",
                HomeLogo = "🔴",
                AwayLogo = "🛡️",
                StartTime = now.AddHours(2.5),
                Status = EventStatus.Upcoming,
                Stadium = "Emirates Stadium",
                IsFeatured = true,
                HasSharpActivity = true,
                PublicTicketsHomePercent = 58,
                PublicTicketsAwayPercent = 42,
                MoneyHandleHomePercent = 78, // Steam move on Arsenal
                MoneyHandleAwayPercent = 22
            };

            m2.Markets.Add(Create1X2Market(
                "ucl-ars-bay-1x2",
                "Arsenal", "Empate", "Bayern Múnich",
                openingHome: -105, currentHome: -138,
                openingDraw: +275, currentDraw: +290,
                openingAway: +265, currentAway: +320
            ));

            m2.Markets.Add(CreateSpreadMarket(
                "ucl-ars-bay-ah",
                "Arsenal -0.75", "Bayern +0.75",
                openingHome: -110, currentHome: -125,
                openingAway: -110, currentAway: +105
            ));

            m2.Sportsbooks = CreateSportsbookQuotes("-132 (Pinnacle)", "+300 (1xBet)", "+335 (DraftKings)");

            // 3. Boston Celtics vs Los Angeles Lakers (NBA - Live)
            var m3 = new MatchEvent
            {
                Id = "nba-bos-lal",
                Sport = SportCategory.Basketball,
                SportName = "Baloncesto",
                League = "NBA - Temporada Regular",
                LeagueIcon = "🏀",
                HomeTeam = "Boston Celtics",
                AwayTeam = "LA Lakers",
                HomeLogo = "☘️",
                AwayLogo = "👑",
                StartTime = now.AddMinutes(-40),
                Status = EventStatus.Live,
                LiveTime = "3Q 04:12",
                HomeScore = 78,
                AwayScore = 74,
                Stadium = "TD Garden",
                IsFeatured = true,
                HasSharpActivity = true,
                PublicTicketsHomePercent = 63,
                PublicTicketsAwayPercent = 37,
                MoneyHandleHomePercent = 31,
                MoneyHandleAwayPercent = 69
            };

            m3.Markets.Add(CreateMoneyline2Way(
                "nba-bos-lal-ml",
                "Boston Celtics", "LA Lakers",
                openingHome: -220, currentHome: -175,
                openingAway: +180, currentAway: +145
            ));

            m3.Markets.Add(CreateSpreadMarket(
                "nba-bos-lal-spr",
                "Celtics -5.5", "Lakers +5.5",
                openingHome: -110, currentHome: -105,
                openingAway: -110, currentAway: -115
            ));

            m3.Markets.Add(CreateTotalsMarket(
                "nba-bos-lal-tot", "228.5",
                openingOver: -110, currentOver: -120,
                openingUnder: -110, currentUnder: +100
            ));

            m3.Sportsbooks = CreateSportsbookQuotes("-170 (FanDuel)", "", "+152 (Pinnacle)");

            // 4. Golden State Warriors vs Dallas Mavericks (NBA - Upcoming)
            var m4 = new MatchEvent
            {
                Id = "nba-gsw-dal",
                Sport = SportCategory.Basketball,
                SportName = "Baloncesto",
                League = "NBA - Temporada Regular",
                LeagueIcon = "🏀",
                HomeTeam = "GS Warriors",
                AwayTeam = "Dallas Mavericks",
                HomeLogo = "🌉",
                AwayLogo = "🐎",
                StartTime = now.AddHours(4),
                Status = EventStatus.Upcoming,
                Stadium = "Chase Center",
                IsFeatured = false,
                HasSharpActivity = false,
                PublicTicketsHomePercent = 52,
                PublicTicketsAwayPercent = 48,
                MoneyHandleHomePercent = 50,
                MoneyHandleAwayPercent = 50
            };

            m4.Markets.Add(CreateMoneyline2Way(
                "nba-gsw-dal-ml",
                "GS Warriors", "Dallas Mavericks",
                openingHome: -130, currentHome: -115,
                openingAway: +110, currentAway: -105
            ));

            m4.Markets.Add(CreateTotalsMarket(
                "nba-gsw-dal-tot", "234.5",
                openingOver: -110, currentOver: +102,
                openingUnder: -110, currentUnder: -122
            ));

            m4.Sportsbooks = CreateSportsbookQuotes("-112 (Bet365)", "", "-102 (DraftKings)");

            // 5. Club América vs Chivas Guadalajara (Liga MX - Clásico Nacional)
            var m5 = new MatchEvent
            {
                Id = "ligamx-ame-chi",
                Sport = SportCategory.Soccer,
                SportName = "Fútbol",
                League = "Liga MX - Torneo Clausura",
                LeagueIcon = "🇲🇽",
                HomeTeam = "Club América",
                AwayTeam = "Chivas Guadalajara",
                HomeLogo = "🦅",
                AwayLogo = "🐐",
                StartTime = now.AddHours(6),
                Status = EventStatus.Upcoming,
                Stadium = "Estadio Azteca",
                IsFeatured = true,
                HasSharpActivity = true,
                PublicTicketsHomePercent = 67,
                PublicTicketsAwayPercent = 33,
                MoneyHandleHomePercent = 82, // Heavy Steam on América
                MoneyHandleAwayPercent = 18
            };

            m5.Markets.Add(Create1X2Market(
                "ligamx-ame-chi-1x2",
                "Club América", "Empate", "Chivas Guadalajara",
                openingHome: +105, currentHome: -140,
                openingDraw: +240, currentDraw: +265,
                openingAway: +250, currentAway: +340
            ));

            m5.Markets.Add(CreateTotalsMarket(
                "ligamx-ame-chi-tot", "2.5",
                openingOver: -105, currentOver: -130,
                openingUnder: -115, currentUnder: +110
            ));

            m5.Sportsbooks = CreateSportsbookQuotes("-135 (Caliente)", "+270 (Bet365)", "+355 (Pinnacle)");

            // 6. Kansas City Chiefs vs San Francisco 49ers (NFL)
            var m6 = new MatchEvent
            {
                Id = "nfl-kc-sf",
                Sport = SportCategory.AmericanFootball,
                SportName = "Fútbol Americano",
                League = "NFL - Sunday Primetime",
                LeagueIcon = "🏈",
                HomeTeam = "KC Chiefs",
                AwayTeam = "SF 49ers",
                HomeLogo = "🏹",
                AwayLogo = "⛏️",
                StartTime = now.AddDays(1).AddHours(3),
                Status = EventStatus.Upcoming,
                Stadium = "GEHA Field at Arrowhead",
                IsFeatured = true,
                HasSharpActivity = true,
                PublicTicketsHomePercent = 75,
                PublicTicketsAwayPercent = 25,
                MoneyHandleHomePercent = 41,
                MoneyHandleAwayPercent = 59 // Massive Sharp on 49ers
            };

            m6.Markets.Add(CreateMoneyline2Way(
                "nfl-kc-sf-ml",
                "KC Chiefs", "SF 49ers",
                openingHome: -160, currentHome: -125,
                openingAway: +135, currentAway: +105
            ));

            m6.Markets.Add(CreateSpreadMarket(
                "nfl-kc-sf-spr",
                "Chiefs -2.5", "49ers +2.5",
                openingHome: -110, currentHome: -108,
                openingAway: -110, currentAway: -112
            ));

            m6.Sportsbooks = CreateSportsbookQuotes("-120 (FanDuel)", "", "+110 (Pinnacle)");

            // 7. NY Yankees vs LA Dodgers (MLB)
            var m7 = new MatchEvent
            {
                Id = "mlb-nyy-lad",
                Sport = SportCategory.Baseball,
                SportName = "Béisbol",
                League = "MLB - Grandes Ligas",
                LeagueIcon = "⚾",
                HomeTeam = "NY Yankees",
                AwayTeam = "LA Dodgers",
                HomeLogo = "🗽",
                AwayLogo = "⚾",
                StartTime = now.AddHours(5),
                Status = EventStatus.Upcoming,
                Stadium = "Yankee Stadium",
                IsFeatured = false,
                HasSharpActivity = false,
                PublicTicketsHomePercent = 51,
                PublicTicketsAwayPercent = 49,
                MoneyHandleHomePercent = 48,
                MoneyHandleAwayPercent = 52
            };

            m7.Markets.Add(CreateMoneyline2Way(
                "mlb-nyy-lad-ml",
                "NY Yankees", "LA Dodgers",
                openingHome: +100, currentHome: -110,
                openingAway: -120, currentAway: -110
            ));

            m7.Sportsbooks = CreateSportsbookQuotes("-105 (DraftKings)", "", "-105 (Bet365)");

            // 8. Carlos Alcaraz vs Jannik Sinner (Tennis)
            var m8 = new MatchEvent
            {
                Id = "ten-alc-sin",
                Sport = SportCategory.Tennis,
                SportName = "Tenis",
                League = "ATP Masters 1000 - Final",
                LeagueIcon = "🎾",
                HomeTeam = "Carlos Alcaraz",
                AwayTeam = "Jannik Sinner",
                HomeLogo = "🇪🇸",
                AwayLogo = "🇮🇹",
                StartTime = now.AddDays(1).AddHours(8),
                Status = EventStatus.Upcoming,
                Stadium = "Court Central",
                IsFeatured = true,
                HasSharpActivity = true,
                PublicTicketsHomePercent = 54,
                PublicTicketsAwayPercent = 46,
                MoneyHandleHomePercent = 68,
                MoneyHandleAwayPercent = 32
            };

            m8.Markets.Add(CreateMoneyline2Way(
                "ten-alc-sin-ml",
                "Carlos Alcaraz", "Jannik Sinner",
                openingHome: -110, currentHome: -145,
                openingAway: -110, currentAway: +120
            ));

            m8.Sportsbooks = CreateSportsbookQuotes("-140 (Pinnacle)", "", "+126 (1xBet)");

            // Store in dictionary
            _matches[m1.Id] = m1;
            _matches[m2.Id] = m2;
            _matches[m3.Id] = m3;
            _matches[m4.Id] = m4;
            _matches[m5.Id] = m5;
            _matches[m6.Id] = m6;
            _matches[m7.Id] = m7;
            _matches[m8.Id] = m8;

            // Pre-seed initial sharp alerts
            AddAlert(new SharpAlert
            {
                MatchId = m1.Id,
                MatchTitle = "Real Madrid vs Man City",
                Sport = "Fútbol",
                MarketName = "Línea de Dinero (1X2)",
                SelectionName = "Manchester City",
                AlertType = "ReverseLineMovement",
                Title = "Movimiento Inverso de Línea (RLM)",
                Description = "71% de boletos en Real Madrid, pero el 56% del dinero fuerte entró en Man City. La cuota del visitante se apretó.",
                Severity = "high",
                OldOdds = +180,
                NewOdds = +140,
                PublicTicketsPercent = 29,
                SharpMoneyPercent = 56,
                Timestamp = now.AddMinutes(-25)
            });

            AddAlert(new SharpAlert
            {
                MatchId = m2.Id,
                MatchTitle = "Arsenal vs Bayern Múnich",
                Sport = "Fútbol",
                MarketName = "1X2 (Match Odds)",
                SelectionName = "Arsenal",
                AlertType = "SteamMove",
                Title = "Golpe de Vapor (Steam Move)",
                Description = "Apuestas de alto volumen profesional cayeron simultáneamente en Pinnacle y Circa a favor de Arsenal.",
                Severity = "high",
                OldOdds = -105,
                NewOdds = -138,
                PublicTicketsPercent = 58,
                SharpMoneyPercent = 78,
                Timestamp = now.AddMinutes(-12)
            });

            AddAlert(new SharpAlert
            {
                MatchId = m6.Id,
                MatchTitle = "KC Chiefs vs SF 49ers",
                Sport = "Fútbol Americano",
                MarketName = "Moneyline",
                SelectionName = "SF 49ers",
                AlertType = "ReverseLineMovement",
                Title = "Dinero Profesional (Whale Action)",
                Description = "El público masivo apuesta a Chiefs (75%), pero los sindicatos de Las Vegas inyectaron el 59% del capital a 49ers (+105).",
                Severity = "high",
                OldOdds = +135,
                NewOdds = +105,
                PublicTicketsPercent = 25,
                SharpMoneyPercent = 59,
                Timestamp = now.AddMinutes(-5)
            });
        }

        private MarketOdds Create1X2Market(
            string marketId,
            string home, string draw, string away,
            int openingHome, int currentHome,
            int openingDraw, int currentDraw,
            int openingAway, int currentAway)
        {
            var m = new MarketOdds
            {
                Id = marketId,
                MarketType = "1X2",
                DisplayName = "Resultado Final (1X2)"
            };

            m.Options.Add(BuildOption($"{marketId}-1", home, home, openingHome, currentHome));
            m.Options.Add(BuildOption($"{marketId}-X", draw, draw, openingDraw, currentDraw));
            m.Options.Add(BuildOption($"{marketId}-2", away, away, openingAway, currentAway));
            return m;
        }

        private MarketOdds CreateMoneyline2Way(
            string marketId,
            string home, string away,
            int openingHome, int currentHome,
            int openingAway, int currentAway)
        {
            var m = new MarketOdds
            {
                Id = marketId,
                MarketType = "Moneyline",
                DisplayName = "Ganador del Partido (Moneyline)"
            };

            m.Options.Add(BuildOption($"{marketId}-1", home, home, openingHome, currentHome));
            m.Options.Add(BuildOption($"{marketId}-2", away, away, openingAway, currentAway));
            return m;
        }

        private MarketOdds CreateSpreadMarket(
            string marketId,
            string homeSpread, string awaySpread,
            int openingHome, int currentHome,
            int openingAway, int currentAway)
        {
            var m = new MarketOdds
            {
                Id = marketId,
                MarketType = "Spread",
                DisplayName = "Hándicap / Spread",
            };

            m.Options.Add(BuildOption($"{marketId}-1", homeSpread, homeSpread, openingHome, currentHome));
            m.Options.Add(BuildOption($"{marketId}-2", awaySpread, awaySpread, openingAway, currentAway));
            return m;
        }

        private MarketOdds CreateTotalsMarket(
            string marketId, string totalLine,
            int openingOver, int currentOver,
            int openingUnder, int currentUnder)
        {
            var m = new MarketOdds
            {
                Id = marketId,
                MarketType = "Totals",
                DisplayName = $"Total Puntos / Goles ({totalLine})"
            };

            m.Options.Add(BuildOption($"{marketId}-over", $"Altas +{totalLine}", $"Over {totalLine}", openingOver, currentOver));
            m.Options.Add(BuildOption($"{marketId}-under", $"Bajas -{totalLine}", $"Under {totalLine}", openingUnder, currentUnder));
            return m;
        }

        private OddsOption BuildOption(string id, string name, string target, int openAm, int curAm)
        {
            var openDec = ConvertToDec(openAm);
            var curDec = ConvertToDec(curAm);

            var opt = new OddsOption
            {
                Id = id,
                Name = name,
                TargetTeam = target,
                OpeningAmerican = openAm,
                OpeningDecimal = openDec,
                PreviousAmerican = openAm,
                PreviousDecimal = openDec,
                CurrentAmerican = curAm,
                CurrentDecimal = curDec,
                Trend = curAm < openAm ? OddsTrend.Down : (curAm > openAm ? OddsTrend.Up : OddsTrend.Stable)
            };

            // Generate 8-10 historical data points representing smooth line evolution
            var baseTime = DateTime.UtcNow.AddHours(-12);
            int steps = 10;
            for (int i = 0; i <= steps; i++)
            {
                double factor = (double)i / steps;
                int interpolated = (int)Math.Round(openAm + (curAm - openAm) * factor);
                // slight jitter for realism
                if (i > 0 && i < steps)
                {
                    interpolated += (i % 2 == 0 ? 5 : -5);
                }
                opt.History.Add(new OddsTick
                {
                    Timestamp = baseTime.AddMinutes(i * 70),
                    AmericanOdds = interpolated,
                    DecimalOdds = ConvertToDec(interpolated),
                    Bookmaker = i % 3 == 0 ? "Pinnacle" : (i % 2 == 0 ? "Bet365" : "Consenso"),
                    Note = i == steps ? "Momio Actual" : (i == 0 ? "Apertura" : null)
                });
            }

            return opt;
        }

        private decimal ConvertToDec(int am)
        {
            if (am == 0) return 1.0m;
            if (am > 0) return Math.Round(1.0m + ((decimal)am / 100m), 2);
            return Math.Round(1.0m + (100m / (decimal)Math.Abs(am)), 2);
        }

        private List<SportsbookQuote> CreateSportsbookQuotes(string bestHome, string bestDraw, string bestAway)
        {
            return new List<SportsbookQuote>
            {
                new() { BookmakerId = "pinnacle", BookmakerName = "Pinnacle", BadgeColor = "#ff7700", HomeAmerican = bestHome.Contains("Pinnacle") ? bestHome : "-115", DrawAmerican = "+285", AwayAmerican = "+240", PayoutRate = 98.2m, IsBestHome = bestHome.Contains("Pinnacle"), IsBestDraw = bestDraw.Contains("Pinnacle"), IsBestAway = bestAway.Contains("Pinnacle") },
                new() { BookmakerId = "bet365", BookmakerName = "Bet365", BadgeColor = "#007b5e", HomeAmerican = bestHome.Contains("Bet365") ? bestHome : "-118", DrawAmerican = "+275", AwayAmerican = "+235", PayoutRate = 96.8m, IsBestHome = bestHome.Contains("Bet365"), IsBestDraw = bestDraw.Contains("Bet365"), IsBestAway = bestAway.Contains("Bet365") },
                new() { BookmakerId = "caliente", BookmakerName = "Caliente.mx", BadgeColor = "#e50014", HomeAmerican = bestHome.Contains("Caliente") ? bestHome : "-120", DrawAmerican = "+260", AwayAmerican = bestAway.Contains("Caliente") ? bestAway : "+230", PayoutRate = 95.5m, IsBestHome = bestHome.Contains("Caliente"), IsBestDraw = bestDraw.Contains("Caliente"), IsBestAway = bestAway.Contains("Caliente") },
                new() { BookmakerId = "draftkings", BookmakerName = "DraftKings", BadgeColor = "#52b848", HomeAmerican = bestHome.Contains("DraftKings") ? bestHome : "-115", DrawAmerican = "+280", AwayAmerican = bestAway.Contains("DraftKings") ? bestAway : "+245", PayoutRate = 96.1m, IsBestHome = bestHome.Contains("DraftKings"), IsBestDraw = bestDraw.Contains("DraftKings"), IsBestAway = bestAway.Contains("DraftKings") },
                new() { BookmakerId = "fanduel", BookmakerName = "FanDuel", BadgeColor = "#1493ff", HomeAmerican = bestHome.Contains("FanDuel") ? bestHome : "-116", DrawAmerican = "+270", AwayAmerican = bestAway.Contains("FanDuel") ? bestAway : "+238", PayoutRate = 96.4m, IsBestHome = bestHome.Contains("FanDuel"), IsBestDraw = bestDraw.Contains("FanDuel"), IsBestAway = bestAway.Contains("FanDuel") }
            };
        }
    }
}
