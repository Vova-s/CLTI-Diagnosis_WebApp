namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class WiFI_fI
    {
        // Додаткові поля для відстеження стану
        private string sirsAbsent = "";
        private string hyperemiaSize = "";

        protected override void OnInitialized()
        {
            // Підписуємося на зміни стану
            StateService.OnChange += HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            InvokeAsync(() => StateHasChanged());
        }

        // Обробники для основних ознак інфекції (тепер просто чекбокси)
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

        // Обробники для SIRS ознак
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
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnLeukocytosisChanged(bool isChecked)
        {
            StateService.HasLeukocytosis = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        // Обробник для SIRS відсутній
        private async Task OnSirsAbsentChanged(string type, bool isSelected)
        {
            if (isSelected)
            {
                sirsAbsent = type;
                StateService.SirsAbsentType = type;

                // Скидаємо розмір гіперемії при зміні типу ураження
                if (type != "Шкіра")
                {
                    hyperemiaSize = "";
                    StateService.HyperemiaSize = null;
                }

                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        // Обробник для розміру гіперемії
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
                0 => "Рівень fI: fI0 - Немає ознак інфекції",
                1 => "Рівень fI: fI1 - Місцева інфекція (шкіра, гіперемія 0,5-2 см)",
                2 => "Рівень fI: fI2 - Місцева інфекція з поширенням (гіперемія >2 см або ураження кісток)",
                3 => "Рівень fI: fI3 - Системна інфекція (SIRS наявний)",
                _ => $"Рівень fI: fI{level}"
            };
        }

        private async Task Continue()
        {
            await InvokeAsync(StateHasChanged);
            // Переходимо до фінальної сторінки оцінки
            NavigationManager.NavigateTo("/Algoritm/Pages/FinalAssessment", forceLoad: true);
            StateService.IsfICompleted = true;
        }

        public void Dispose()
        {
            // Відписуємося від події при знищенні компонента
            StateService.OnChange -= HandleStateChanged;
        }
    }
}