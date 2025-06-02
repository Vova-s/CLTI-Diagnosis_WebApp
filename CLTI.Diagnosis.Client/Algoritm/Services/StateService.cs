using System;
using CLTI.Diagnosis.Client.Algoritm.Models;
using CLTI.Diagnosis.Client.Algoritm.Steps;

namespace CLTI.Diagnosis.Client.Algoritm.Services
{
    public class StateService
    {
        // Моделі даних
        private readonly VascularData _vascularData;
        private readonly WoundData _woundData;
        private readonly InfectionData _infectionData;
        private readonly CRABData _crabData;
        private readonly UIState _uiState;

        // Калькулятори
        private readonly WLevelCalculator _wCalculator;
        private readonly ILevelCalculator _iCalculator;
        private readonly FILevelCalculator _fiCalculator;
        private readonly CRABCalculator _crabCalculator;
        private readonly ClinicalStageCalculator _stageCalculator;

        // Стан кроків
        public bool KpiStepCompleted { get; private set; }
        public bool PpiStepCompleted { get; private set; }
        public bool IsWCompleted { get; set; } = false;
        public bool IsICompleted { get; set; } = false;
        public bool IsfICompleted { get; set; } = false;
        public bool IsWiFIResultsCompleted { get; set; } = false;
        public bool IsCRABCompleted { get; set; } = false;

        public StateService()
        {
            // Ініціалізація моделей
            _vascularData = new VascularData();
            _woundData = new WoundData();
            _infectionData = new InfectionData();
            _crabData = new CRABData();
            _uiState = new UIState();

            // Ініціалізація калькуляторів
            _wCalculator = new WLevelCalculator(_woundData);
            _iCalculator = new ILevelCalculator(_vascularData);
            _fiCalculator = new FILevelCalculator(_infectionData);
            _crabCalculator = new CRABCalculator(_crabData);
            _stageCalculator = new ClinicalStageCalculator(_wCalculator, _iCalculator, _fiCalculator);
        }

        // ОРИГІНАЛЬНИЙ API - БЕЗ ЗМІН!
        public double KpiValue => _vascularData.KpiValue;
        public double PpiValue => _vascularData.PpiValue;

        public bool ShowKpiNormal => _uiState.ShowKpiNormal;
        public bool ShowKpiLow => _uiState.ShowKpiLow;
        public bool ShowKpiHigh => _uiState.ShowKpiHigh;
        public bool ShowPpiField => _uiState.ShowPpiField;
        public bool ShowPpiLow => _uiState.ShowPpiLow;
        public bool ShowPpiNormal => _uiState.ShowPpiNormal;
        public bool ShowHemodynamicSection => _uiState.ShowHemodynamicSection;
        public bool ShowPpiInSidebar => _uiState.ShowPpiInSidebar;
        public bool ShowWifiSection
        {
            get => _uiState.ShowWifiSection;
            set => _uiState.ShowWifiSection = value;
        }

        public bool WStepCompleted => _wCalculator.Calculate().HasValue;
        public bool HasKpiValue => _vascularData.HasKpiValue;

        // Властивості ран
        public bool HasNecrosis
        {
            get => _woundData.HasNecrosis;
            set => _woundData.HasNecrosis = value;
        }
        public string? NecrosisType
        {
            get => _woundData.NecrosisType;
            set => _woundData.NecrosisType = value;
        }
        public string? GangreneSpread
        {
            get => _woundData.GangreneSpread;
            set => _woundData.GangreneSpread = value;
        }
        public string? UlcerLocation
        {
            get => _woundData.UlcerLocation;
            set => _woundData.UlcerLocation = value;
        }
        public string? UlcerAffectsBone
        {
            get => _woundData.UlcerAffectsBone;
            set => _woundData.UlcerAffectsBone = value;
        }
        public string? UlcerDepth
        {
            get => _woundData.UlcerDepth;
            set => _woundData.UlcerDepth = value;
        }
        public string? UlcerLocation2
        {
            get => _woundData.UlcerLocation2;
            set => _woundData.UlcerLocation2 = value;
        }

        // Судинні дані
        public string? PsatValue
        {
            get => _vascularData.PsatValue;
            set => _vascularData.PsatValue = value;
        }
        public bool HasArterialCalcification
        {
            get => _vascularData.HasArterialCalcification;
            set => _vascularData.HasArterialCalcification = value;
        }
        public double? TcPO2Value
        {
            get => _vascularData.TcPO2Value;
            set => _vascularData.TcPO2Value = value;
        }
        public bool HasDiabetes
        {
            get => _vascularData.HasDiabetes;
            set => _vascularData.HasDiabetes = value;
        }

        // Інфекційні дані
        public bool HasLocalSwelling
        {
            get => _infectionData.HasLocalSwelling;
            set => _infectionData.HasLocalSwelling = value;
        }
        public bool HasErythema
        {
            get => _infectionData.HasErythema;
            set => _infectionData.HasErythema = value;
        }
        public bool HasLocalPain
        {
            get => _infectionData.HasLocalPain;
            set => _infectionData.HasLocalPain = value;
        }
        public bool HasLocalWarmth
        {
            get => _infectionData.HasLocalWarmth;
            set => _infectionData.HasLocalWarmth = value;
        }
        public bool HasPus
        {
            get => _infectionData.HasPus;
            set => _infectionData.HasPus = value;
        }
        public bool HasTachycardia
        {
            get => _infectionData.HasTachycardia;
            set => _infectionData.HasTachycardia = value;
        }
        public bool HasTachypnea
        {
            get => _infectionData.HasTachypnea;
            set => _infectionData.HasTachypnea = value;
        }
        public bool HasTemperatureChange
        {
            get => _infectionData.HasTemperatureChange;
            set => _infectionData.HasTemperatureChange = value;
        }
        public bool HasLeukocytosis
        {
            get => _infectionData.HasLeukocytosis;
            set => _infectionData.HasLeukocytosis = value;
        }
        public string? SirsAbsentType
        {
            get => _infectionData.SirsAbsentType;
            set => _infectionData.SirsAbsentType = value;
        }
        public string? HyperemiaSize
        {
            get => _infectionData.HyperemiaSize;
            set => _infectionData.HyperemiaSize = value;
        }

        // CRAB дані - через модель
        public bool IsOlderThan75
        {
            get => _crabData.IsOlderThan75;
            set => _crabData.IsOlderThan75 = value;
        }
        public bool HasPreviousAmputationOrRevascularization
        {
            get => _crabData.HasPreviousAmputationOrRevascularization;
            set => _crabData.HasPreviousAmputationOrRevascularization = value;
        }
        public bool HasPainAndNecrosis
        {
            get => _crabData.HasPainAndNecrosis;
            set => _crabData.HasPainAndNecrosis = value;
        }
        public bool HasPartialFunctionalDependence
        {
            get => _crabData.HasPartialFunctionalDependence;
            set => _crabData.HasPartialFunctionalDependence = value;
        }
        public bool IsOnHemodialysis
        {
            get => _crabData.IsOnHemodialysis;
            set => _crabData.IsOnHemodialysis = value;
        }
        public bool HasAnginaOrMI
        {
            get => _crabData.HasAnginaOrMI;
            set => _crabData.HasAnginaOrMI = value;
        }
        public bool IsUrgentOperation
        {
            get => _crabData.IsUrgentOperation;
            set => _crabData.IsUrgentOperation = value;
        }
        public bool HasCompleteFunctionalDependence
        {
            get => _crabData.HasCompleteFunctionalDependence;
            set => _crabData.HasCompleteFunctionalDependence = value;
        }

        // Результати
        public bool CannotSaveLimb
        {
            get => _stageCalculator.CannotSaveLimb;
            set => _stageCalculator.CannotSaveLimb = value;
        }

        public int? WLevelValue => _wCalculator.Calculate();
        public int? ILevelValue => _iCalculator.Calculate();
        public int? FILevelValue => _fiCalculator.Calculate();

        public int ClinicalStage => _stageCalculator.CalculateClinicalStage();
        public string AmputationRisk => _stageCalculator.CalculateAmputationRisk();
        public string RevascularizationBenefit => _stageCalculator.CalculateRevascularizationBenefit();

        // CRAB результати - через калькулятор
        public int? CRABTotalScore => _crabCalculator.CalculateScore();
        public string CRABRiskLevel => _crabCalculator.GetRiskLevel();
        public string CRABRiskDescription => _crabCalculator.GetRiskDescription();
        public double CRABMortalityRiskPercentage => _crabCalculator.GetMortalityRiskPercentage();

        public bool ShouldShowPsatField => _vascularData.ShouldShowPsatField;
        public bool ShouldShowTcPO2Field => _vascularData.ShouldShowTcPO2Field;
        public bool HasSirs => _fiCalculator.HasSirs();
        public bool ShouldShowHyperemiaField => _fiCalculator.ShouldShowHyperemiaField();
        public bool ShouldShowSirsAbsentSection => _fiCalculator.ShouldShowSirsAbsentSection();
        public bool HasTwoOrMoreInfectionSigns => _infectionData.HasTwoOrMoreInfectionSigns;

        public bool CanContinue =>
            ((KpiValue < 0.9 && KpiValue > 0) || (KpiValue > 1.4 && PpiValue < 0.7))
            && WLevelValue.HasValue;

        public bool CanContinueI => ILevelValue.HasValue;
        public bool CanContinueFI => FILevelValue.HasValue;

        public bool NeedExit =>
            (KpiValue >= 0.9 && KpiValue <= 1.4) || (KpiValue > 1.4 && PpiValue >= 0.7);

        public event Action? OnChange;

        public void NotifyStateChanged() => OnChange?.Invoke();

        // ОРИГІНАЛЬНІ МЕТОДИ - БЕЗ ЗМІН!
        public void UpdateKpiValue(double value)
        {
            _vascularData.KpiValue = value;
            _uiState.ShowKpiNormal = false;
            _uiState.ShowKpiLow = false;
            _uiState.ShowKpiHigh = false;
            _uiState.ShowPpiField = false;
            _uiState.ShowPpiInSidebar = false;

            if (value > 0)
            {
                if (value < 0.9)
                {
                    _uiState.ShowKpiLow = true;
                    KpiStepCompleted = true;
                }
                else if (value >= 0.9 && value <= 1.4)
                {
                    _uiState.ShowKpiNormal = true;
                    KpiStepCompleted = true;
                    _uiState.ShowWifiSection = false;
                }
                else if (value > 1.4)
                {
                    _uiState.ShowKpiHigh = true;
                    _uiState.ShowPpiField = true;
                    _uiState.ShowPpiInSidebar = true;
                    KpiStepCompleted = true;
                }
            }
            else
            {
                KpiStepCompleted = false;
                _uiState.ShowWifiSection = false;
            }

            NotifyStateChanged();
        }

        public void UpdatePpiValue(double value)
        {
            _vascularData.PpiValue = value;
            _uiState.ShowPpiLow = false;
            _uiState.ShowPpiNormal = false;

            if (value > 0)
            {
                if (value < 0.7)
                {
                    _uiState.ShowPpiLow = true;
                    PpiStepCompleted = true;
                }
                else if (value >= 0.7)
                {
                    _uiState.ShowPpiNormal = true;
                    PpiStepCompleted = true;
                    _uiState.ShowWifiSection = false;
                }
            }
            else
            {
                PpiStepCompleted = false;
                _uiState.ShowWifiSection = false;
            }

            NotifyStateChanged();
        }

        public void Reset()
        {
            _vascularData.Reset();
            _woundData.Reset();
            _infectionData.Reset();
            _crabData.Reset(); // Використовуємо метод Reset() з CRABData
            _uiState.Reset();

            KpiStepCompleted = false;
            PpiStepCompleted = false;
            IsWCompleted = false;
            IsICompleted = false;
            IsfICompleted = false;
            IsWiFIResultsCompleted = false;
            IsCRABCompleted = false;
            _stageCalculator.CannotSaveLimb = false;

            NotifyStateChanged();
        }

        // Методи для доступу до компонентів (якщо потрібно для нових функцій)
        public VascularData GetVascularData() => _vascularData;
        public WoundData GetWoundData() => _woundData;
        public InfectionData GetInfectionData() => _infectionData;
        public CRABData GetCRABData() => _crabData;
        public UIState GetUIState() => _uiState;
    }
}