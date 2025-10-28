using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Pages
{
    public partial class WiFI_results
    {
        [Inject]
        public CLTI.Diagnosis.Client.Features.Diagnosis.Services.CltiCaseService? CaseService { get; set; }
        protected override void OnInitialized()
        {
            StateService.OnChange += HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private async Task OnCannotSaveLimbChanged(bool cannotSave)
        {
            StateService.CannotSaveLimb = cannotSave;
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
            if (StateService.CannotSaveLimb)
                return "error";

            return StateService.ClinicalStage switch
            {
                1 => "success",
                2 or 3 => "warning",
                4 => "error",
                _ => "default"
            };
        }

        private string GetClinicalStageDescription()
        {
            if (StateService.CannotSaveLimb)
                return "Неможливо зберегти кінцівку";

            return StateService.ClinicalStage switch
            {
                1 => "Дуже низький ризик втрати кінцівки",
                2 => "Низький ризик втрати кінцівки",
                3 => "Помірний ризик втрати кінцівки",
                4 => "Високий ризик втрати кінцівки",
                _ => ""
            };
        }

        private string GetAmputationRiskState()
        {
            if (StateService.CannotSaveLimb)
                return "error";

            return StateService.AmputationRisk.ToLower() switch
            {
                "дуже низький" => "success",
                "низький" => "success",
                "помірний" => "warning",
                "високий" => "error",
                _ => "default"
            };
        }

        private string GetAmputationRiskText()
        {
            return StateService.CannotSaveLimb ? "Надзвичайно високий" : StateService.AmputationRisk;
        }

        private string GetRevascularizationState()
        {
            return StateService.RevascularizationBenefit.ToLower() switch
            {
                "висока" => "success",
                "середня" => "warning",
                "низька" => "error",
                "дуже низька" => "error",
                _ => "default"
            };
        }

        private string GetRevascularizationText()
        {
            return StateService.RevascularizationBenefit;
        }

        private async Task Continue()
        {
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }

            // Встановлюємо, що WiFI результати завершено

            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/CRAB", forceLoad: true);
            StateService.IsWiFIResultsCompleted = true;
        }
        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}