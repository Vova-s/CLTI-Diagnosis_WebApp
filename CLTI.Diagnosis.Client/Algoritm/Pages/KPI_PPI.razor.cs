using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class KPI_PPI : IDisposable
    {
        private string kpiValueString = "";
        private string ppiValue = "";
        private Action? onStateChanged;

        protected override void OnInitialized()
        {
            onStateChanged = () => InvokeAsync(StateHasChanged);
            StateService.OnChange += onStateChanged;
        }

        private bool HasKpiValue() => StateService.KpiValue > 0;

        private void HandleKpiInputChanged(string value)
        {
            kpiValueString = value;
            ProcessKpiValue();
        }

        private void ProcessKpiValue()
        {
            if (double.TryParse(kpiValueString.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
            {
                StateService.UpdateKpiValue(value);
            }
            else
            {
                StateService.UpdateKpiValue(0);
            }
        }

        private void HandlePpiInputChanged(string value)
        {
            ppiValue = value;
            ProcessPpiValue();
        }

        private void ProcessPpiValue()
        {
            if (double.TryParse(ppiValue.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
            {
                StateService.UpdatePpiValue(value);
            }
            else
            {
                StateService.UpdatePpiValue(0);
            }
        }

        private async void Continue()
        {
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/Wifi_W", forceLoad: true);
            StateService.ShowWifiSection = true;
        }

        private async void Exit()
        {
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/", forceLoad: true);
        }

        public void Dispose()
        {
            if (onStateChanged is not null)
                StateService.OnChange -= onStateChanged;
        }
    }
}