using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class CRAB
    {
        protected override void OnInitialized()
        {
            StateService.OnChange += HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private async Task OnAgeChanged(bool isChecked)
        {
            StateService.IsOlderThan75 = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnPreviousAmputationChanged(bool isChecked)
        {
            StateService.HasPreviousAmputationOrRevascularization = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnPainNecrosisChanged(bool isChecked)
        {
            StateService.HasPainAndNecrosis = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnPartialFunctionalDependenceChanged(bool isChecked)
        {
            StateService.HasPartialFunctionalDependence = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnHemodialysisChanged(bool isChecked)
        {
            StateService.IsOnHemodialysis = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnAnginaOrMIChanged(bool isChecked)
        {
            StateService.HasAnginaOrMI = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnUrgentOperationChanged(bool isChecked)
        {
            StateService.IsUrgentOperation = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnCompleteFunctionalDependenceChanged(bool isChecked)
        {
            StateService.HasCompleteFunctionalDependence = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private string GetCRABRiskMessage()
        {
            var score = StateService.CRABTotalScore ?? 0;

            if (score <= 6)
                return "Низький ризик періпроцедуральної смертності";
            else if (score >= 7 && score <= 10)
                return "Помірний ризик періпроцедуральної смертності";
            else if (score >= 11)
                return "Високий ризик періпроцедуральної смертності";
            else
                return "Невизначений ризик";
        }

        private string GetCRABRiskState()
        {
            var score = StateService.CRABTotalScore ?? 0;

            if (score <= 6)
                return "success";
            else if (score >= 7 && score <= 10)
                return "warning";
            else if (score >= 11)
                return "error";
            else
                return "default";
        }

        private async Task Continue()
        {
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/2YLE", forceLoad: true);
            StateService.IsCRABCompleted = true;
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}