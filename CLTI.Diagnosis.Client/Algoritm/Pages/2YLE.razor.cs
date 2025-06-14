using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class _2YLE
    {
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

        private async Task OnNonAmbulatoryChanged(bool isChecked)
        {
            StateService.IsNonAmbulatory = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnRutherford5Changed(bool isChecked)
        {
            StateService.HasRutherford5 = isChecked;
            // Взаємовиключення з Rutherford 6
            if (isChecked && StateService.HasRutherford6)
            {
                StateService.HasRutherford6 = false;
            }
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnRutherford6Changed(bool isChecked)
        {
            StateService.HasRutherford6 = isChecked;
            // Взаємовиключення з Rutherford 5
            if (isChecked && StateService.HasRutherford5)
            {
                StateService.HasRutherford5 = false;
            }
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnCerebrovascularDiseaseChanged(bool isChecked)
        {
            StateService.HasCerebrovascularDisease = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnHemodialysisChanged(bool isChecked)
        {
            StateService.Has2YLEHemodialysis = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnBMI18to19Changed(bool isChecked)
        {
            StateService.HasBMI18to19 = isChecked;
            // Взаємовиключення з BMI <18
            if (isChecked && StateService.HasBMILessThan18)
            {
                StateService.HasBMILessThan18 = false;
            }
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnBMILessThan18Changed(bool isChecked)
        {
            StateService.HasBMILessThan18 = isChecked;
            // Взаємовиключення з BMI 18-19
            if (isChecked && StateService.HasBMI18to19)
            {
                StateService.HasBMI18to19 = false;
            }
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnAge65to79Changed(bool isChecked)
        {
            StateService.IsAge65to79 = isChecked;
            // Взаємовиключення з віком ≥80
            if (isChecked && StateService.IsAge80Plus)
            {
                StateService.IsAge80Plus = false;
            }
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnAge80PlusChanged(bool isChecked)
        {
            StateService.IsAge80Plus = isChecked;
            // Взаємовиключення з віком 65-79
            if (isChecked && StateService.IsAge65to79)
            {
                StateService.IsAge65to79 = false;
            }
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnEjectionFraction40to49Changed(bool isChecked)
        {
            StateService.HasEjectionFraction40to49 = isChecked;
            // Взаємовиключення з фракцією <40%
            if (isChecked && StateService.HasEjectionFractionLessThan40)
            {
                StateService.HasEjectionFractionLessThan40 = false;
            }
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnEjectionFractionLessThan40Changed(bool isChecked)
        {
            StateService.HasEjectionFractionLessThan40 = isChecked;
            // Взаємовиключення з фракцією 40-49%
            if (isChecked && StateService.HasEjectionFraction40to49)
            {
                StateService.HasEjectionFraction40to49 = false;
            }
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private string Get2YLERiskMessage()
        {
            var score = StateService.YLETotalScore ?? 0.0;

            if (score <= 3.0)
                return "Низький ризик смертності протягом 2 років";
            else if (score > 3.0 && score <= 6.0)
                return "Помірний ризик смертності протягом 2 років";
            else if (score > 6.0)
                return "Високий ризик смертності протягом 2 років";
            else
                return "Невизначений ризик";
        }

        private string Get2YLERiskState()
        {
            var score = StateService.YLETotalScore ?? 0.0;

            if (score <= 3.0)
                return "success";
            else if (score > 3.0 && score <= 6.0)
                return "warning";
            else if (score > 6.0)
                return "error";
            else
                return "default";
        }

        private async Task Continue()
        {
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }

            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/SurgicalRisk", forceLoad: true);
            StateService.Is2YLECompleted = true;
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}