using FidenzComfortIndex.Models;

namespace FidenzComfortIndex.Services
{
    public interface IComfortIndexService
    {
        double CalculateScore(double temperatureCelsius, int humidity, double windSpeed, int cloudiness);
    }

    /// <summary>
    /// Computes a 0-100 "Comfort Index" from four weather parameters.
    /// Design: each parameter contributes an independent penalty subtracted from 100,
    /// with weights reflecting how much each factor drives *perceived* comfort:
    ///   Temperature 40% | Humidity 30% | Wind 20% | Cloudiness 10%
    /// Each penalty is capped at its own weight so one bad parameter can't
    /// swing the score more than its share, and the score is clamped to [0,100].
    /// See README for full reasoning and trade-offs.
    /// </summary>
    public class ComfortIndexService : IComfortIndexService
    {
        private const double IdealTempC = 22.5;
        private const double IdealHumidityLow = 40;
        private const double IdealHumidityHigh = 60;
        private const double HumidityMidpoint = 50;
        private const double CalmWindThreshold = 3.0; // m/s, below this = no penalty

        public double CalculateScore(double temperatureCelsius, int humidity, double windSpeed, int cloudiness)
        {
            double tempPenalty = CalculateTemperaturePenalty(temperatureCelsius);
            double humidityPenalty = CalculateHumidityPenalty(humidity);
            double windPenalty = CalculateWindPenalty(windSpeed);
            double cloudPenalty = CalculateCloudPenalty(cloudiness);

            double score = 100 - tempPenalty - humidityPenalty - windPenalty - cloudPenalty;

            return Math.Clamp(Math.Round(score, 1), 0, 100);
        }

        // Weight: 40. Deviation from 22.5C penalized at 2.5 pts/degree, capped at 40.
        private static double CalculateTemperaturePenalty(double tempC)
        {
            double deviation = Math.Abs(tempC - IdealTempC);
            return Math.Min(40, deviation * 2.5);
        }

        // Weight: 30. A 10-point buffer either side of 50% before penalizing, 1 pt per % beyond that.
        private static double CalculateHumidityPenalty(int humidity)
        {
            double deviation = Math.Abs(humidity - HumidityMidpoint);
            double excess = Math.Max(0, deviation - 10);
            return Math.Min(30, excess);
        }

        // Weight: 20. Light breeze (<=3 m/s) is free; every m/s above that costs 4 pts.
        private static double CalculateWindPenalty(double windSpeedMs)
        {
            double excess = Math.Max(0, windSpeedMs - CalmWindThreshold);
            return Math.Min(20, excess * 4);
        }

        // Weight: 10. Smallest weight — overcast skies matter less than temp/humidity/wind.
        private static double CalculateCloudPenalty(int cloudinessPercent)
        {
            return Math.Min(10, cloudinessPercent * 0.1);
        }
    }
}