using CLTI.Diagnosis.Client.Algoritm.Services;
using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class GLASS_FemoroPoplitealSegment
    {
        private string selectedStage = "";

        protected override void OnInitialized()
        {
            StateService.OnChange += HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private async Task OnStageChanged(string stage, bool isSelected)
        {
            if (isSelected)
            {
                selectedStage = stage;
                StateService.GLASSFemoroPoplitealStage = stage;
                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        private string GetGLASSStageResult()
        {
            return selectedStage switch
            {
                "Stage0" => "GLASS стегново-підколінний сегмент: Ступінь 0",
                "Stage1" => "GLASS стегново-підколінний сегмент: Ступінь 1",
                "Stage2" => "GLASS стегново-підколінний сегмент: Ступінь 2",
                "Stage3" => "GLASS стегново-підколінний сегмент: Ступінь 3",
                "Stage4" => "GLASS стегново-підколінний сегмент: Ступінь 4",
                _ => ""
            };
        }

        private string GetStageDescription()
        {
            return selectedStage switch
            {
                "Stage0" => "Відсутність значущих стенозів у стегново-підколінному сегменті",
                "Stage1" => "Легкі ураження стегново-підколінного сегменту з обмеженою протяжністю",
                "Stage2" => "Помірні ураження стегново-підколінного сегменту з можливим залученням підколінної артерії",
                "Stage3" => "Значні ураження стегново-підколінного сегменту з поширеним ураженням поверхневої стегнової артерії",
                "Stage4" => "Тяжкі ураження стегново-підколінного сегменту з протяжними оклюзіями",
                _ => ""
            };
        }

        private string GetTreatmentRecommendation()
        {
            return selectedStage switch
            {
                "Stage0" => "Консервативне лікування або спостереження",
                "Stage1" => "Ендоваскулярне лікування як метод першої лінії - балонна ангіопластика ± стентування",
                "Stage2" => "Ендоваскулярне лікування з можливим використанням DEB або стентів з лікарським покриттям",
                "Stage3" => "Складне ендоваскулярне лікування або розгляд хірургічної реваскуляризації",
                "Stage4" => "Хірургічна реваскуляризація (шунтування) або складні ендоваскулярні процедури",
                _ => ""
            };
        }

        private async Task Continue()
        {
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);

            // Тут можна перейти до наступної сторінки або завершити оцінку
            NavigationManager.NavigateTo("/", forceLoad: true);
            StateService.IsGLASSFemoroPoplitealCompleted = true;
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}