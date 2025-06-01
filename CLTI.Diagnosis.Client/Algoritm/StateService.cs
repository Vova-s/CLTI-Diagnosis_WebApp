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

        public bool HasNecrosis { get; set; }
        public string? NecrosisType { get; set; }
        public string? GangreneSpread { get; set; }
        public string? UlcerLocation { get; set; }
        public string? UlcerAffectsBone { get; set; }
        public string? UlcerDepth { get; set; }
        public string? UlcerLocation2 { get; set; }

        public string? PsatValue { get; set; }
        public bool HasArterialCalcification { get; set; }
        public double? TcPO2Value { get; set; }
        public bool HasDiabetes { get; set; } = false;

        public bool HasLocalSwelling { get; set; }
        public bool HasErythema { get; set; }
        public bool HasLocalPain { get; set; }
        public bool HasLocalWarmth { get; set; }
        public bool HasPus { get; set; }

        public bool HasTachycardia { get; set; }
        public bool HasTachypnea { get; set; }
        public bool HasTemperatureChange { get; set; }
        public bool HasLeukocytosis { get; set; }

        public string? SirsAbsentType { get; set; }
        public string? HyperemiaSize { get; set; }

        public int? WLevelValue => CalculateWLevelValue();
        public int? ILevelValue => CalculateILevelValue();
        public int? FILevelValue => CalculateFILevelValue();

        public bool ShouldShowPsatField => KpiValue > 1.3;
        public bool ShouldShowTcPO2Field => HasArterialCalcification;

        public bool HasSirs => CalculateSirsPresence();

        public bool ShouldShowHyperemiaField => HasTwoOrMoreInfectionSigns && !HasSirs && !string.IsNullOrEmpty(SirsAbsentType) && SirsAbsentType == "Шкіра";

        public bool ShouldShowSirsAbsentSection =>
       HasTwoOrMoreInfectionSigns &&
       !HasSirs; 
        public bool HasTwoOrMoreInfectionSigns => new[] { HasLocalSwelling, HasErythema, HasLocalPain, HasLocalWarmth, HasPus }.Count(b => b) >= 2;
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

            HasNecrosis = false;
            NecrosisType = null;
            GangreneSpread = null;
            UlcerLocation = null;
            UlcerAffectsBone = null;
            UlcerDepth = null;
            UlcerLocation2 = null;

            PsatValue = null;
            HasArterialCalcification = false;
            TcPO2Value = null;
            HasDiabetes = false;

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

            if (KpiValue > 1.3)
            {
                if (string.IsNullOrEmpty(PsatValue))
                {
                    return null;
                }

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
            int small = GetSmallSirsSignsCount();
            int large = GetLargeSirsSignsCount();

            // SIRS є лише якщо ≥2 загальні ознаки і серед них ≥1 велика
            return (small + large >= 2) && (large >= 1);
        }



        private int GetSmallSirsSignsCount()
        {
            int count = 0;
            if (HasTachycardia) count++;
            if (HasTachypnea) count++;
                return count;
        }

        private int GetLargeSirsSignsCount()
        {
            int count = 0;
            if (HasTemperatureChange) count++;
            if (HasLeukocytosis) count++;
            return count;
        }

        private int? CalculateFILevelValue()
        {
            int infectionSignsCount = 0;
            if (HasLocalSwelling) infectionSignsCount++;
            if (HasErythema) infectionSignsCount++;
            if (HasLocalPain) infectionSignsCount++;
            if (HasLocalWarmth) infectionSignsCount++;
            if (HasPus) infectionSignsCount++;

            if (infectionSignsCount <= 1)
            {
                return 0;
            }

            int smallSigns = GetSmallSirsSignsCount();
            int largeSigns = GetLargeSirsSignsCount();

            // SIRS наявний: ≥2 ознаки, з них ≥1 велика
            if ((smallSigns + largeSigns) >= 2 && largeSigns >= 1)
            {
                return 3;
            }

            // SIRS відсутній: ≤1 ознака (мала/велика)
            if ((smallSigns + largeSigns) <= 1)
            {
                if (SirsAbsentType == "Шкіра")
                {
                    if (string.IsNullOrEmpty(HyperemiaSize)) return null;

                    return HyperemiaSize switch
                    {
                        "0.5-2" => 1,
                        ">2" => 2,
                        _ => null
                    };
                }
                else if (SirsAbsentType == "Кістки")
                {
                    return 2;
                }

                return null;
            }

            // Якщо тільки 2 малі ознаки, без великих — також SIRS відсутній
            if (smallSigns == 2 && largeSigns == 0)
            {
                if (SirsAbsentType == "Шкіра")
                {
                    if (string.IsNullOrEmpty(HyperemiaSize)) return null;

                    return HyperemiaSize switch
                    {
                        "0.5-2" => 1,
                        ">2" => 2,
                        _ => null
                    };
                }
                else if (SirsAbsentType == "Кістки")
                {
                    return 2;
                }

                return null;
            }

            return null;
        }

    }
}
