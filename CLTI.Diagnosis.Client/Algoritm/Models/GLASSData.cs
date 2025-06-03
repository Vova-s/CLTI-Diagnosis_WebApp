namespace CLTI.Diagnosis.Client.Algoritm.Models
{
    public class GLASSData
    {
        // Стадія I
        public bool HasGeneralOrExternalStenosis { get; set; } = false;
        public bool HasCLIWithoutBothStenosis { get; set; } = false;
        public bool HasInfrarenalStenosis { get; set; } = false;
        public bool HasCombinationFirstThreePoints { get; set; } = false;

        // Стадія II
        public bool HasCLIInAorta { get; set; } = false;
        public bool HasCLIWithExternalIliacStenosis { get; set; } = false;
        public bool HasDiffuseLesionInAortoIliacSegment { get; set; } = false;
        public bool HasExpressionDiffusionInAorto { get; set; } = false;

        // Стадія A
        public bool HasGeneralStenosisOver50Percent { get; set; } = false;

        // Стадія B
        public bool HasGeneralStenosisOver50PercentB { get; set; } = false;

        public int? CalculateStage()
        {
            // Перевіряємо Стадію A
            if (HasGeneralStenosisOver50Percent)
            {
                return 1; // Стадія A (відповідає стадії I + критерій A)
            }

            // Перевіряємо Стадію B
            if (HasGeneralStenosisOver50PercentB)
            {
                return 2; // Стадія B (відповідає стадії I + критерій B)
            }

            // Перевіряємо Стадію I
            if (HasGeneralOrExternalStenosis || HasCLIWithoutBothStenosis ||
                HasInfrarenalStenosis || HasCombinationFirstThreePoints)
            {
                return 1; // Стадія I
            }

            // Перевіряємо Стадію II
            if (HasCLIInAorta || HasCLIWithExternalIliacStenosis ||
                HasDiffuseLesionInAortoIliacSegment || HasExpressionDiffusionInAorto)
            {
                return 2; // Стадія II
            }

            return null; // Не визначено
        }

        public string GetStageDescription()
        {
            var stage = CalculateStage();

            return stage switch
            {
                1 => "Стадія I - Стеноз загальної та/або зовнішньої клубової артерії",
                2 => "Стадія II - Хронічна повна оклюзія аорти та/або зовнішньої клубової артерії",
                _ => "Стадія не визначена"
            };
        }

        public void Reset()
        {
            HasGeneralOrExternalStenosis = false;
            HasCLIWithoutBothStenosis = false;
            HasInfrarenalStenosis = false;
            HasCombinationFirstThreePoints = false;
            HasCLIInAorta = false;
            HasCLIWithExternalIliacStenosis = false;
            HasDiffuseLesionInAortoIliacSegment = false;
            HasExpressionDiffusionInAorto = false;
            HasGeneralStenosisOver50Percent = false;
            HasGeneralStenosisOver50PercentB = false;
        }
    }
}