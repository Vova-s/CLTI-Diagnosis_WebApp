using System;
using CLTI.Diagnosis.Client.Algoritm.Models;
using CLTI.Diagnosis.Client.Algoritm.Steps;

namespace CLTI.Diagnosis.Client.Algoritm.Services
{
    public class StateService
    {
        #region Private Fields - Моделі даних
        private readonly VascularData _vascularData;
        private readonly WoundData _woundData;
        private readonly InfectionData _infectionData;
        private readonly CRABData _crabData;
        private readonly YLEData _yleData;
        private readonly GLASSData _glassData;
        private readonly UIState _uiState;
        #endregion

        #region Private Fields - Калькулятори
        private readonly WLevelCalculator _wCalculator;
        private readonly ILevelCalculator _iCalculator;
        private readonly FILevelCalculator _fiCalculator;
        private readonly CRABCalculator _crabCalculator;
        private readonly YLECalculator _yleCalculator;
        private readonly GLASSCalculator _glassCalculator;
        private readonly ClinicalStageCalculator _stageCalculator;
        #endregion

        #region Constructor
        public StateService()
        {
            // Ініціалізація моделей
            _vascularData = new VascularData();
            _woundData = new WoundData();
            _infectionData = new InfectionData();
            _crabData = new CRABData();
            _yleData = new YLEData();
            _glassData = new GLASSData();
            _uiState = new UIState();

            // Ініціалізація калькуляторів
            _wCalculator = new WLevelCalculator(_woundData);
            _iCalculator = new ILevelCalculator(_vascularData);
            _fiCalculator = new FILevelCalculator(_infectionData);
            _crabCalculator = new CRABCalculator(_crabData);
            _yleCalculator = new YLECalculator(_yleData);
            _glassCalculator = new GLASSCalculator(_glassData);
            _stageCalculator = new ClinicalStageCalculator(_wCalculator, _iCalculator, _fiCalculator);
        }
        #endregion

        #region Events
        public event Action? OnChange;
        public void NotifyStateChanged() => OnChange?.Invoke();
        #endregion

        #region Step Completion States
        public bool KpiStepCompleted { get; private set; }
        public bool PpiStepCompleted { get; private set; }
        public bool IsWCompleted { get; set; } = false;
        public bool IsICompleted { get; set; } = false;
        public bool IsfICompleted { get; set; } = false;
        public bool IsWiFIResultsCompleted { get; set; } = false;
        public bool IsCRABCompleted { get; set; } = false;
        public bool Is2YLECompleted { get; set; } = false;
        public bool IsSurgicalRiskCompleted { get; set; } = false;
        public bool IsGLASSCompleted { get; set; } = false;
        public bool IsGLASSFemoroPoplitealCompleted { get; set; } = false;
        public bool IsGLASSInfrapoplitealCompleted { get; set; } = false;
        public bool IsGLASSFinalCompleted { get; set; } = false;
        public bool IsSubmalleolarDiseaseCompleted { get; set; } = false;
        public bool IsRevascularizationAssessmentCompleted { get; set; } = false;
        public bool IsRevascularizationMethodCompleted { get; set; } = false;

        /// <summary>
        /// Identifier of the saved CLTI case in the database.
        /// </summary>
        public int? CaseId { get; set; }
        #endregion

        #region Basic Properties - KPI/PPI
        public double KpiValue => _vascularData.KpiValue;
        public double PpiValue => _vascularData.PpiValue;
        public bool HasKpiValue => _vascularData.HasKpiValue;
        #endregion

        #region UI State Properties
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
        #endregion

        #region Wound Data Properties
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
        #endregion

        #region Vascular Data Properties
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
        #endregion

        #region Infection Data Properties
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
        public bool HasTwoOrMoreInfectionSigns => _infectionData.HasTwoOrMoreInfectionSigns;
        #endregion

        #region CRAB Data Properties
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
        #endregion

        #region 2YLE Data Properties
        public bool IsNonAmbulatory
        {
            get => _yleData.IsNonAmbulatory;
            set => _yleData.IsNonAmbulatory = value;
        }
        public bool HasRutherford5
        {
            get => _yleData.HasRutherford5;
            set => _yleData.HasRutherford5 = value;
        }
        public bool HasRutherford6
        {
            get => _yleData.HasRutherford6;
            set => _yleData.HasRutherford6 = value;
        }
        public bool HasCerebrovascularDisease
        {
            get => _yleData.HasCerebrovascularDisease;
            set => _yleData.HasCerebrovascularDisease = value;
        }
        public bool Has2YLEHemodialysis
        {
            get => _yleData.Has2YLEHemodialysis;
            set => _yleData.Has2YLEHemodialysis = value;
        }
        public bool HasBMI18to19
        {
            get => _yleData.HasBMI18to19;
            set => _yleData.HasBMI18to19 = value;
        }
        public bool HasBMILessThan18
        {
            get => _yleData.HasBMILessThan18;
            set => _yleData.HasBMILessThan18 = value;
        }
        public bool IsAge65to79
        {
            get => _yleData.IsAge65to79;
            set => _yleData.IsAge65to79 = value;
        }
        public bool IsAge80Plus
        {
            get => _yleData.IsAge80Plus;
            set => _yleData.IsAge80Plus = value;
        }
        public bool HasEjectionFraction40to49
        {
            get => _yleData.HasEjectionFraction40to49;
            set => _yleData.HasEjectionFraction40to49 = value;
        }
        public bool HasEjectionFractionLessThan40
        {
            get => _yleData.HasEjectionFractionLessThan40;
            set => _yleData.HasEjectionFractionLessThan40 = value;
        }
        #endregion

        #region GLASS Data Properties
        public string? GLASSSelectedStage { get; set; }
        public string? GLASSSubStage { get; set; }
        public string? GLASSFemoroPoplitealStage { get; set; }
        public string? GLASSInfrapoplitealStage { get; set; }
        public string? GLASSFinalStage { get; set; }
        public string? SubmalleolarDescriptor { get; set; }

        public bool HasGeneralOrExternalStenosis
        {
            get => _glassData.HasGeneralOrExternalStenosis;
            set => _glassData.HasGeneralOrExternalStenosis = value;
        }
        public bool HasCLIWithoutBothStenosis
        {
            get => _glassData.HasCLIWithoutBothStenosis;
            set => _glassData.HasCLIWithoutBothStenosis = value;
        }
        public bool HasInfrarenalStenosis
        {
            get => _glassData.HasInfrarenalStenosis;
            set => _glassData.HasInfrarenalStenosis = value;
        }
        public bool HasCombinationFirstThreePoints
        {
            get => _glassData.HasCombinationFirstThreePoints;
            set => _glassData.HasCombinationFirstThreePoints = value;
        }
        public bool HasCLIInAorta
        {
            get => _glassData.HasCLIInAorta;
            set => _glassData.HasCLIInAorta = value;
        }
        public bool HasCLIWithExternalIliacStenosis
        {
            get => _glassData.HasCLIWithExternalIliacStenosis;
            set => _glassData.HasCLIWithExternalIliacStenosis = value;
        }
        public bool HasDiffuseLesionInAortoIliacSegment
        {
            get => _glassData.HasDiffuseLesionInAortoIliacSegment;
            set => _glassData.HasDiffuseLesionInAortoIliacSegment = value;
        }
        public bool HasExpressionDiffusionInAorto
        {
            get => _glassData.HasExpressionDiffusionInAorto;
            set => _glassData.HasExpressionDiffusionInAorto = value;
        }
        public bool HasGeneralStenosisOver50Percent
        {
            get => _glassData.HasGeneralStenosisOver50Percent;
            set => _glassData.HasGeneralStenosisOver50Percent = value;
        }
        public bool HasGeneralStenosisOver50PercentB
        {
            get => _glassData.HasGeneralStenosisOver50PercentB;
            set => _glassData.HasGeneralStenosisOver50PercentB = value;
        }
        #endregion

        #region WiFI Results
        public int? WLevelValue => _wCalculator.Calculate();
        public int? ILevelValue => _iCalculator.Calculate();
        public int? FILevelValue => _fiCalculator.Calculate();
        public bool WStepCompleted => _wCalculator.Calculate().HasValue;
        #endregion

        #region Clinical Stage and Risks
        public bool CannotSaveLimb
        {
            get => _stageCalculator.CannotSaveLimb;
            set => _stageCalculator.CannotSaveLimb = value;
        }
        public int ClinicalStage => _stageCalculator.CalculateClinicalStage();
        public string AmputationRisk => _stageCalculator.CalculateAmputationRisk();
        public string RevascularizationBenefit => _stageCalculator.CalculateRevascularizationBenefit();
        #endregion

        #region CRAB Results
        public int? CRABTotalScore => _crabCalculator.CalculateScore();
        public string CRABRiskLevel => _crabCalculator.GetRiskLevel();
        public string CRABRiskDescription => _crabCalculator.GetRiskDescription();
        public double CRABMortalityRiskPercentage => _crabCalculator.GetMortalityRiskPercentage();
        #endregion

        #region 2YLE Results
        public double? YLETotalScore => _yleCalculator.CalculateScore();
        public string YLERiskLevel => _yleCalculator.GetRiskLevel();
        public string YLERiskDescription => _yleCalculator.GetRiskDescription();
        public string YLESurvivalPrediction => _yleCalculator.GetSurvivalPrediction();
        public double YLEEstimatedSurvivalPercentage => _yleCalculator.GetEstimatedSurvivalPercentage();
        #endregion

        #region GLASS Results
        public int? GLASSStage => _glassCalculator.CalculateStage();
        public string GLASSStageDescription => _glassCalculator.GetStageDescription();
        public string GLASSDetailedDescription => _glassCalculator.GetDetailedDescription();
        public string GLASSTreatmentRecommendation => _glassCalculator.GetTreatmentRecommendation();
        #endregion

        #region Surgical Risk Calculations
        public string CalculatedSurgicalRisk
        {
            get
            {
                var crabScore = CRABTotalScore ?? 0;
                var yleScore = YLETotalScore ?? 0.0;

                // Високий ризик: CRAB ≥7 (смертність >5%) або 2YLE ≥8 (виживаність <50%)
                if (crabScore >= 7 || yleScore >= 8.0)
                {
                    return "Високий";
                }
                // Помірний ризик: CRAB <7 (смертність <5%) та 2YLE <8 (виживаність >50%)
                else if (crabScore < 7 && yleScore < 8.0)
                {
                    return "Помірний";
                }

                return "Невизначений";
            }
        }

        public bool IsHighSurgicalRisk
        {
            get
            {
                var crabScore = CRABTotalScore ?? 0;
                var yleScore = YLETotalScore ?? 0.0;
                return crabScore >= 7 || yleScore >= 8.0;
            }
        }

        public bool IsModerateSurgicalRisk
        {
            get
            {
                var crabScore = CRABTotalScore ?? 0;
                var yleScore = YLETotalScore ?? 0.0;
                return crabScore < 7 && yleScore < 8.0;
            }
        }
        #endregion

        #region Helper Properties
        public bool ShouldShowPsatField => _vascularData.ShouldShowPsatField;
        public bool ShouldShowTcPO2Field => _vascularData.ShouldShowTcPO2Field;
        public bool HasSirs => _fiCalculator.HasSirs();
        public bool ShouldShowHyperemiaField => _fiCalculator.ShouldShowHyperemiaField();
        public bool ShouldShowSirsAbsentSection => _fiCalculator.ShouldShowSirsAbsentSection();
        #endregion

        #region Business Logic Properties
        public bool CanContinueGLASS => GLASSStage.HasValue;

        public bool CanContinue =>
            ((KpiValue < 0.9 && KpiValue > 0) || (KpiValue > 1.4 && PpiValue < 0.7))
            && WLevelValue.HasValue;

        public bool CanContinueI => ILevelValue.HasValue;
        public bool CanContinueFI => FILevelValue.HasValue;

        public bool NeedExit =>
            (KpiValue >= 0.9 && KpiValue <= 1.4) || (KpiValue > 1.4 && PpiValue >= 0.7);
        #endregion

        #region Public Methods - Value Updates
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
        #endregion

        #region Public Methods - Data Management
        public void Reset()
        {
            // Скидання всіх моделей
            _vascularData.Reset();
            _woundData.Reset();
            _infectionData.Reset();
            _crabData.Reset();
            _yleData.Reset();
            _glassData.Reset();
            _uiState.Reset();

            // Скидання станів кроків
            KpiStepCompleted = false;
            PpiStepCompleted = false;
            IsWCompleted = false;
            IsICompleted = false;
            IsfICompleted = false;
            IsWiFIResultsCompleted = false;
            IsCRABCompleted = false;
            Is2YLECompleted = false;
            IsSurgicalRiskCompleted = false;
            IsGLASSCompleted = false;
            GLASSSelectedStage = null;
            GLASSSubStage = null;
            _stageCalculator.CannotSaveLimb = false;
            GLASSFemoroPoplitealStage = null;
            IsGLASSFemoroPoplitealCompleted = false;
            GLASSInfrapoplitealStage = null;
            IsGLASSInfrapoplitealCompleted = false;
            GLASSFinalStage = null;
            IsGLASSFinalCompleted = false;
            SubmalleolarDescriptor = null;
            IsSubmalleolarDiseaseCompleted = false;
            IsRevascularizationAssessmentCompleted = false;
            IsRevascularizationMethodCompleted = false;
            NotifyStateChanged();
        }
        #endregion

        #region Public Methods - Data Access
        public VascularData GetVascularData() => _vascularData;
        public WoundData GetWoundData() => _woundData;
        public InfectionData GetInfectionData() => _infectionData;
        public CRABData GetCRABData() => _crabData;
        public YLEData GetYLEData() => _yleData;
        public GLASSData GetGLASSData() => _glassData;
        public UIState GetUIState() => _uiState;
        #endregion
    }
}