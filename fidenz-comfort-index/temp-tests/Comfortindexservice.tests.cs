// Place this file in a separate test project (e.g. FidenzComfortIndex.Tests) referencing xUnit.
// dotnet new xunit -n FidenzComfortIndex.Tests
// dotnet add FidenzComfortIndex.Tests reference ../FidenzComfortIndex

using FidenzComfortIndex.Services;
using Xunit;

namespace FidenzComfortIndex.Tests
{
    public class ComfortIndexServiceTests
    {
        private readonly ComfortIndexService _sut = new();

        [Fact]
        public void IdealConditions_ReturnsScoreCloseTo100()
        {
            // 22.5C, 50% humidity, calm wind, clear sky -> minimal penalties
            var score = _sut.CalculateScore(22.5, 50, 1.0, 0);
            Assert.True(score >= 95, $"Expected near-perfect score, got {score}");
        }

        [Fact]
        public void ExtremeHeat_HeavilyPenalized()
        {
            var hotScore = _sut.CalculateScore(45, 50, 1.0, 0);
            var idealScore = _sut.CalculateScore(22.5, 50, 1.0, 0);
            Assert.True(hotScore < idealScore);
        }

        [Fact]
        public void Score_NeverGoesBelowZero()
        {
            var score = _sut.CalculateScore(-30, 100, 40, 100);
            Assert.Equal(0, score);
        }

        [Fact]
        public void Score_NeverExceeds100()
        {
            var score = _sut.CalculateScore(22.5, 50, 0, 0);
            Assert.True(score <= 100);
        }

        [Theory]
        [InlineData(20, 45, 2, 10)]
        [InlineData(30, 70, 8, 60)]
        public void Score_IsAlwaysWithinValidRange(double temp, int humidity, double wind, int clouds)
        {
            var score = _sut.CalculateScore(temp, humidity, wind, clouds);
            Assert.InRange(score, 0, 100);
        }

        [Fact]
        public void HighWindSpeed_ReducesScoreMoreThanCalmWind()
        {
            var calm = _sut.CalculateScore(22.5, 50, 1, 0);
            var windy = _sut.CalculateScore(22.5, 50, 10, 0);
            Assert.True(windy < calm);
        }
    }
}