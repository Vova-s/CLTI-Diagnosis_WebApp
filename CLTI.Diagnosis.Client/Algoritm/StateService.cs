namespace CLTI.Diagnosis.Client.Algoritm
{
    public class StateService
    {
        public double KpiValue { get; private set; } = 0;
        public double PpiValue { get; private set; } = 0;

        // Відображення різних станів КПІ/ППІ
        public bool ShowKpiNormal { get; private set; }
        public bool ShowKpiLow { get; private set; }
        public bool ShowKpiHigh { get; private set; }
        public bool ShowPpiField { get; private set; }
        public bool ShowPpiLow { get; private set; }
        public bool ShowPpiNormal { get; private set; }

        // Стани для бокової панелі
        public bool ShowHemodynamicSection => true; // Завжди показувати першу секцію
        public bool ShowPpiInSidebar { get; private set; }
        public bool ShowWifiSection { get; private set; }

        // Стани завершення кроків (для галочок)
        public bool KpiStepCompleted { get; private set; }
        public bool PpiStepCompleted { get; private set; }

        public bool HasKpiValue => KpiValue > 0;

        // --- WIFI: W ---
        public bool HasNecrosis { get; set; }
        public string? NecrosisType { get; set; } // "Гангрена" / "Виразка"
        public string? GangreneSpread { get; set; } // "Поширюється на плесно", "Поширюється лише на пальці"
        public string? UlcerLocation { get; set; } // "На п'яті", "Не на п'яті"

        public int WNecrosisCode => HasNecrosis ? 1 : 0;

        public int? NecrosisTypeCode => NecrosisType switch
        {
            "Гангрена" => 1,
            "Виразка" => 2,
            _ => null
        };

        public int? GangreneSpreadCode => GangreneSpread switch
        {
            "Поширюється на плесно" => 1,
            "Поширюється лише на пальці" => 2,
            _ => null
        };

        public int? UlcerLocationCode => UlcerLocation switch
        {
            "На п'яті" => 1,
            "Не на п'яті" => 2,
            _ => null
        };

        public int? WLevelValue => CalculateWLevelValue();

        public event Action? OnChange;

        public void UpdateKpiValue(double value)
        {
            KpiValue = value;

            // Скидання всіх станів
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
                    ShowWifiSection = true; // Можемо переходити до WiFI
                }
                else if (value >= 0.9 && value <= 1.4)
                {
                    ShowKpiNormal = true;
                    KpiStepCompleted = true;
                    ShowWifiSection = false; // В нормі - також можемо переходити
                }
                else if (value > 1.4)
                {
                    ShowKpiHigh = true;
                    ShowPpiField = true;
                    ShowPpiInSidebar = true; // Показати ППІ в боковій панелі
                    KpiStepCompleted = true;
                    // WiFI секція буде показана після введення ППІ
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
                    ShowWifiSection = true; // Можемо переходити до WiFI
                }
                else if (value >= 0.7)
                {
                    ShowPpiNormal = true;
                    PpiStepCompleted = true;
                    ShowWifiSection = false; // В нормі - також можемо переходити
                }
            }
            else
            {
                PpiStepCompleted = false;
                ShowWifiSection = false;
            }

            NotifyStateChanged();
        }

        public bool CanContinue =>
            (KpiValue < 0.9 && KpiValue > 0) ||
            (KpiValue > 1.4 && PpiValue < 0.7);
        public bool NeedExit =>
        (KpiValue >= 0.9 && KpiValue <= 1.4) || (KpiValue > 1.4 && PpiValue >= 0.7);
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

            HasNecrosis = false;
            NecrosisType = null;
            GangreneSpread = null;
            UlcerLocation = null;

            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

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
                return UlcerLocation switch
                {
                    "На п'яті" => 2,
                    "Не на п'яті" => 1,
                    _ => null
                };
            }

            return null;
        }
    }
}