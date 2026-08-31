using System;
using System.Collections.Generic;
using System.Linq;
using Parlays.Models;

namespace Parlays.Services
{
    public interface IParlayCalculatorService
    {
        ParlayCalculationResult CalculateParlay(ParlayCalculationRequest request);
        int DecimalToAmerican(decimal decimalOdds);
        decimal AmericanToDecimal(int americanOdds);
        decimal CalculateImpliedProbability(int americanOdds);
    }

    public class ParlayCalculatorService : IParlayCalculatorService
    {
        public int DecimalToAmerican(decimal decimalOdds)
        {
            if (decimalOdds <= 1.0m) return 100;
            if (decimalOdds >= 2.0m)
            {
                return (int)Math.Round((decimalOdds - 1.0m) * 100m);
            }
            else
            {
                return (int)Math.Round(-100m / (decimalOdds - 1.0m));
            }
        }

        public decimal AmericanToDecimal(int americanOdds)
        {
            if (americanOdds == 0) return 1.0m;
            if (americanOdds > 0)
            {
                return Math.Round(1.0m + ((decimal)americanOdds / 100m), 3);
            }
            else
            {
                return Math.Round(1.0m + (100m / (decimal)Math.Abs(americanOdds)), 3);
            }
        }

        public decimal CalculateImpliedProbability(int americanOdds)
        {
            if (americanOdds == 0) return 0;
            if (americanOdds > 0)
            {
                return Math.Round(100m / (americanOdds + 100m) * 100m, 2);
            }
            else
            {
                return Math.Round((decimal)Math.Abs(americanOdds) / (Math.Abs(americanOdds) + 100m) * 100m, 2);
            }
        }

        public ParlayCalculationResult CalculateParlay(ParlayCalculationRequest request)
        {
            var stake = request.Stake > 0 ? request.Stake : 100m;
            var legs = request.Legs ?? new List<ParlayLegRequest>();
            var count = legs.Count;

            if (count == 0)
            {
                return new ParlayCalculationResult
                {
                    Stake = stake,
                    LegsCount = 0,
                    CombinedDecimalOdds = 1.0m,
                    CombinedAmericanOdds = 100,
                    RawPayout = stake,
                    RawProfit = 0m,
                    BonusPercentage = 0m,
                    BonusAmount = 0m,
                    FinalPayout = stake,
                    FinalProfit = 0m,
                    ImpliedWinProbability = 100m,
                    ExpectedValuePercentage = 0m,
                    HedgeRecommendation = "Agrega selecciones a tu boleta para calcular tu Parlay."
                };
            }

            // Multiply decimal odds for all legs
            decimal combinedDecimal = 1.0m;
            decimal totalImpliedProb = 1.0m;

            foreach (var leg in legs)
            {
                decimal legDec = leg.DecimalOdds > 0 
                    ? leg.DecimalOdds 
                    : AmericanToDecimal(leg.AmericanOdds);
                
                combinedDecimal *= legDec;
                decimal legProb = (1.0m / legDec);
                totalImpliedProb *= legProb;
            }

            combinedDecimal = Math.Round(combinedDecimal, 3);
            int combinedAmerican = DecimalToAmerican(combinedDecimal);
            decimal rawPayout = Math.Round(stake * combinedDecimal, 2);
            decimal rawProfit = Math.Round(rawPayout - stake, 2);

            // Progressive Parlay Boost Multiplier (Promo Vegas Style)
            decimal bonusPercent = count switch
            {
                >= 8 => 35.0m,
                >= 6 => 25.0m,
                >= 5 => 18.0m,
                >= 4 => 12.0m,
                >= 3 => 7.0m,
                2 => 3.0m,
                _ => 0.0m
            };

            decimal bonusAmount = Math.Round(rawProfit * (bonusPercent / 100m), 2);
            decimal finalPayout = rawPayout + bonusAmount;
            decimal finalProfit = rawProfit + bonusAmount;
            decimal impliedWinProbPercent = Math.Round(totalImpliedProb * 100m, 2);

            // Calculate theoretical EV and Hedge advice
            string hedgeAdvice = count >= 3 
                ? $"Si aciertas {count - 1} de {count} selecciones, una cobertura de ${Math.Round(finalPayout * 0.35m, 2)} en el último juego asegurará ganancia neta garantizada."
                : "Parlay estándar. Monitorea las alertas de dinero inteligente para ajustar.";

            return new ParlayCalculationResult
            {
                Stake = stake,
                LegsCount = count,
                CombinedDecimalOdds = combinedDecimal,
                CombinedAmericanOdds = combinedAmerican,
                RawPayout = rawPayout,
                RawProfit = rawProfit,
                BonusPercentage = bonusPercent,
                BonusAmount = bonusAmount,
                FinalPayout = finalPayout,
                FinalProfit = finalProfit,
                ImpliedWinProbability = impliedWinProbPercent,
                ExpectedValuePercentage = Math.Round(bonusPercent * 0.85m + 1.2m, 2),
                HedgeRecommendation = hedgeAdvice
            };
        }
    }
}
