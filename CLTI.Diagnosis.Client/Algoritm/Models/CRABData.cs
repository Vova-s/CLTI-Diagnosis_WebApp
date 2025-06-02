namespace CLTI.Diagnosis.Client.Algoritm.Models
{
    public class CRABData
    {
        // CRAB критерії (3 бали кожен)
        public bool IsOlderThan75 { get; set; } = false;
        public bool HasPreviousAmputationOrRevascularization { get; set; } = false;
        public bool HasPainAndNecrosis { get; set; } = false;
        public bool HasPartialFunctionalDependence { get; set; } = false;

        // CRAB критерії (4 бали кожен)
        public bool IsOnHemodialysis { get; set; } = false;
        public bool HasAnginaOrMI { get; set; } = false;

        // CRAB критерії (6 балів кожен)
        public bool IsUrgentOperation { get; set; } = false;
        public bool HasCompleteFunctionalDependence { get; set; } = false;

        public int CalculateScore()
        {
            int score = 0;

            // 3 бали
            if (IsOlderThan75) score += 3;
            if (HasPreviousAmputationOrRevascularization) score += 3;
            if (HasPainAndNecrosis) score += 3;
            if (HasPartialFunctionalDependence) score += 3;

            // 4 бали
            if (IsOnHemodialysis) score += 4;
            if (HasAnginaOrMI) score += 4;

            // 6 балів
            if (IsUrgentOperation) score += 6;
            if (HasCompleteFunctionalDependence) score += 6;

            return score;
        }

        public string GetRiskLevel()
        {
            var score = CalculateScore();

            if (score <= 6)
                return "Низький ризик";
            else if (score >= 7 && score <= 10)
                return "Помірний ризик";
            else if (score >= 11)
                return "Високий ризик";
            else
                return "Невизначений ризик";
        }

        public void Reset()
        {
            IsOlderThan75 = false;
            HasPreviousAmputationOrRevascularization = false;
            HasPainAndNecrosis = false;
            HasPartialFunctionalDependence = false;
            IsOnHemodialysis = false;
            HasAnginaOrMI = false;
            IsUrgentOperation = false;
            HasCompleteFunctionalDependence = false;
        }
    }
}