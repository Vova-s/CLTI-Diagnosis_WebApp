namespace CLTI.Diagnosis.Client.Algoritm
{
    public class StateService
    {
        public double KpiValue { get; private set; } = 0;
        public double PpiValue { get; private set; } = 0;

        public bool ShowKpiNormal { get; private set; }
        public bool ShowKpiLow { get; private set; }
        public bool ShowKpiHigh { get; private set; }
        public bool ShowPpiField { get; private set; }
        public bool ShowPpiLow { get; private set; }
        public bool ShowPpiNormal { get; private set; }

        public bool ShowHemodynamicSection => true;
        public bool ShowPpiInSidebar { get; private set; }
        public bool ShowWifiSection { get; set; }

        public bool KpiStepCompleted { get; private set; }
        public bool PpiStepCompleted { get; private set; }
        public bool WStepCompleted => WLevelValue.HasValue;
        public bool IsWCompleted { get; set; } = false;
        public bool IsICompleted { get; set; } = false;
        public bool IsfICompleted { get; set; } = false;

        public bool HasKpiValue => KpiValue > 0;

        // WiFI W критерій
        public bool HasNecrosis { get; set; }
        public string? NecrosisType { get; set; }
        public string? GangreneSpread { get; set; }
        public string? UlcerLocation { get; set; }
        public string? UlcerAffectsBone { get; set; }
        public string? UlcerDepth { get; set; }
        public string? UlcerLocation2 { get; set; }

        // WiFI I критерій (нові властивості)
        public string? PsatValue { get; set; }
        public bool HasArterialCalcification { get; set; }
        public double? TcPO2Value { get; set; }

        public int? WLevelValue => CalculateWLevelValue();
        public int? ILevelValue => CalculateILevelValue();

        public bool CanContinue =>
            ((KpiValue < 0.9 && KpiValue > 0) || (KpiValue > 1.4 && PpiValue < 0.7))
            && WLevelValue.HasValue;

        public bool CanContinueI =>
            ILevelValue.HasValue;

        public bool NeedExit =>
            (KpiValue >= 0.9 && KpiValue <= 1.4) || (KpiValue > 1.4 && PpiValue >= 0.7);

        public event Action? OnChange;

        public void NotifyStateChanged() => OnChange?.Invoke();

        public void UpdateKpiValue(double value)
        {
            KpiValue = value;
            ShowKpiNormal = false;
            ShowKpiLow = false;
            ShowKpiHigh = false;
            ShowPpiField = false;
            ShowPpiInSidebar = false;

            if (value > 0)
            {
                if (value < 0.9)
                {
                    ShowKpiLow = true;
                    KpiStepCompleted = true;
                }
                else if (value >= 0.9 && value <= 1.4)
                {
                    ShowKpiNormal = true;
                    KpiStepCompleted = true;
                    ShowWifiSection = false;
                }
                else if (value > 1.4)
                {
                    ShowKpiHigh = true;
                    ShowPpiField = true;
                    ShowPpiInSidebar = true;
                    KpiStepCompleted = true;
                }
            }
            else
            {
                KpiStepCompleted = false;
                ShowWifiSection = false;
            }

            NotifyStateChanged();
        }

        public void UpdatePpiValue(double value)
        {
            PpiValue = value;
            ShowPpiLow = false;
            ShowPpiNormal = false;

            if (value > 0)
            {
                if (value < 0.7)
                {
                    ShowPpiLow = true;
                    PpiStepCompleted = true;
                }
                else if (value >= 0.7)
                {
                    ShowPpiNormal = true;
                    PpiStepCompleted = true;
                    ShowWifiSection = false;
                }
            }
            else
            {
                PpiStepCompleted = false;
                ShowWifiSection = false;
            }

            NotifyStateChanged();
        }

        public void Reset()
        {
            KpiValue = 0;
            PpiValue = 0;
            ShowKpiNormal = false;
            ShowKpiLow = false;
            ShowKpiHigh = false;
            ShowPpiField = false;
            ShowPpiLow = false;
            ShowPpiNormal = false;
            ShowPpiInSidebar = false;
            ShowWifiSection = false;
            KpiStepCompleted = false;
            PpiStepCompleted = false;
            IsWCompleted = false;
            IsICompleted = false;
            IsfICompleted = false;

            // Скидання W критерію
            HasNecrosis = false;
            NecrosisType = null;
            GangreneSpread = null;
            UlcerLocation = null;
            UlcerAffectsBone = null;
            UlcerDepth = null;
            UlcerLocation2 = null;

            // Скидання I критерію
            PsatValue = null;
            HasArterialCalcification = false;
            TcPO2Value = null;

            NotifyStateChanged();
        }

        private int? CalculateWLevelValue()
        {
            if (!HasNecrosis) return 0;

            if (NecrosisType == "Гангрена")
            {
                return GangreneSpread switch
                {
                    "Поширюється на плесно" => 3,
                    "Поширюється лише на пальці" => 2,
                    _ => null
                };
            }

            if (NecrosisType == "Виразка")
            {
                if (UlcerLocation == "На п'яті")
                {
                    return UlcerDepth switch
                    {
                        "До кістки" => 3,
                        "Не до кістки" => 2,
                        _ => null
                    };
                }

                if (UlcerLocation == "Не на п'яті")
                {
                    if (UlcerAffectsBone == "Не захоплює")
                    {
                        return 1;
                    }

                    if (UlcerAffectsBone == "Захоплює")
                    {
                        return UlcerLocation2 switch
                        {
                            "Плесна і передплесна" => 3,
                            "Дистальних фаланг" => 2,
                            _ => null
                        };
                    }
                }
            }

            return null;
        }

        private int? CalculateILevelValue()
        {
            // Базова логіка оцінки критерію I на основі таблиці з зображення
            if (string.IsNullOrEmpty(PsatValue)) return null;

            // Якщо є кальцифікація і вказано TcPO2
            if (HasArterialCalcification && TcPO2Value.HasValue)
            {
                return TcPO2Value.Value switch
                {
                    >= 60 => 0,
                    >= 40 and < 60 => 1,
                    >= 30 and < 40 => 2,
                    < 30 => 3,
                    _ => null
                };
            }

            // Оцінка на основі ПСАТ
            return PsatValue switch
            {
                "≥0,6" => 0,
                "40-59" => TcPO2Value.HasValue ? GetTcPO2Level(TcPO2Value.Value) : 1,
                "30-39" => TcPO2Value.HasValue ? GetTcPO2Level(TcPO2Value.Value) : 2,
                "<30" => 3,
                _ => null
            };
        }

        private int GetTcPO2Level(double tcPO2)
        {
            return tcPO2 switch
            {
                >= 60 => 0,
                >= 40 and < 60 => 1,
                >= 30 and < 40 => 2,
                < 30 => 3,
                _ => 0
            };
        }
    }
}