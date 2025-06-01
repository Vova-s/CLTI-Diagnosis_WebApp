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

        // WiFI I критерій (оновлені властивості згідно з новою логікою)
        public string? PsatValue { get; set; }
        public bool HasArterialCalcification { get; set; }
        public double? TcPO2Value { get; set; }
        public bool HasDiabetes { get; set; } = false; // Нова властивість для цукрового діабету

        // WiFI fI критерій (інфекція стопи)
        public bool HasLocalSwelling { get; set; }
        public bool HasErythema { get; set; }
        public bool HasLocalPain { get; set; }
        public bool HasLocalWarmth { get; set; }
        public bool HasPus { get; set; }

        // SIRS ознаки
        public bool HasTachycardia { get; set; } // Пульс > 90/хв (мала ознака)
        public bool HasTachypnea { get; set; } // ЧД > 20/хв (мала ознака)
        public bool HasTemperatureChange { get; set; } // Температура >38 або <36°C (велика ознака)
        public bool HasLeukocytosis { get; set; } // Лейкоцити >12 або <4 × 10⁹/л (велика ознака)

        public string? SirsAbsentType { get; set; }
        public string? HyperemiaSize { get; set; }

        public int? WLevelValue => CalculateWLevelValue();
        public int? ILevelValue => CalculateILevelValue();
        public int? FILevelValue => CalculateFILevelValue();

        // Логіка для показу полів у WiFI_I
        public bool ShouldShowPsatField => KpiValue > 1.3; // Показувати ПСАТ тільки якщо КПІ > 1,3
        public bool ShouldShowTcPO2Field => HasArterialCalcification; // Показувати TcPO2 тільки якщо є кальцифікація

        // Логіка для fI критерію
        public bool HasAnyInfectionSign => HasLocalSwelling || HasErythema || HasLocalPain || HasLocalWarmth || HasPus;
        public bool ShouldShowHyperemiaField => HasAnyInfectionSign && !HasSirs && !string.IsNullOrEmpty(SirsAbsentType);
        public bool HasSirs => CalculateSirsPresence();

        public bool CanContinue =>
            ((KpiValue < 0.9 && KpiValue > 0) || (KpiValue > 1.4 && PpiValue < 0.7))
            && WLevelValue.HasValue;

        public bool CanContinueI => ILevelValue.HasValue;
        public bool CanContinueFI => FILevelValue.HasValue;

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
            HasDiabetes = false;

            // Скидання fI критерію
            HasLocalSwelling = false;
            HasErythema = false;
            HasLocalPain = false;
            HasLocalWarmth = false;
            HasPus = false;
            HasTachycardia = false;
            HasTachypnea = false;
            HasTemperatureChange = false;
            HasLeukocytosis = false;
            SirsAbsentType = null;
            HyperemiaSize = null;

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
            // Новий алгоритм згідно з медичними рекомендаціями

            // Якщо є кальцифікація і вказано TcPO2 - використовуємо TcPO2
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

            // Якщо КПІ ≤ 1,3 - оцінюємо за КПІ
            if (KpiValue <= 1.3)
            {
                return KpiValue switch
                {
                    >= 0.9 => 0,
                    >= 0.6 and < 0.9 => 1,
                    >= 0.4 and < 0.6 => 2,
                    < 0.4 and > 0 => 3,
                    _ => null
                };
            }

            // Якщо КПІ > 1,3 - потрібен ПСАТ
            if (KpiValue > 1.3)
            {
                if (string.IsNullOrEmpty(PsatValue))
                {
                    return null; // Не можемо розрахувати без ПСАТ
                }

                // Оцінка на основі ПСАТ (основний показник при КПІ > 1,3)
                return PsatValue switch
                {
                    "≥60" => 0,
                    "40-59" => 1,
                    "30-39" => 2,
                    "<30" => 3,
                    _ => null
                };
            }

            return null;
        }

        private bool CalculateSirsPresence()
        {
            // SIRS присутній якщо є:
            // - 2 або більше малих ознак (пульс + ЧД) або
            // - 1 або більше великих ознак (температура або лейкоцити)

            int smallSigns = 0;
            int largeSigns = 0;

            if (HasTachycardia) smallSigns++;
            if (HasTachypnea) smallSigns++;
            if (HasTemperatureChange) largeSigns++;
            if (HasLeukocytosis) largeSigns++;

            return largeSigns >= 1 || smallSigns >= 2;
        }

        private int? CalculateFILevelValue()
        {
            // Якщо немає жодної ознаки інфекції - fI = 0
            if (!HasAnyInfectionSign)
            {
                return 0;
            }

            // Якщо є ознаки інфекції, перевіряємо SIRS
            if (HasSirs)
            {
                return 3; // SIRS наявний
            }

            // SIRS відсутній - залежить від ураження
            if (SirsAbsentType == "Шкіра")
            {
                // Уражені лише шкіра та підшкірна жирова клітковина
                if (string.IsNullOrEmpty(HyperemiaSize))
                {
                    return null; // Потрібно вказати розмір гіперемії
                }

                return HyperemiaSize switch
                {
                    "0.5-2" => 1, // Гіперемія 0,5-2 см
                    ">2" => 2,    // Гіперемія >2 см
                    _ => null
                };
            }
            else if (SirsAbsentType == "Кістки")
            {
                // Уражені кістки, суглоби або сухожилки
                return 2;
            }

            return null; // Ще не всі дані введені
        }
    }
}