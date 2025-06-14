using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class WiFI_fI
    {
        private string sirsAbsent = "";
        private string hyperemiaSize = "";

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

        // Обробники 5 основних ознак інфекції
        private async Task OnLocalSwellingChanged(bool isChecked)
        {
            StateService.HasLocalSwelling = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnErythemaChanged(bool isChecked)
        {
            StateService.HasErythema = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnLocalPainChanged(bool isChecked)
        {
            StateService.HasLocalPain = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnLocalWarmthChanged(bool isChecked)
        {
            StateService.HasLocalWarmth = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnPusChanged(bool isChecked)
        {
            StateService.HasPus = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        // Обробники ознак SIRS
        private async Task OnTachycardiaChanged(bool isChecked)
        {
            StateService.HasTachycardia = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnTachypneaChanged(bool isChecked)
        {
            StateService.HasTachypnea = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnTemperatureChanged(bool isChecked)
        {
            StateService.HasTemperatureChange = isChecked;

            // Якщо є системна інфекція — очищаємо SIRS-відсутній
            if (isChecked && StateService.HasSirs)
            {
                ClearSirsAbsentData();
            }

            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnLeukocytosisChanged(bool isChecked)
        {
            StateService.HasLeukocytosis = isChecked;

            // Якщо є системна інфекція — очищаємо SIRS-відсутній
            if (isChecked && StateService.HasSirs)
            {
                ClearSirsAbsentData();
            }

            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        // Очистити SIRS-відсутній, якщо стан змінився
        private void ClearSirsAbsentData()
        {
            sirsAbsent = "";
            hyperemiaSize = "";
            StateService.SirsAbsentType = null;
            StateService.HyperemiaSize = null;
        }

        // Обробник вибору типу ураження (шкіра / кістка)
        private async Task OnSirsAbsentChanged(string type, bool isSelected)
        {
            if (isSelected)
            {
                sirsAbsent = type;
                StateService.SirsAbsentType = type;

                if (type != "Шкіра")
                {
                    hyperemiaSize = "";
                    StateService.HyperemiaSize = null;
                }

                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        // Обробник вибору гіперемії
        private async Task OnHyperemiaSizeChanged(string size, bool isSelected)
        {
            if (isSelected)
            {
                hyperemiaSize = size;
                StateService.HyperemiaSize = size;
                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        private string GetResultMessage()
        {
            var level = StateService.FILevelValue;
            return level switch
            {
                0 => "Рівень fI: fI0 - Немає ознак інфекції (0-1 ознака)",
                1 => "Рівень fI: fI1 - Місцева інфекція (шкіра, гіперемія 0,5-2 см)",
                2 => "Рівень fI: fI2 - Місцева інфекція з поширенням (гіперемія >2 см або ураження кісток)",
                3 => "Рівень fI: fI3 - Системна інфекція (SIRS наявний)",
                _ => $"Рівень fI: fI{level}"
            };
        }

        private async Task Continue()
        {
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/WiFI_results", forceLoad: true);
            StateService.IsfICompleted = true;
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}
