using Microsoft.AspNetCore.Components;
using CLTI.Diagnosis.Client.Algoritm.Services;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class SideBarDropDown
    {
        // === Вхідні параметри ===
        [Parameter] public string Placeholder { get; set; } = "Оберіть...";
        [Parameter] public List<string> Items { get; set; } = new();
        [Parameter] public bool DefaultOpen { get; set; } = false;
        [Parameter] public EventCallback<Dictionary<string, bool>> OnChanged { get; set; }

        // === Сервіси ===
        [Inject] public StateService? StateService { get; set; }

        // === Внутрішній стан ===
        private bool IsOpen { get; set; } = false;

        // === Життєвий цикл ===
        protected override void OnInitialized()
        {
            IsOpen = DefaultOpen;
        }

        // === Перемикання відкриття списку ===
        private void ToggleOpen() => IsOpen = !IsOpen;

        // === Визначення стану виконання для чекбоксів ===
        private bool GetItemCompletedStatus(string item)
        {
            if (StateService == null) return false;

            return item switch
            {
                // Гемодинаміка
                "КПІ" => StateService.KpiStepCompleted,
                "ППІ" => StateService.PpiStepCompleted,

                // WiFI
                "Оцінка критерію W" => StateService.WStepCompleted,
                "Оцінка критерію I" => StateService.IsICompleted,
                "Оцінка критерію fI" => StateService.IsfICompleted,
                "Оцінка результатів" => StateService.IsWiFIResultsCompleted,

                // CRAB / 2YLE
                "Оцінка перипроцедуральної смертності" => StateService.IsCRABCompleted,
                "Оцінка дворічної виживаності" => StateService.Is2YLECompleted,
                "Кінцева оцінка ступеня хірургічного ризику" => StateService.IsSurgicalRiskCompleted,

                // GLASS
                "Визначення анатомічної стадії аорто-клубової хвороби за GLASS" => StateService.IsGLASSCompleted,
                "Визначення ступеня ураження стегново-підколінного сегмента" => StateService.IsGLASSFemoroPoplitealCompleted,
                "Визначення ступеня ураження інфрапоплітеального сегмента" => StateService.IsGLASSInfrapoplitealCompleted,
                "Остаточне визначення анатомічної стадії інфраінгвінальної хвороби за GLASS" => StateService.IsGLASSFinalCompleted,
                "Встановлення дескриптора підкісточкової (стопної) хвороби та формулювання діагнозу пацієнта" => StateService.IsGLASSFinalCompleted,

                _ => false
            };
        }

        // === Обробка зміни стану чекбокса (поки порожня) ===
        private async Task OnItemStatusChanged(string item, bool isChecked)
        {
            // Для майбутньої логіки: можна викликати OnChanged
            await Task.CompletedTask;
        }
    }
}
