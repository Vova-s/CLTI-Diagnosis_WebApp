namespace CLTI.Diagnosis.Client.Algoritm.Steps
{
    public class ClinicalStageCalculator
    {
        private readonly WLevelCalculator _wCalculator;
        private readonly ILevelCalculator _iCalculator;
        private readonly FILevelCalculator _fiCalculator;

        public bool CannotSaveLimb { get; set; } = false;

        public ClinicalStageCalculator(
            WLevelCalculator wCalculator,
            ILevelCalculator iCalculator,
            FILevelCalculator fiCalculator)
        {
            _wCalculator = wCalculator;
            _iCalculator = iCalculator;
            _fiCalculator = fiCalculator;
        }

        public int CalculateClinicalStage()
        {
            if (CannotSaveLimb) return 5;

            var w = _wCalculator.Calculate() ?? 0;
            var i = _iCalculator.Calculate() ?? 0;
            var fi = _fiCalculator.Calculate() ?? 0;

            // Логіка з таблиці
            if (w == 0 && (i == 0 || i == 1) && (fi == 0 || fi == 1)) return 1;
            if (w == 0 && i == 2 && (fi == 0 || fi == 1 || fi == 2)) return 2;
            if (w == 0 && i == 3 && (fi == 0 || fi == 1 || fi == 2)) return 3;

            if (w == 1 && i == 0 && (fi == 0 || fi == 1)) return 1;
            if (w == 1 && i == 1 && (fi == 0 || fi == 1)) return 2;
            if (w == 1 && i == 2 && (fi == 0 || fi == 1)) return 3;
            if (w == 1 && i == 3 && (fi == 0 || fi == 1 || fi == 2)) return 4;

            if (w == 2 && i == 0 && fi == 0) return 2;
            if (w == 2 && i == 0 && fi == 1) return 3;
            if (w == 2 && i == 1 && (fi == 0 || fi == 1)) return 3;
            if (w == 2 && i == 2 && (fi == 0 || fi == 1 || fi == 2)) return 4;
            if (w == 2 && i == 3 && (fi == 0 || fi == 1 || fi == 2 || fi == 3)) return 4;

            if (w == 3 && (fi == 0 || fi == 1 || fi == 2 || fi == 3)) return 4;

            return 4;
        }

        public string CalculateAmputationRisk()
        {
            if (CannotSaveLimb) return "Надзвичайно високий";

            return CalculateClinicalStage() switch
            {
                1 => "Дуже низький",
                2 => "Низький",
                3 => "Помірний",
                4 => "Високий",
                5 => "Надзвичайно високий",
                _ => "Невизначений"
            };
        }

        public string CalculateRevascularizationBenefit()
        {
            if (CannotSaveLimb) return "Не показана";

            var i = _iCalculator.Calculate() ?? 0;

            return i switch
            {
                0 => "Дуже низька",
                1 => "Низька",
                2 => "Середня",
                3 => "Висока",
                _ => "Невизначена"
            };
        }
    }
}