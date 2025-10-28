using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Pages
{
    public partial class WiFI_I
    {
        // Списки для DropDown компонентів
        private List<string> psatItems = new() { "≥60", "40-59", "30-39", "<30" };

        // Поля для зберігання стану
        private string tcPO2Value = "";

        [Inject]
        public CLTI.Diagnosis.Client.Features.Diagnosis.Services.CltiCaseService? CaseService { get; set; }
        protected override void OnInitialized()
        {
            // Підписуємося на зміни стану
            StateService.OnChange += HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            InvokeAsync(() => StateHasChanged());
        }

        private async Task OnPsatSelect(string selectedValue)
        {
            StateService.PsatValue = selectedValue;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnArterialCalcificationChanged(bool isChecked)
        {
            StateService.HasArterialCalcification = isChecked;

            // Якщо кальцифікація була знята, очищаємо TcPO2
            if (!isChecked)
            {
                StateService.TcPO2Value = null;
                tcPO2Value = "";
            }

            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnTcPO2Changed(string value)
        {
            tcPO2Value = value;

            if (double.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double numValue))
            {
                StateService.TcPO2Value = numValue;
            }
            else
            {
                StateService.TcPO2Value = null;
            }

            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task Continue()
        {
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/Wifi_fI", forceLoad: true);
            StateService.IsICompleted = true;
        }

        public void Dispose()
        {
            // Відписуємося від події при знищенні компонента
            StateService.OnChange -= HandleStateChanged;
        }
    }
}