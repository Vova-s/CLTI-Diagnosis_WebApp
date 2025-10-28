using CLTI.Diagnosis.Client.Features.Diagnosis.Models;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Services
{
    public class CRABCalculator
    {
        private readonly CRABData _crabData;

        public CRABCalculator(CRABData crabData)
        {
            _crabData = crabData;
        }

        public int CalculateScore()
        {
            return _crabData.CalculateScore();
        }

        public string GetRiskLevel()
        {
            return _crabData.GetRiskLevel();
        }

        public string GetRiskDescription()
        {
            var score = CalculateScore();

            if (score <= 6)
                return "Низький ризик періпроцедуральної смертності (≤6 балів)";
            else if (score >= 7 && score <= 10)
                return "Помірний ризик періпроцедуральної смертності (7-10 балів)";
            else if (score >= 11)
                return "Високий ризик періпроцедуральної смертності (≥11 балів)";
            else
                return "Невизначений ризик періпроцедуральної смертності";
        }

        public double GetMortalityRiskPercentage()
        {
            var score = CalculateScore();

            // Приблизні відсотки ризику на основі CRAB шкали
            if (score <= 6)
                return 2.0;      // Низький ризик ~2%
            else if (score >= 7 && score <= 10)
                return 8.0;      // Помірний ризик ~8%
            else if (score >= 11)
                return 20.0;     // Високий ризик ~20%
            else
                return 0.0;
        }
    }
}