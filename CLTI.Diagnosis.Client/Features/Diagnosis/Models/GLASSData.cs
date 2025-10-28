namespace CLTI.Diagnosis.Client.Features.Diagnosis.Models
{
    public class GLASSData
    {
        // === Стадія I ===
        public bool HasGeneralOrExternalStenosis { get; set; } = false;
        public bool HasCLIWithoutBothStenosis { get; set; } = false;
        public bool HasInfrarenalStenosis { get; set; } = false;
        public bool HasCombinationFirstThreePoints { get; set; } = false;

        // === Стадія II ===
        public bool HasCLIInAorta { get; set; } = false;
        public bool HasCLIWithExternalIliacStenosis { get; set; } = false;
        public bool HasDiffuseLesionInAortoIliacSegment { get; set; } = false;
        public bool HasExpressionDiffusionInAorto { get; set; } = false;

        // === Критерії A та B ===
        public bool HasGeneralStenosisOver50Percent { get; set; } = false;      // Стадія A
        public bool HasGeneralStenosisOver50PercentB { get; set; } = false;     // Стадія B

        // === Обчислення стадії ===
        public int? CalculateStage()
        {
            if (HasGeneralStenosisOver50Percent)
                return 1; // Стадія A (відповідає Стадії I + критерій A)

            if (HasGeneralStenosisOver50PercentB)
                return 2; // Стадія B (відповідає Стадії I + критерій B)

            if (HasGeneralOrExternalStenosis || HasCLIWithoutBothStenosis ||
                HasInfrarenalStenosis || HasCombinationFirstThreePoints)
                return 1; // Стадія I

            if (HasCLIInAorta || HasCLIWithExternalIliacStenosis ||
                HasDiffuseLesionInAortoIliacSegment || HasExpressionDiffusionInAorto)
                return 2; // Стадія II

            return null; // Не визначено
        }

        // === Опис стадії ===
        public string GetStageDescription()
        {
            return CalculateStage() switch
            {
                1 => "Стадія I - Стеноз загальної та/або зовнішньої клубової артерії",
                2 => "Стадія II - Хронічна повна оклюзія аорти та/або зовнішньої клубової артерії",
                _ => "Стадія не визначена"
            };
        }

        // === Скидання всіх параметрів ===
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
