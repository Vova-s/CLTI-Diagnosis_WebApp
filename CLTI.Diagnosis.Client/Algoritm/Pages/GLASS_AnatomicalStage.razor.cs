using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class GLASS_AnatomicalStage
    {
        private string selectedStage = "";
        private string stenosisLevel = "";

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
                StateService.GLASSSelectedStage = stage;
                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnStenosisLevelChanged(string level, bool isSelected)
        {
            if (isSelected)
            {
                stenosisLevel = level;
                StateService.GLASSSubStage = level;
                StateService.NotifyStateChanged();
                await InvokeAsync(StateHasChanged);
            }
        }

        private string GetGLASSStageResult()
        {
            string subStageCode = stenosisLevel == "Відсутній" ? "A" : "B";
            return $"GLASS: {selectedStage.Replace("Stage", "")}{subStageCode}";
        }

        private string GetGLASSDescription()
        {
            return selectedStage switch
            {
                "Stage I" => "Легкі до помірних анатомічних уражень аорто-клубового сегменту",
                "Stage II" => "Складні анатомічні ураження аорто-клубового сегменту",
                _ => ""
            };
        }

        private string GetTreatmentRecommendation()
        {
            if (selectedStage == "Stage I")
            {
                return stenosisLevel == "Відсутній"
                    ? "Рекомендується ендоваскулярне лікування як метод першої лінії"
                    : "Розгляд хірургічного лікування через ураження загальної стегнової артерії";
            }
            else if (selectedStage == "Stage II")
            {
                return stenosisLevel == "Відсутній"
                    ? "Складні ураження - розгляд мультидисциплінарного підходу"
                    : "Високий ризик - необхідна ретельна оцінка хірургічних ризиків";
            }
            return "";
        }

        private async Task Continue()
        {
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }

            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/Algoritm/Pages/GLASS_FemoroPoplitealSegment", forceLoad: true);
            StateService.IsGLASSCompleted = true;
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}