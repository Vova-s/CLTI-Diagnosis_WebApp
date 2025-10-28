using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Pages
{
    public partial class WiFI_W
    {
        [Inject]
        public CLTI.Diagnosis.Client.Features.Diagnosis.Services.CltiCaseService? CaseService { get; set; }
        // Списки для DropDown компонентів
        private List<string> necrosisTypeItems = new() { "Гангрена", "Виразка" };
        private List<string> gangreneSpreadItems = new() { "Поширюється на плесно", "Поширюється лише на пальці" };
        private List<string> ulcerLocationItems = new() { "На п'яті", "Не на п'яті" };

        // Додаткові поля для виразки відповідно до правильного алгоритму
        private string ulcerAffectsBone = "";
        private string ulcerDepth = "";
        private string ulcerLocation2 = "";

        private async Task OnNecrosisChanged(bool hasNecrosis, bool isSelected)
        {
            if (isSelected)
            {
                StateService.HasNecrosis = hasNecrosis;
                if (!hasNecrosis)
                {
                    // Скидаємо всі налаштування некрозу
                    StateService.NecrosisType = null;
                    StateService.GangreneSpread = null;
                    StateService.UlcerLocation = null;
                    StateService.UlcerAffectsBone = null;
                    StateService.UlcerDepth = null;
                    StateService.UlcerLocation2 = null;
                    ulcerAffectsBone = "";
                    ulcerDepth = "";
                    ulcerLocation2 = "";
                }
                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnNecrosisTypeSelect(string selectedType)
        {
            StateService.NecrosisType = selectedType;
            // Скидаємо інші налаштування при зміні типу
            StateService.GangreneSpread = null;
            StateService.UlcerLocation = null;
            StateService.UlcerAffectsBone = null;
            StateService.UlcerDepth = null;
            StateService.UlcerLocation2 = null;
            ulcerAffectsBone = "";
            ulcerDepth = "";
            ulcerLocation2 = "";
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnGangreneSpreadSelect(string selectedSpread)
        {
            StateService.GangreneSpread = selectedSpread;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnUlcerLocationSelect(string selectedLocation)
        {
            StateService.UlcerLocation = selectedLocation;
            StateService.UlcerAffectsBone = null;
            StateService.UlcerDepth = null;
            StateService.UlcerLocation2 = null;
            ulcerAffectsBone = "";
            ulcerDepth = "";
            ulcerLocation2 = "";
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnUlcerAffectsBoneChanged(string affects, bool isSelected)
        {
            if (isSelected)
            {
                ulcerAffectsBone = affects;
                StateService.UlcerAffectsBone = affects;
                StateService.UlcerDepth = null;
                StateService.UlcerLocation2 = null;
                ulcerDepth = "";
                ulcerLocation2 = "";
                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnUlcerDepthChanged(string depth, bool isSelected)
        {
            if (isSelected)
            {
                ulcerDepth = depth;
                StateService.UlcerDepth = depth;
                StateService.UlcerLocation2 = null;
                ulcerLocation2 = "";
                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnUlcerLocation2Changed(string location, bool isSelected)
        {
            if (isSelected)
            {
                ulcerLocation2 = location;
                StateService.UlcerLocation2 = location;
                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task Continue()
        {
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/Wifi_I", forceLoad: true);
            StateService.IsWCompleted = true;
        }
    }
}