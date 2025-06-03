namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class RevascularizationMethod
    {
        private string selectedSegment = "";
        private bool highLimbRisk = false;
        private bool severeIschemia = false;

        protected override void OnInitialized()
        {
            StateService.OnChange += HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private async Task OnSegmentChanged(string segment, bool isSelected)
        {
            if (isSelected)
            {
                selectedSegment = segment;

                // Скидаємо додаткові параметри при зміні сегменту
                if (segment != "Combined")
                {
                    highLimbRisk = false;
                    severeIschemia = false;
                }

                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnHighLimbRiskChanged(bool isChecked)
        {
            highLimbRisk = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnSevereIschemiaChanged(bool isChecked)
        {
            severeIschemia = isChecked;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        private string GetSegmentDescription()
        {
            return selectedSegment switch
            {
                "AortoIliac" => "Обрано: Реваскуляризація аорто-клубового сегмента",
                "Infrainguinal" => "Обрано: Реваскуляризація при інфраінгвінальній хворобі",
                "Combined" => "Обрано: Комбіноване ураження - потребує комплексного підходу",
                _ => ""
            };
        }

        private string GetRecommendation()
        {
            return selectedSegment switch
            {
                "AortoIliac" => "Переходимо до вибору оптимального методу для аорто-клубового сегмента",
                "Infrainguinal" => "Переходимо до вибору оптимального методу для інфраінгвінальної хвороби",
                "Combined" => highLimbRisk && severeIschemia
                    ? "Рекомендується одночасна реваскуляризація припливу та відтоку"
                    : "Рекомендується поетапна реваскуляризація, починаючи з припливу",
                _ => ""
            };
        }

        private async Task Continue()
        {
            string nextPage = selectedSegment switch
            {
                "AortoIliac" => "/Algoritm/Pages/AortoIliacRevascularization",
                "Infrainguinal" => "/Algoritm/Pages/InfrainguinalRevascularization",
                "Combined" => "/Algoritm/Pages/CombinedRevascularization",
                _ => "/Algoritm/Pages/RevascularizationSummary"
            };

            NavigationManager.NavigateTo(nextPage, forceLoad: true);
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}
