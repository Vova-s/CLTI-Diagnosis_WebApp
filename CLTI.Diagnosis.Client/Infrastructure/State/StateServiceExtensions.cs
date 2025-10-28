using CLTI.Diagnosis.Client.Infrastructure.Auth;
using CLTI.Diagnosis.Client.Infrastructure.Http;
using CLTI.Diagnosis.Client.Infrastructure.State;
using CLTI.Diagnosis.Client.Features.Diagnosis.Services;
using CLTI.Diagnosis.Client.Infrastructure.Auth;
using CLTI.Diagnosis.Client.Infrastructure.Http;
using CLTI.Diagnosis.Client.Infrastructure.State;
using CLTI.Diagnosis.Client.Features.Diagnosis.Services;

namespace CLTI.Diagnosis.Client.Extensions
{
    /// <summary>
    /// Extension методи для конвертації між StateService та StateServiceDto
    /// </summary>
    public static class StateServiceExtensions
    {
        /// <summary>
        /// Конвертує StateService в StateServiceDto для відправки через API
        /// </summary>
        /// <param name="stateService">StateService з поточними даними</param>
        /// <returns>DTO готове для відправки</returns>
        public static StateServiceDto ToDto(this StateService stateService)
        {
            return new StateServiceDto
            {
                CaseId = stateService.CaseId,

                // === Гемодинамічні параметри ===
                KpiValue = stateService.KpiValue,
                PpiValue = stateService.PpiValue,
                PsatValue = stateService.PsatValue,
                TcPO2Value = stateService.TcPO2Value,
                HasArterialCalcification = stateService.HasArterialCalcification,
                HasDiabetes = stateService.HasDiabetes,

                // === Дані про рани ===
                HasNecrosis = stateService.HasNecrosis,
                NecrosisType = stateService.NecrosisType,
                GangreneSpread = stateService.GangreneSpread,
                UlcerLocation = stateService.UlcerLocation,
                UlcerAffectsBone = stateService.UlcerAffectsBone,
                UlcerDepth = stateService.UlcerDepth,
                UlcerLocation2 = stateService.UlcerLocation2,

                // === Дані про інфекцію ===
                HasLocalSwelling = stateService.HasLocalSwelling,
                HasErythema = stateService.HasErythema,
                HasLocalPain = stateService.HasLocalPain,
                HasLocalWarmth = stateService.HasLocalWarmth,
                HasPus = stateService.HasPus,
                HasTachycardia = stateService.HasTachycardia,
                HasTachypnea = stateService.HasTachypnea,
                HasTemperatureChange = stateService.HasTemperatureChange,
                HasLeukocytosis = stateService.HasLeukocytosis,
                SirsAbsentType = stateService.SirsAbsentType,
                HyperemiaSize = stateService.HyperemiaSize,

                // === CRAB дані ===
                IsOlderThan75 = stateService.IsOlderThan75,
                HasPreviousAmputationOrRevascularization = stateService.HasPreviousAmputationOrRevascularization,
                HasPainAndNecrosis = stateService.HasPainAndNecrosis,
                HasPartialFunctionalDependence = stateService.HasPartialFunctionalDependence,
                IsOnHemodialysis = stateService.IsOnHemodialysis,
                HasAnginaOrMI = stateService.HasAnginaOrMI,
                IsUrgentOperation = stateService.IsUrgentOperation,
                HasCompleteFunctionalDependence = stateService.HasCompleteFunctionalDependence,

                // === 2YLE дані ===
                IsNonAmbulatory = stateService.IsNonAmbulatory,
                HasRutherford5 = stateService.HasRutherford5,
                HasRutherford6 = stateService.HasRutherford6,
                HasCerebrovascularDisease = stateService.HasCerebrovascularDisease,
                Has2YLEHemodialysis = stateService.Has2YLEHemodialysis,
                HasBMI18to19 = stateService.HasBMI18to19,
                HasBMILessThan18 = stateService.HasBMILessThan18,
                IsAge65to79 = stateService.IsAge65to79,
                IsAge80Plus = stateService.IsAge80Plus,
                HasEjectionFraction40to49 = stateService.HasEjectionFraction40to49,
                HasEjectionFractionLessThan40 = stateService.HasEjectionFractionLessThan40,

                // === GLASS дані ===
                GLASSSelectedStage = stateService.GLASSSelectedStage,
                GLASSSubStage = stateService.GLASSSubStage,
                GLASSFemoroPoplitealStage = stateService.GLASSFemoroPoplitealStage,
                GLASSInfrapoplitealStage = stateService.GLASSInfrapoplitealStage,
                GLASSFinalStage = stateService.GLASSFinalStage,
                SubmalleolarDescriptor = stateService.SubmalleolarDescriptor,

                // === Стани завершення кроків ===
                IsWCompleted = stateService.IsWCompleted,
                IsICompleted = stateService.IsICompleted,
                IsfICompleted = stateService.IsfICompleted,
                IsWiFIResultsCompleted = stateService.IsWiFIResultsCompleted,
                IsCRABCompleted = stateService.IsCRABCompleted,
                Is2YLECompleted = stateService.Is2YLECompleted,
                IsSurgicalRiskCompleted = stateService.IsSurgicalRiskCompleted,
                IsGLASSCompleted = stateService.IsGLASSCompleted,
                IsGLASSFemoroPoplitealCompleted = stateService.IsGLASSFemoroPoplitealCompleted,
                IsGLASSInfrapoplitealCompleted = stateService.IsGLASSInfrapoplitealCompleted,
                IsGLASSFinalCompleted = stateService.IsGLASSFinalCompleted,
                IsSubmalleolarDiseaseCompleted = stateService.IsSubmalleolarDiseaseCompleted,
                IsRevascularizationAssessmentCompleted = stateService.IsRevascularizationAssessmentCompleted,
                IsRevascularizationMethodCompleted = stateService.IsRevascularizationMethodCompleted,

                // === Додаткові поля ===
                CannotSaveLimb = stateService.CannotSaveLimb,

                // === Розраховані значення ===
                WLevelValue = stateService.WLevelValue,
                ILevelValue = stateService.ILevelValue,
                FILevelValue = stateService.FILevelValue,
                ClinicalStage = stateService.ClinicalStage,
                CRABTotalScore = stateService.CRABTotalScore,
                YLETotalScore = stateService.YLETotalScore
            };
        }

        /// <summary>
        /// Оновлює StateService даними з StateServiceDto (отриманими з API)
        /// </summary>
        /// <param name="stateService">StateService для оновлення</param>
        /// <param name="dto">DTO з новими даними</param>
        public static void UpdateFromDto(this StateService stateService, StateServiceDto dto)
        {
            // Зберігаємо CaseId
            stateService.CaseId = dto.CaseId;

            // Оновлюємо основні дані через наявні методи StateService
            stateService.UpdateKpiValue(dto.KpiValue);
            stateService.UpdatePpiValue(dto.PpiValue);

            // Оновлюємо дані через властивості (це тригерить перерахунок)
            // === Судинні дані ===
            var vascularData = stateService.GetVascularData();
            vascularData.PsatValue = dto.PsatValue;
            vascularData.TcPO2Value = dto.TcPO2Value;
            vascularData.HasArterialCalcification = dto.HasArterialCalcification;
            vascularData.HasDiabetes = dto.HasDiabetes;

            // === Дані про рани ===
            var woundData = stateService.GetWoundData();
            woundData.HasNecrosis = dto.HasNecrosis;
            woundData.NecrosisType = dto.NecrosisType;
            woundData.GangreneSpread = dto.GangreneSpread;
            woundData.UlcerLocation = dto.UlcerLocation;
            woundData.UlcerAffectsBone = dto.UlcerAffectsBone;
            woundData.UlcerDepth = dto.UlcerDepth;
            woundData.UlcerLocation2 = dto.UlcerLocation2;

            // === Дані про інфекцію ===
            var infectionData = stateService.GetInfectionData();
            infectionData.HasLocalSwelling = dto.HasLocalSwelling;
            infectionData.HasErythema = dto.HasErythema;
            infectionData.HasLocalPain = dto.HasLocalPain;
            infectionData.HasLocalWarmth = dto.HasLocalWarmth;
            infectionData.HasPus = dto.HasPus;
            infectionData.HasTachycardia = dto.HasTachycardia;
            infectionData.HasTachypnea = dto.HasTachypnea;
            infectionData.HasTemperatureChange = dto.HasTemperatureChange;
            infectionData.HasLeukocytosis = dto.HasLeukocytosis;
            infectionData.SirsAbsentType = dto.SirsAbsentType;
            infectionData.HyperemiaSize = dto.HyperemiaSize;

            // === CRAB дані ===
            var crabData = stateService.GetCRABData();
            crabData.IsOlderThan75 = dto.IsOlderThan75;
            crabData.HasPreviousAmputationOrRevascularization = dto.HasPreviousAmputationOrRevascularization;
            crabData.HasPainAndNecrosis = dto.HasPainAndNecrosis;
            crabData.HasPartialFunctionalDependence = dto.HasPartialFunctionalDependence;
            crabData.IsOnHemodialysis = dto.IsOnHemodialysis;
            crabData.HasAnginaOrMI = dto.HasAnginaOrMI;
            crabData.IsUrgentOperation = dto.IsUrgentOperation;
            crabData.HasCompleteFunctionalDependence = dto.HasCompleteFunctionalDependence;

            // === 2YLE дані ===
            var yleData = stateService.GetYLEData();
            yleData.IsNonAmbulatory = dto.IsNonAmbulatory;
            yleData.HasRutherford5 = dto.HasRutherford5;
            yleData.HasRutherford6 = dto.HasRutherford6;
            yleData.HasCerebrovascularDisease = dto.HasCerebrovascularDisease;
            yleData.Has2YLEHemodialysis = dto.Has2YLEHemodialysis;
            yleData.HasBMI18to19 = dto.HasBMI18to19;
            yleData.HasBMILessThan18 = dto.HasBMILessThan18;
            yleData.IsAge65to79 = dto.IsAge65to79;
            yleData.IsAge80Plus = dto.IsAge80Plus;
            yleData.HasEjectionFraction40to49 = dto.HasEjectionFraction40to49;
            yleData.HasEjectionFractionLessThan40 = dto.HasEjectionFractionLessThan40;

            // === GLASS дані ===
            stateService.GLASSSelectedStage = dto.GLASSSelectedStage;
            stateService.GLASSSubStage = dto.GLASSSubStage;
            stateService.GLASSFemoroPoplitealStage = dto.GLASSFemoroPoplitealStage;
            stateService.GLASSInfrapoplitealStage = dto.GLASSInfrapoplitealStage;
            stateService.GLASSFinalStage = dto.GLASSFinalStage;
            stateService.SubmalleolarDescriptor = dto.SubmalleolarDescriptor;

            // === Стани завершення кроків ===
            stateService.IsWCompleted = dto.IsWCompleted;
            stateService.IsICompleted = dto.IsICompleted;
            stateService.IsfICompleted = dto.IsfICompleted;
            stateService.IsWiFIResultsCompleted = dto.IsWiFIResultsCompleted;
            stateService.IsCRABCompleted = dto.IsCRABCompleted;
            stateService.Is2YLECompleted = dto.Is2YLECompleted;
            stateService.IsSurgicalRiskCompleted = dto.IsSurgicalRiskCompleted;
            stateService.IsGLASSCompleted = dto.IsGLASSCompleted;
            stateService.IsGLASSFemoroPoplitealCompleted = dto.IsGLASSFemoroPoplitealCompleted;
            stateService.IsGLASSInfrapoplitealCompleted = dto.IsGLASSInfrapoplitealCompleted;
            stateService.IsGLASSFinalCompleted = dto.IsGLASSFinalCompleted;
            stateService.IsSubmalleolarDiseaseCompleted = dto.IsSubmalleolarDiseaseCompleted;
            stateService.IsRevascularizationAssessmentCompleted = dto.IsRevascularizationAssessmentCompleted;
            stateService.IsRevascularizationMethodCompleted = dto.IsRevascularizationMethodCompleted;

            // === Додаткові поля ===
            stateService.CannotSaveLimb = dto.CannotSaveLimb;

            // Повідомляємо про зміни
            stateService.NotifyStateChanged();
        }
    }
}