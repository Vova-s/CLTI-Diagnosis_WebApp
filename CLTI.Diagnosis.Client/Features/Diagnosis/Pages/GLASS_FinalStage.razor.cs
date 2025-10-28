using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Pages
{
    public partial class GLASS_FinalStage
    {
        private int femoroPoplitealStage = 0;
        private int infrapoplitealStage = 0;
        private string finalStage = "";

        [Inject]
        public CLTI.Diagnosis.Client.Features.Diagnosis.Services.CltiCaseService? CaseService { get; set; }

        protected override void OnInitialized()
        {
            StateService.OnChange += HandleStateChanged;

            // Отримуємо стадії з StateService
            femoroPoplitealStage = GetStageNumber(StateService.GLASSFemoroPoplitealStage);
            infrapoplitealStage = GetStageNumber(StateService.GLASSInfrapoplitealStage);

            // Визначаємо остаточну стадію
            finalStage = CalculateFinalStage();
        }

        private void HandleStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private int GetStageNumber(string? stageString)
        {
            if (string.IsNullOrEmpty(stageString))
                return 0;

            // Витягуємо число зі строки типу "Stage2"
            if (stageString.StartsWith("Stage") && int.TryParse(stageString.Substring(5), out int stage))
            {
                return stage;
            }

            return 0;
        }

        private string CalculateFinalStage()
        {
            // Логіка згідно з таблицею GLASS
            return (femoroPoplitealStage, infrapoplitealStage) switch
            {
                // Ряд 0
                (0, 0) => "–", // Немає ураження
                (0, 1) => "I",
                (0, 2) => "I",
                (0, 3) => "II",
                (0, 4) => "III",

                // Ряд 1
                (1, 0) => "I",
                (1, 1) => "I",
                (1, 2) => "II",
                (1, 3) => "II",
                (1, 4) => "III",

                // Ряд 2
                (2, 0) => "I",
                (2, 1) => "II",
                (2, 2) => "II",
                (2, 3) => "II",
                (2, 4) => "III",

                // Ряд 3
                (3, 0) => "II",
                (3, 1) => "II",
                (3, 2) => "II",
                (3, 3) => "III",
                (3, 4) => "III",

                // Ряд 4
                (4, 0) => "III",
                (4, 1) => "III",
                (4, 2) => "III",
                (4, 3) => "III",
                (4, 4) => "III",

                _ => "–"
            };
        }

        private string GetFemoroPoplitealResult()
        {
            return $"GLASS стегново-підколінний сегмент: Ступінь {femoroPoplitealStage}";
        }

        private string GetInfrapoplitealResult()
        {
            return $"GLASS інфрапоплітеальний сегмент: Ступінь {infrapoplitealStage}";
        }

        private bool IsCurrentCombination(int femoro, int infra)
        {
            return femoroPoplitealStage == femoro && infrapoplitealStage == infra;
        }

        private string GetFinalStageResult()
        {
            if (finalStage == "–")
                return "Анатомічна стадія інфраінгвінальної хвороби за GLASS: Відсутня";

            return $"Анатомічна стадія інфраінгвінальної хвороби за GLASS: Стадія {finalStage}";
        }

        private string GetFinalStageState()
        {
            return finalStage switch
            {
                "–" => "success",
                "I" => "success",
                "II" => "warning",
                "III" => "error",
                _ => "default"
            };
        }

        private string GetStageDescription()
        {
            return finalStage switch
            {
                "–" => "Відсутні значущі анатомічні ураження інфраінгвінального сегменту",
                "I" => "Анатомічна стадія I - мінімальні до помірних ураження інфраінгвінального сегменту",
                "II" => "Анатомічна стадія II - помірні до значних ураження інфраінгвінального сегменту",
                "III" => "Анатомічна стадія III - значні до тяжких ураження інфраінгвінального сегменту",
                _ => "Не вдалося визначити стадію"
            };
        }

        private string GetTechnicalFailureRate()
        {
            var rate = finalStage switch
            {
                "I" => "<10%",
                "II" => "10-20%",
                "III" => ">20%",
                _ => "Не визначено"
            };

            return $"Імовірність технічного неуспіху реваскуляризації: {rate}";
        }

        private string GetFunctionalityRate()
        {
            var rate = finalStage switch
            {
                "I" => ">70%",
                "II" => "50-70%",
                "III" => "<50%",
                _ => "Не визначено"
            };

            return $"Відсоток функціонування зони реконструкції впродовж 1 року: {rate}";
        }

        private async Task Continue()
        {
            if (CaseService != null)
            {
                await CaseService.SaveCaseAsync(StateService);
            }
            // Зберігаємо остаточну стадію в StateService
            StateService.GLASSFinalStage = finalStage;
            StateService.IsGLASSFinalCompleted = true;
            StateService.NotifyStateChanged();

            await InvokeAsync(StateHasChanged);
            // Змінюємо навігацію на нову сторінку
            NavigationManager.NavigateTo("/Algoritm/Pages/CLTI_SubmalleolarDisease", forceLoad: true);
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}