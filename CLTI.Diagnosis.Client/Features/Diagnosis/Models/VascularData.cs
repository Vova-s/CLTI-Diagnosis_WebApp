using System;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Models
{
    public class VascularData
    {
        // === Гемодинамічні параметри ===
        public double KpiValue { get; set; } = 0;
        public double PpiValue { get; set; } = 0;
        public string? PsatValue { get; set; }
        public double? TcPO2Value { get; set; }

        // === Медичні фактори ===
        public bool HasArterialCalcification { get; set; }
        public bool HasDiabetes { get; set; } = false;

        // === Логічні властивості ===
        public bool HasKpiValue => KpiValue > 0;
        public bool ShouldShowPsatField => KpiValue > 1.3;
        public bool ShouldShowTcPO2Field => HasArterialCalcification;

        // === Скидання значень ===
        public void Reset()
        {
            KpiValue = 0;
            PpiValue = 0;
            PsatValue = null;
            TcPO2Value = null;
            HasArterialCalcification = false;
            HasDiabetes = false;
        }
    }
}
