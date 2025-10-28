
using CLTI.Diagnosis.Client.Infrastructure.Auth;
using CLTI.Diagnosis.Client.Infrastructure.Http;
using CLTI.Diagnosis.Client.Infrastructure.State;
using CLTI.Diagnosis.Client.Features.Diagnosis.Services;
using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Pages
{
    public partial class GLASS_InfrapoplitealSegment
    {
        private bool hasSevereCalcification = false;
        private string selectedStage = "";
        private string adjustedStage = "";

        [Inject]
        public CLTI.Diagnosis.Client.Features.Diagnosis.Services.CltiCaseService? CaseService { get; set; }

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
            StateService.GLASSInfrapoplitealStage = adjustedStage;
            StateService.NotifyStateChanged();
        }

        private string GetGLASSStageResult()
        {
            return adjustedStage switch
            {
                "Stage0" => "GLASS інфраподколінний сегмент: Ступінь 0",
                "Stage1" => "GLASS інфраподколінний сегмент: Ступінь 1",
                "Stage2" => "GLASS інфраподколінний сегмент: Ступінь 2",
                "Stage3" => "GLASS інфраподколінний сегмент: Ступінь 3",
                "Stage4" => "GLASS інфраподколінний сегмент: Ступінь 4",
                _ => ""
            };
        }

        private string GetStageDescription()
        {
            return adjustedStage switch
            {
                "Stage0" => "Відсутність значущих стенозів в інфраподколінному сегменті",
                "Stage1" => "Мінімальні ураження з локальним стенозом",
                "Stage2" => "Помірні ураження з обмеженою протяжністю",
                "Stage3" => "Значні ураження з поширеним стенозом",
                "Stage4" => "Тяжкі ураження з дифузними стенозами або оклюзіями",
                _ => ""
            };
        }

        private string GetTreatmentRecommendation()
        {
            return adjustedStage switch
            {
                "Stage0" => "Консервативне лікування або спостереження",
                "Stage1" => "Ендоваскулярне лікування - балонна ангіопластика",
                "Stage2" => "Ендоваскулярне лікування з використанням DEB або спец. балонів",
                "Stage3" => "Складне ендоваскулярне лікування або дистальна реваскуляризація",
                "Stage4" => "Дистальна хірургічна реваскуляризація або складні процедури",
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
            NavigationManager.NavigateTo("/Algoritm/Pages/GLASS_FinalStage", forceLoad: true);
            StateService.IsGLASSInfrapoplitealCompleted = true;
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}
