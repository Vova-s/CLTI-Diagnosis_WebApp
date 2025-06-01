using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class WiFI_I
    {
        // Списки для DropDown компонентів
        private List<string> psatItems = new() { "≥0,6", "40-59", "30-39", "<30" };

        // Поля для зберігання стану
        private string selectedPsatValue = "";
        private bool hasArterialCalcification = false;
        private bool showTcPO2 = false;
        private string tcPO2Value = "";

        private async Task OnPsatSelect(string selectedValue)
        {
            selectedPsatValue = selectedValue;
            StateService.PsatValue = selectedValue;

            // Оновлюємо стан та перевіряємо чи потрібно показати TcPO2
            CheckIfTcPO2Needed();
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnArterialCalcificationChanged(bool isChecked)
        {
            hasArterialCalcification = isChecked;
            StateService.HasArterialCalcification = isChecked;

            // Якщо є кальцифікація, показуємо TcPO2
            if (isChecked)
            {
                showTcPO2 = true;
            }
            else
            {
                CheckIfTcPO2Needed();
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

        private void CheckIfTcPO2Needed()
        {
            // TcPO2 потрібен якщо:
            // 1. Є артеріальна кальцифікація, або
            // 2. ПСАТ в межах 30-59 (потрібна додаткова оцінка)
            showTcPO2 = hasArterialCalcification ||
                       (selectedPsatValue == "40-59" || selectedPsatValue == "30-39");
        }

        private async Task Continue()
        {
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/Wifi_fI", forceLoad: true);
            StateService.IsICompleted = true;
        }
    }
}