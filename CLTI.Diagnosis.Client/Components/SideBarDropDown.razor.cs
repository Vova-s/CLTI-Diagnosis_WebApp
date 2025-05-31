using Microsoft.AspNetCore.Components;
using CLTI.Diagnosis.Client.Algoritm;

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

            // Автоматично визначаємо статус на основі StateService
            return item switch
            {
                "КПІ" => StateService.KpiStepCompleted,
                "ППІ" => StateService.PpiStepCompleted,
                _ => false
            };
        }

        private async Task OnItemStatusChanged(string item, bool isChecked)
        {
            // Користувач не може змінювати статус - він автоматичний
            // Цей метод залишається порожнім, оскільки галочки контролюються StateService
            await Task.CompletedTask;
        }
    }
}