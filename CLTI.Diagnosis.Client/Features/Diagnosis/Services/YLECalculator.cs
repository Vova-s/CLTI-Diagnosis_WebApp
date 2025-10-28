using CLTI.Diagnosis.Client.Features.Diagnosis.Models;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Services
{
    public class YLECalculator
    {
        private readonly YLEData _yleData;

        public YLECalculator(YLEData yleData)
        {
            _yleData = yleData;
        }

        public double CalculateScore()
        {
            return _yleData.CalculateScore();
        }

        public string GetRiskLevel()
        {
            return _yleData.GetRiskLevel();
        }

        public string GetSurvivalPrediction()
        {
            return _yleData.GetSurvivalPrediction();
        }

        public string GetRiskDescription()
        {
            var score = CalculateScore();

            if (score <= 3.0)
                return "Низький ризик смертності протягом 2 років (≤3,0 балів)";
            else if (score > 3.0 && score <= 6.0)
                return "Помірний ризик смертності протягом 2 років (3,1-6,0 балів)";
            else if (score > 6.0)
                return "Високий ризик смертності протягом 2 років (>6,0 балів)";
            else
                return "Невизначений ризик смертності";
        }

        public double GetEstimatedSurvivalPercentage()
        {
            var score = CalculateScore();

            // Приблизна оцінка 2-річної виживаності на основі балів
            if (score <= 3.0)
                return 90.0;      // Висока виживаність ~90%
            else if (score > 3.0 && score <= 6.0)
                return 70.0;      // Помірна виживаність ~70%
            else if (score > 6.0)
                return 50.0;      // Низька виживаність ~50%
            else
                return 0.0;
        }
    }
}