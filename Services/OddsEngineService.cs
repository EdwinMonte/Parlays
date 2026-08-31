using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parlays.Hubs;
using Parlays.Models;

namespace Parlays.Services
{
    public class OddsEngineService : BackgroundService
    {
        private readonly IOddsDataService _oddsService;
        private readonly IHubContext<OddsHub> _hubContext;
        private readonly IParlayCalculatorService _calculator;
        private readonly ILogger<OddsEngineService> _logger;
        private readonly Random _random = new();

        public OddsEngineService(
            IOddsDataService oddsService,
            IHubContext<OddsHub> hubContext,
            IParlayCalculatorService calculator,
            ILogger<OddsEngineService> logger)
        {
            _oddsService = oddsService;
            _hubContext = hubContext;
            _calculator = calculator;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Motor de Movimientos de Momios (Odds Engine) iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_random.Next(2500, 4200), stoppingToken);

                    var matches = _oddsService.GetAllMatches();
                    if (!matches.Any()) continue;

                    // Select a match to adjust
                    var match = matches[_random.Next(matches.Count)];
                    if (match.Markets.Count == 0) continue;

                    var market = match.Markets[_random.Next(match.Markets.Count)];
                    if (market.Options.Count == 0) continue;

                    var option = market.Options[_random.Next(market.Options.Count)];

                    // Save previous values
                    option.PreviousAmerican = option.CurrentAmerican;
                    option.PreviousDecimal = option.CurrentDecimal;

                    // Odds shift calculation (-15 to +15 step of 5)
                    int delta = _random.Next(1, 4) * 5 * (_random.Next(0, 2) == 0 ? 1 : -1);
                    int newAmerican = option.CurrentAmerican + delta;

                    // Keep odds reasonable
                    if (newAmerican == 0) newAmerican = delta > 0 ? 105 : -105;
                    if (newAmerican > 650) newAmerican = 650;
                    if (newAmerican < -650) newAmerican = -650;

                    option.CurrentAmerican = newAmerican;
                    option.CurrentDecimal = _calculator.AmericanToDecimal(newAmerican);
                    option.Trend = delta > 0 ? OddsTrend.Up : OddsTrend.Down;

                    // Add historical tick
                    var tick = new OddsTick
                    {
                        Timestamp = DateTime.UtcNow,
                        AmericanOdds = newAmerican,
                        DecimalOdds = option.CurrentDecimal,
                        Bookmaker = _random.Next(0, 2) == 0 ? "Pinnacle" : "Bet365"
                    };
                    option.History.Add(tick);
                    if (option.History.Count > 40)
                    {
                        option.History.RemoveAt(0);
                    }

                    // Save updated match
                    _oddsService.UpdateMatch(match);

                    // Broadcast SignalR update
                    await _hubContext.Clients.All.SendAsync("ReceiveOddsUpdate", new
                    {
                        matchId = match.Id,
                        marketId = market.Id,
                        optionId = option.Id,
                        optionName = option.Name,
                        americanOdds = option.CurrentAmerican,
                        decimalOdds = option.CurrentDecimal,
                        impliedProbability = option.ImpliedProbability,
                        shiftPercentage = option.ShiftPercentage,
                        trend = (int)option.Trend,
                        timestamp = DateTime.UtcNow.ToString("HH:mm:ss")
                    }, cancellationToken: stoppingToken);

                    // Randomly simulate a Sharp Money Alert / Steam Move (~18% chance)
                    if (_random.Next(0, 100) < 18)
                    {
                        var alertTypes = new[] { "ReverseLineMovement", "SteamMove", "WhaleVolume" };
                        var chosenType = alertTypes[_random.Next(alertTypes.Length)];
                        
                        string title = chosenType switch
                        {
                            "ReverseLineMovement" => "⚠️ Alerta RLM (Movimiento Inverso)",
                            "SteamMove" => "⚡ Golpe de Vapor (Steam Move Rápido)",
                            _ => "🐋 Entrada Masiva de Capital (Whale Action)"
                        };

                        string desc = chosenType switch
                        {
                            "ReverseLineMovement" => $"El momio de {option.Name} se movió hacia {option.CurrentAmerican:+#;-#;0} con solo 32% de tickets públicos.",
                            "SteamMove" => $"Múltiples casas de apuestas líderes recortaron la cuota de {option.Name} en segundos.",
                            _ => $"Orden de 6 cifras ejecutada en {match.HomeTeam} vs {match.AwayTeam}."
                        };

                        var alert = new SharpAlert
                        {
                            MatchId = match.Id,
                            MatchTitle = $"{match.HomeTeam} vs {match.AwayTeam}",
                            Sport = match.SportName,
                            MarketName = market.DisplayName,
                            SelectionName = option.Name,
                            AlertType = chosenType,
                            Title = title,
                            Description = desc,
                            Severity = "high",
                            OldOdds = option.PreviousAmerican,
                            NewOdds = option.CurrentAmerican,
                            PublicTicketsPercent = _random.Next(20, 45),
                            SharpMoneyPercent = _random.Next(65, 88),
                            Timestamp = DateTime.UtcNow
                        };

                        _oddsService.AddAlert(alert);
                        await _hubContext.Clients.All.SendAsync("ReceiveSharpAlert", alert, cancellationToken: stoppingToken);
                    }

                    // Live match score / clock progression if Live
                    if (match.Status == EventStatus.Live && _random.Next(0, 100) < 25)
                    {
                        if (match.Sport == SportCategory.Soccer)
                        {
                            if (int.TryParse(match.LiveTime.Split('\'')[0], out int min))
                            {
                                min = Math.Min(90, min + 1);
                                match.LiveTime = $"{min}' 2T";
                            }
                        }
                        else if (match.Sport == SportCategory.Basketball)
                        {
                            if (_random.Next(0, 2) == 0) match.HomeScore += _random.Next(2, 4);
                            else match.AwayScore += _random.Next(2, 4);
                        }

                        _oddsService.UpdateMatch(match);
                        await _hubContext.Clients.All.SendAsync("ReceiveScoreUpdate", new
                        {
                            matchId = match.Id,
                            homeScore = match.HomeScore,
                            awayScore = match.AwayScore,
                            liveTime = match.LiveTime
                        }, cancellationToken: stoppingToken);
                    }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error en OddsEngineService");
                }
            }
        }
    }
}
