using Microsoft.AspNetCore.Components;
using CLTI.Diagnosis.Client.Algoritm.Services;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class SideBarDropDown
    {
        [Parameter] public string Placeholder { get; set; } = "Оберіть...";
        [Parameter] public List<string> Items { get; set; } = new();
        [Parameter] public bool DefaultOpen { get; set; } = false;
        [Parameter] public EventCallback<Dictionary<string, bool>> OnChanged { get; set; }
        [Inject] public StateService? StateService { get; set; }

        private bool IsOpen { get; set; } = false;

        private void ToggleOpen() => IsOpen = !IsOpen;

        protected override void OnInitialized()
        {
            IsOpen = DefaultOpen;
        }

        private bool GetItemCompletedStatus(string item)
        {
            if (StateService == null) return false;

            return item switch
            {
                "КПІ" => StateService.KpiStepCompleted,
                "ППІ" => StateService.PpiStepCompleted,
                "Оцінка критерію W" => StateService.WStepCompleted,
                "Оцінка критерію I" => StateService.IsICompleted,
                "Оцінка критерію fI" => StateService.IsfICompleted,
                "Оцінка результатів" => StateService.IsWiFIResultsCompleted,
                "Оцінка перипроцедуральної смертності" => StateService.IsCRABCompleted,
                "Оцінка дворічної виживаності" => StateService.Is2YLECompleted,
                "Кінцева оцінка ступеня хірургічного ризику" => StateService.IsSurgicalRiskCompleted,
                _ => false
            };
        }

        private async Task OnItemStatusChanged(string item, bool isChecked)
        {
            await Task.CompletedTask;
        }
    }
}