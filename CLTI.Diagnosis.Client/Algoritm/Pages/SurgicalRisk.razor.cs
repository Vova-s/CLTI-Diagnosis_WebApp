using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class SurgicalRisk
    {
        protected override void OnInitialized()
        {
            StateService.OnChange += HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private string GetCRABRiskText()
        {
            var crabScore = StateService.CRABTotalScore ?? 0;
            var mortalityRisk = crabScore >= 7 ? ">5%" : "<5%";

            return $"Періпроцедуральна смертність: {mortalityRisk} (бали ≥7 = >5%)";
        }

        private string GetCRABRiskState()
        {
            var crabScore = StateService.CRABTotalScore ?? 0;
            return crabScore >= 7 ? "error" : "success";
        }

        private string Get2YLERiskText()
        {
            var yleScore = StateService.YLETotalScore ?? 0.0;
            var survivalRate = yleScore >= 8.0 ? "<50%" : ">50%";

            return $"Дворічна виживаність: {survivalRate} (бали ≥8 = <50%)";
        }

        private string Get2YLERiskState()
        {
            var yleScore = StateService.YLETotalScore ?? 0.0;
            return yleScore >= 8.0 ? "error" : "success";
        }

        private string GetSurgicalRiskText()
        {
            var riskLevel = StateService.CalculatedSurgicalRisk;

            return riskLevel switch
            {
                "Помірний" => "Хірургічний ризик: ПОМІРНИЙ",
                "Високий" => "Хірургічний ризик: ВИСОКИЙ",
                _ => "Хірургічний ризик: НЕВИЗНАЧЕНИЙ"
            };
        }

        private string GetSurgicalRiskState()
        {
            var riskLevel = StateService.CalculatedSurgicalRisk;

            return riskLevel switch
            {
                "Помірний" => "warning",
                "Високий" => "error",
                _ => "default"
            };
        }

        private string GetSurgicalRiskDescription()
        {
            var riskLevel = StateService.CalculatedSurgicalRisk;

            return riskLevel switch
            {
                "Помірний" => "Очікувана перипроцедуральна смертність <5% та дворічна виживаність >50%",
                "Високий" => "Очікувана перипроцедуральна смертність >5% або дворічна виживаність <50%",
                _ => "Неможливо визначити ризик"
            };
        }

        private string GetClinicalRecommendations()
        {
            var riskLevel = StateService.CalculatedSurgicalRisk;
            var crabScore = StateService.CRABTotalScore ?? 0;
            var yleScore = StateService.YLETotalScore ?? 0.0;

            if (riskLevel == "Помірний")
            {
                return "Хірургічне втручання може бути виконано з прийнятним ризиком. Рекомендується стандартна підготовка до операції та моніторинг.";
            }
            else if (riskLevel == "Високий")
            {
                var reasons = new List<string>();
                if (crabScore >= 7) reasons.Add("високий ризик періпроцедуральної смертності");
                if (yleScore >= 8.0) reasons.Add("низька дворічна виживаність");

                var reasonText = string.Join(" та ", reasons);
                return $"Підвищений хірургічний ризик через {reasonText}. Необхідна ретельна оцінка співвідношення ризик/користь, розгляд альтернативних методів лікування та посилена періопераційна підтримка.";
            }
            else
            {
                return "Неможливо надати специфічні рекомендації. Рекомендується індивідуальна оцінка пацієнта.";
            }
        }

        private async Task Continue()
        {
            StateService.IsSurgicalRiskCompleted = true;
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
            NavigationManager.NavigateTo("/", forceLoad: true);

        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}