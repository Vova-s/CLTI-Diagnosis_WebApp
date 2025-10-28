namespace CLTI.Diagnosis.Client.Features.Diagnosis.Models
{
    public class YLEData
    {
        // === Критерії 2-річної виживаності (YLE) ===

        // Функціональний статус
        public bool IsNonAmbulatory { get; set; } = false;               // 2.0

        // Стадія Rutherford
        public bool HasRutherford5 { get; set; } = false;                // 1.5
        public bool HasRutherford6 { get; set; } = false;                // 3.0

        // Супутні захворювання
        public bool HasCerebrovascularDisease { get; set; } = false;    // 1.0
        public bool Has2YLEHemodialysis { get; set; } = false;          // 2.0

        // Індекс маси тіла (ІМТ)
        public bool HasBMI18to19 { get; set; } = false;                 // 1.0
        public bool HasBMILessThan18 { get; set; } = false;             // 2.0

        // Вік
        public bool IsAge65to79 { get; set; } = false;                  // 1.5
        public bool IsAge80Plus { get; set; } = false;                  // 3.0

        // Фракція викиду (EF)
        public bool HasEjectionFraction40to49 { get; set; } = false;    // 1.5
        public bool HasEjectionFractionLessThan40 { get; set; } = false;// 2.0

        // === Розрахунок балів ===
        public double CalculateScore()
        {
            double score = 0.0;

            if (IsNonAmbulatory) score += 2.0;
            if (HasRutherford5) score += 1.5;
            if (HasRutherford6) score += 3.0;
            if (HasCerebrovascularDisease) score += 1.0;
            if (Has2YLEHemodialysis) score += 2.0;
            if (HasBMI18to19) score += 1.0;
            if (HasBMILessThan18) score += 2.0;
            if (IsAge65to79) score += 1.5;
            if (IsAge80Plus) score += 3.0;
            if (HasEjectionFraction40to49) score += 1.5;
            if (HasEjectionFractionLessThan40) score += 2.0;

            return score;
        }

        // === Рівень ризику ===
        public string GetRiskLevel()
        {
            var score = CalculateScore();

            if (score <= 3.0)
                return "Низький ризик";
            else if (score <= 6.0)
                return "Помірний ризик";
            else
                return "Високий ризик";
        }

        // === Прогноз виживаності ===
        public string GetSurvivalPrediction()
        {
            var score = CalculateScore();

            if (score <= 3.0)
                return "Висока ймовірність 2-річної виживаності";
            else if (score <= 6.0)
                return "Помірна ймовірність 2-річної виживаності";
            else
                return "Низька ймовірність 2-річної виживаності";
        }

        // === Скидання значень ===
        public void Reset()
        {
            IsNonAmbulatory = false;
            HasRutherford5 = false;
            HasRutherford6 = false;
            HasCerebrovascularDisease = false;
            Has2YLEHemodialysis = false;
            HasBMI18to19 = false;
            HasBMILessThan18 = false;
            IsAge65to79 = false;
            IsAge80Plus = false;
            HasEjectionFraction40to49 = false;
            HasEjectionFractionLessThan40 = false;
        }
    }
}
