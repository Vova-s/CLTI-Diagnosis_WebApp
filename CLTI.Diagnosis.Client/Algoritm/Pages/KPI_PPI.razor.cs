using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class KPI_PPI
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

        private void HandleKpiInput(ChangeEventArgs e)
        {
            kpiValueString = e.Value?.ToString() ?? "";
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

        private void UpdatePpiValue(ChangeEventArgs e)
        {
            ppiValue = e.Value?.ToString() ?? "";
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
            NavigationManager.NavigateTo("/Algoritm/Pages/Wifi", forceLoad: true);
        }

        private void Finish()
        {
            kpiValueString = "";
            ppiValue = "";
            StateService.Reset();
        }

        public void Dispose()
        {
            if (onStateChanged is not null)
                StateService.OnChange -= onStateChanged;
        }
    }
}
