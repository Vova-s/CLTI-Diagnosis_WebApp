// GLASS_FemoroPoplitealSegment.razor.cs
using CLTI.Diagnosis.Client.Algoritm.Services;
using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class GLASS_FemoroPoplitealSegment
    {
        private bool hasSevereCalcification = false;
        private string selectedStage = "";
        private string adjustedStage = "";

        [Inject]
        public CLTI.Diagnosis.Services.CltiCaseService? CaseService { get; set; }

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
                AdjustStageWithCalcification();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnCalcificationChanged(bool value)
        {
            hasSevereCalcification = value;
            AdjustStageWithCalcification();
            await InvokeAsync(StateHasChanged);
        }

        private void AdjustStageWithCalcification()
        {
            int baseStage = selectedStage switch
            {
                "Stage0" => 0,
                "Stage1" => 1,
                "Stage2" => 2,
                "Stage3" => 3,
                "Stage4" => 4,
                _ => -1
            };

            int adjusted = (hasSevereCalcification && baseStage >= 0 && baseStage < 4)
                ? baseStage + 1
                : baseStage;

            adjustedStage = $"Stage{adjusted}";
            StateService.GLASSFemoroPoplitealStage = adjustedStage;
            StateService.NotifyStateChanged();
        }

        private string GetGLASSStageResult()
        {
            return adjustedStage switch
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
            return adjustedStage switch
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
            return adjustedStage switch
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
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }

            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/GLASS_InfrapoplitealSegment", forceLoad: true);
            StateService.IsGLASSFemoroPoplitealCompleted = true;
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}
