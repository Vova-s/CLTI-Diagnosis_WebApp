using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class RevascularizationAssessment
    {
        private bool hasPoorPerfusion = false;
        private bool hasWoundProgression = false;

        [Inject]
        public CLTI.Diagnosis.Services.CltiCaseService? CaseService { get; set; }

        protected override void OnInitialized()
        {
            StateService.OnChange += HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private async Task OnPoorPerfusionChanged(bool value)
        {
            hasPoorPerfusion = value;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnWoundProgressionChanged(bool value)
        {
            hasWoundProgression = value;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private string GetWiFIResult()
        {
            var w = StateService.WLevelValue ?? 0;
            var i = StateService.ILevelValue ?? 0;
            var fi = StateService.FILevelValue ?? 0;
            return $"W{w}I{i}fI{fi}";
        }

        private string GetClinicalStageState()
        {
            return StateService.ClinicalStage switch
            {
                1 => "success",
                2 or 3 => "warning",
                4 => "error",
                _ => "default"
            };
        }

        // Показуємо чекбокс "Погана перфузія" тільки для I0 і W2-3
        private bool ShouldShowPoorPerfusion()
        {
            var i = StateService.ILevelValue ?? 0;
            var w = StateService.WLevelValue ?? 0;

            return i == 0 && (w == 2 || w == 3);
        }

        // Показуємо чекбокс "Рана прогресує" тільки для I1
        private bool ShouldShowWoundProgression()
        {
            var i = StateService.ILevelValue ?? 0;

            return i == 1;
        }

        private string? GetRevascularizationRecommendation()
        {
            var w = StateService.WLevelValue ?? 0;
            var i = StateService.ILevelValue ?? 0;
            var fi = StateService.FILevelValue ?? 0;

            // Згідно з алгоритмом WiFI
            // I0 + W2-3 + погана перфузія = оперувати
            if (i == 0 && (w == 2 || w == 3) && hasPoorPerfusion)
            {
                return "РЕКОМЕНДУЄТЬСЯ РЕВАСКУЛЯРИЗАЦІЯ";
            }

            // I0 + W2-3 без поганої перфузії = не оперувати
            if (i == 0 && (w == 2 || w == 3) && !hasPoorPerfusion)
            {
                return "РЕВАСКУЛЯРИЗАЦІЯ НЕ РЕКОМЕНДУЄТЬСЯ";
            }

            // I1 + рана прогресує = оперувати
            if (i == 1 && hasWoundProgression)
            {
                return "РЕКОМЕНДУЄТЬСЯ РЕВАСКУЛЯРИЗАЦІЯ";
            }

            // I1 + рана не прогресує = не оперувати
            if (i == 1 && !hasWoundProgression)
            {
                return "РЕВАСКУЛЯРИЗАЦІЯ НЕ РЕКОМЕНДУЄТЬСЯ";
            }

            // I2-3 - завжди оперувати незалежно від W
            if (i >= 2)
            {
                return "РЕКОМЕНДУЄТЬСЯ РЕВАСКУЛЯРИЗАЦІЯ";
            }

            // I0 + W0-1 - не оперувати
            if (i == 0 && (w == 0 || w == 1))
            {
                return "РЕВАСКУЛЯРИЗАЦІЯ НЕ РЕКОМЕНДУЄТЬСЯ";
            }

            return null;
        }

        private bool IsRevascularizationRecommended()
        {
            var recommendation = GetRevascularizationRecommendation();
            return recommendation?.Contains("РЕКОМЕНДУЄТЬСЯ РЕВАСКУЛЯРИЗАЦІЯ") == true;
        }

        private bool IsRevascularizationNotRecommended()
        {
            var recommendation = GetRevascularizationRecommendation();
            return recommendation?.Contains("НЕ РЕКОМЕНДУЄТЬСЯ") == true;
        }

        private string GetRecommendationState()
        {
            if (IsRevascularizationRecommended())
                return "warning";
            else if (IsRevascularizationNotRecommended())
                return "success";

            return "default";
        }

        private string? GetDetailedExplanation()
        {
            var w = StateService.WLevelValue ?? 0;
            var i = StateService.ILevelValue ?? 0;

            if (i == 0 && (w == 2 || w == 3) && hasPoorPerfusion)
            {
                return "При I0 і W2-3 з поганою перфузією реваскуляризація може поліпшити загоєння ран і зменшити ризик ампутації.";
            }

            if (i == 0 && (w == 2 || w == 3) && !hasPoorPerfusion)
            {
                return "При I0 і W2-3 без поганої перфузії достатньо консервативного лікування і догляду за раною.";
            }

            if (i == 1 && hasWoundProgression)
            {
                return "При I1 з прогресуванням рани реваскуляризація необхідна для покращення загоєння.";
            }

            if (i == 1 && !hasWoundProgression)
            {
                return "При I1 без прогресування рани можна продовжити консервативне лікування з регулярним моніторингом.";
            }

            if (i >= 2)
            {
                return $"При I{i} наявна значуща ішемія, що вимагає реваскуляризації для збереження кінцівки.";
            }

            if (i == 0 && (w == 0 || w == 1))
            {
                return "При мінімальній ішемії та незначних ранових дефектах достатньо консервативного лікування.";
            }

            return null;
        }

        // В методі Continue() додайте:
        private async Task Continue()
        {
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }
            StateService.IsRevascularizationAssessmentCompleted = true; // ДОДАНО
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
            // Переходимо до наступного етапу (вибір методу реваскуляризації)
            NavigationManager.NavigateTo("/Algoritm/Pages/RevascularizationMethod", forceLoad: true);
        }

        // В методі SaveAndExit() додайте:
        private async Task SaveAndExit()
        {
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }
            StateService.IsRevascularizationAssessmentCompleted = true; // ДОДАНО
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
            // Повертаємося на домашню сторінку
            NavigationManager.NavigateTo("/", forceLoad: true);
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}