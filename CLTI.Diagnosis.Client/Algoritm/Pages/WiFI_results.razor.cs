using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class WiFI_results
    {
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

        private string GetClinicalStageClass()
        {
            if (StateService.CannotSaveLimb)
                return "stage-5";

            return StateService.ClinicalStage switch
            {
                1 => "stage-1",
                2 or 3 => "stage-2-3",
                4 => "stage-4",
                _ => ""
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

        private string GetAmputationRiskClass()
        {
            if (StateService.CannotSaveLimb)
                return "very-high";

            return StateService.AmputationRisk.ToLower() switch
            {
                "дуже низький" => "very-low",
                "низький" => "low",
                "помірний" => "moderate",
                "високий" => "high",
                _ => ""
            };
        }

        private string GetAmputationRiskText()
        {
            return StateService.CannotSaveLimb ? "Надзвичайно високий" : StateService.AmputationRisk;
        }

        private string GetRevascularizationClass()
        {
            return StateService.RevascularizationBenefit.ToLower() switch
            {
                "висока" => "high",
                "середня" => "moderate",
                "низька" => "low",
                "дуже низька" => "very-low",
                _ => ""
            };
        }

        private string GetRevascularizationText()
        {
            return StateService.RevascularizationBenefit;
        }

        private async Task ReturnToHome()
        {
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/", forceLoad: true);
        }

        private async Task NewPatient()
        {
            StateService.Reset();
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/KPI-PPI", forceLoad: true);
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}