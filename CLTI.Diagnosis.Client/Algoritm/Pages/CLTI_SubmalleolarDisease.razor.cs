using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class CLTI_SubmalleolarDisease
    {
        private string selectedDescriptor = "";

        [Inject]
        public CLTI.Diagnosis.Client.Services.CltiCaseService? CaseService { get; set; }

        protected override void OnInitialized()
        {
            StateService.OnChange += HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private async Task OnDescriptorChanged(string descriptor, bool isSelected)
        {
            if (isSelected)
            {
                selectedDescriptor = descriptor;
                StateService.SubmalleolarDescriptor = descriptor;
                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        private int GetStageNumber(string? stageString)
        {
            if (string.IsNullOrEmpty(stageString))
                return 0;

            if (stageString.StartsWith("Stage") && int.TryParse(stageString.Substring(5), out int stage))
            {
                return stage;
            }

            return 0;
        }

        private string GetAllPrognosticIndicators()
        {
            var lines = new List<string>
    {
        $"• Ризик ампутації в межах 1 року – {StateService.AmputationRisk.ToLower()}",
        $"• Користь від реваскуляризації – {StateService.RevascularizationBenefit.ToLower()}",
        $"• Перипроцедуральна смертність {(StateService.CRABTotalScore >= 7 ? ">5%" : "<5%")}",
        $"• Дворічна виживаність {(StateService.YLETotalScore >= 8.0 ? "<50%" : ">50%")}",
        $"• Ймовірність технічного неуспіху реваскуляризації {GetFailureRate()}",
        $"• Функціонування зони реконструкції впродовж 1 року {GetFunctionalityRate()}"
    };

            return string.Join("<br />", lines);
        }


        private string GetFailureRate()
        {
            return StateService.GLASSFinalStage switch
            {
                "I" => "<10%",
                "II" => "10-20%",
                "III" => ">20%",
                _ => "Не визначено"
            };
        }

        private string GetFunctionalityRate()
        {
            return StateService.GLASSFinalStage switch
            {
                "I" => ">70%",
                "II" => "50-70%",
                "III" => "<50%",
                _ => "Не визначено"
            };
        }

        private async Task CompleteDiagnosis()
        {
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }
            StateService.IsSubmalleolarDiseaseCompleted = true;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);

            // Повертаємось на головну сторінку або показуємо підтвердження завершення
            NavigationManager.NavigateTo("/Algoritm/Pages/RevascularizationAssessment", forceLoad: true);
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}