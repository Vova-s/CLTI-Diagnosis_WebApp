using System;

namespace CLTI.Diagnosis.Client.Algoritm.Models
{
    public class VascularData
    {
        public double KpiValue { get; set; } = 0;
        public double PpiValue { get; set; } = 0;
        public string? PsatValue { get; set; }
        public double? TcPO2Value { get; set; }
        public bool HasArterialCalcification { get; set; }
        public bool HasDiabetes { get; set; } = false;

        public bool HasKpiValue => KpiValue > 0;
        public bool ShouldShowPsatField => KpiValue > 1.3;
        public bool ShouldShowTcPO2Field => HasArterialCalcification;

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