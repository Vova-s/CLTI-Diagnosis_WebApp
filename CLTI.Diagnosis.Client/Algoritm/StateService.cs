namespace CLTI.Diagnosis.Client.Algoritm
{
    public class StateService
    {
        public double KpiValue { get; private set; } = 0;
        public double PpiValue { get; private set; } = 0;

        public bool IsPpiVisible => ShowPpiField;
        public bool ShowPpiField { get; private set; }

        public bool ShowKpiNormal { get; private set; }
        public bool ShowKpiLow { get; private set; }

        public bool ShowPpiLow { get; private set; }
        public bool ShowPpiNormal { get; private set; }

        public bool HasKpiValue => KpiValue > 0;

        // --- WIFI: W ---
        public bool HasNecrosis { get; set; }
        public string? NecrosisType { get; set; } // "Гангрена" / "Виразка"
        public string? GangreneSpread { get; set; } // "Поширюється на плесно", "Поширюється лише на пальці"
        public string? UlcerLocation { get; set; } // "На п’яті", "Не на п’яті"

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
            "На п’яті" => 1,
            "Не на п’яті" => 2,
            _ => null
        };

        public int? WLevelValue => CalculateWLevelValue();

        public event Action? OnChange;

        public void UpdateKpiValue(double value)
        {
            KpiValue = value;

            ShowKpiNormal = value >= 0.9 && value <= 1.4;
            ShowKpiLow = value < 0.9;
            ShowPpiField = value > 1.4;

            NotifyStateChanged();
        }

        public void UpdatePpiValue(double value)
        {
            PpiValue = value;

            ShowPpiLow = value < 0.7 && value > 0;
            ShowPpiNormal = value >= 0.7;

            NotifyStateChanged();
        }

        public bool CanContinue =>
            KpiValue < 0.9 ||
            (KpiValue > 1.4 && PpiValue < 0.7 && PpiValue > 0);

        public void Reset()
        {
            KpiValue = 0;
            PpiValue = 0;
            ShowKpiNormal = false;
            ShowKpiLow = false;
            ShowPpiField = false;
            ShowPpiLow = false;
            ShowPpiNormal = false;

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
                    "На п’яті" => 2,
                    "Не на п’яті" => 1,
                    _ => null
                };
            }

            return null;
        }
    }
}