using Microsoft.AspNetCore.Components;

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

            // Автоматично визначаємо ризики на основі існуючих даних
            AutoDetectRisks();
        }

        private void HandleStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private void AutoDetectRisks()
        {
            // Автоматично визначаємо високий ризик для кінцівок
            var clinicalStage = StateService.ClinicalStage;
            highLimbRisk = clinicalStage >= 3;

            // Автоматично визначаємо виражену ішемію
            var iLevel = StateService.ILevelValue ?? 0;
            severeIschemia = iLevel >= 2;
        }

        private async Task OnSegmentChanged(string segment, bool isSelected)
        {
            if (isSelected)
            {
                selectedSegment = segment;

                // Скидаємо додаткові параметри при зміні сегменту
                if (segment != "Combined")
                {
                    AutoDetectRisks(); // Повертаємо автовизначені значення
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

        private string GetCurrentWiFIResult()
        {
            var w = StateService.WLevelValue ?? 0;
            var i = StateService.ILevelValue ?? 0;
            var fi = StateService.FILevelValue ?? 0;
            return $"Класифікація WiFI: W{w}I{i}fI{fi} - Клінічна стадія {StateService.ClinicalStage}";
        }

        private string GetMethodState()
        {
            if (selectedSegment == "AortoIliac")
            {
                var glassStage = StateService.GLASSSelectedStage;
                return glassStage == "Stage I" ? "success" : "warning";
            }
            else if (selectedSegment == "Infrainguinal")
            {
                var glassFinalStage = StateService.GLASSFinalStage;
                return glassFinalStage switch
                {
                    "I" => "success",
                    "II" => "warning",
                    "III" => "error",
                    _ => "default"
                };
            }
            else if (selectedSegment == "Combined")
            {
                return highLimbRisk && severeIschemia ? "error" : "warning";
            }

            return "default";
        }

        private string GetMethodDescription()
        {
            if (selectedSegment == "AortoIliac")
            {
                var glassStage = StateService.GLASSSelectedStage;
                var glassSubStage = StateService.GLASSSubStage;

                if (glassStage == "Stage I" && glassSubStage == "Відсутній")
                {
                    return "GLASS IA - Ендоваскулярне втручання як метод першої лінії, при невдачі - відкрита операція";
                }
                else if (glassStage == "Stage I" && glassSubStage == "Присутній")
                {
                    return "GLASS IB - Гібридна операція як метод першої лінії, при невдачі - відкрита операція";
                }
                else if (glassStage == "Stage II" && glassSubStage == "Відсутній")
                {
                    return "GLASS IIA - Відкрита операція як метод першої лінії";
                }
                else if (glassStage == "Stage II" && glassSubStage == "Присутній")
                {
                    return "GLASS IIB - Відкрита операція як метод першої лінії";
                }
            }
            else if (selectedSegment == "Infrainguinal")
            {
                var glassFinalStage = StateService.GLASSFinalStage;
                var surgicalRisk = StateService.CalculatedSurgicalRisk;
                var clinicalStage = StateService.ClinicalStage;

                if (surgicalRisk == "Високий")
                {
                    if (glassFinalStage == "I")
                    {
                        return clinicalStage <= 2
                            ? "Високий хірургічний ризик - ендоваскулярне лікування при WiFI стадії 1-2"
                            : "Високий хірургічний ризик - операція не показана при WiFI стадії 3-4";
                    }
                    else if (glassFinalStage == "II")
                    {
                        return "Високий хірургічний ризик - тільки ендоваскулярне лікування при GLASS II";
                    }
                    else if (glassFinalStage == "III")
                    {
                        return "Високий хірургічний ризик - операція не показана при GLASS III";
                    }
                }
                else
                {
                    if (glassFinalStage == "I")
                    {
                        return clinicalStage <= 2
                            ? "Стандартний ризик - ендоваскулярне лікування при WiFI стадії 1-2 та GLASS I"
                            : "Стандартний ризик - відкрита операція при WiFI стадії 3-4 та GLASS I";
                    }
                    else if (glassFinalStage == "II")
                    {
                        return "Стандартний ризик - ендоваскулярне лікування при GLASS II";
                    }
                    else if (glassFinalStage == "III")
                    {
                        return "Стандартний ризик - відкрита операція при GLASS III";
                    }
                }
            }
            else if (selectedSegment == "Combined")
            {
                if (highLimbRisk && severeIschemia)
                {
                    return "Високий ризик для кінцівок (WiFI стадія 3-4) + виражена ішемія (I2-3) - одночасна корекція";
                }
                else
                {
                    var iLevel = StateService.ILevelValue ?? 0;
                    var wLevel = StateService.WLevelValue ?? 0;

                    if (iLevel == 1 && (wLevel == 0 || wLevel == 1))
                    {
                        return "Ішемія низького ступеня (I1) + обмежена втрата тканини (W0/1) - корекція лише припливу";
                    }
                    else
                    {
                        return "Поетапна реваскуляризація - першочергово приплив, потім повторна оцінка WiFI та GLASS";
                    }
                }
            }

            return "";
        }


        private string GetClinicalRecommendations()
        {
            var recommendations = new List<string>();

            // Загальні рекомендації на основі хірургічного ризику
            var surgicalRisk = StateService.CalculatedSurgicalRisk;
            if (surgicalRisk == "Високий")
            {
                recommendations.Add("Через високий хірургічний ризик віддавати перевагу менш інвазивним методам.");
            }

            // Рекомендації на основі клінічної стадії
            var clinicalStage = StateService.ClinicalStage;
            if (clinicalStage >= 4)
            {
                recommendations.Add("При високому ризику втрати кінцівки розглянути термінову реваскуляризацію.");
            }

            // Рекомендації на основі інфекції
            var fiLevel = StateService.FILevelValue ?? 0;
            if (fiLevel >= 2)
            {
                recommendations.Add("При наявності інфекції забезпечити адекватний контроль інфекції до/після реваскуляризації.");
            }

            // Рекомендації на основі ран
            var wLevel = StateService.WLevelValue ?? 0;
            if (wLevel >= 2)
            {
                recommendations.Add("При наявності некрозу/виразок забезпечити адекватний догляд за раною та розвантаження.");
            }

            if (selectedSegment == "Combined")
            {
                recommendations.Add("При комбінованому ураженні необхідна мультидисциплінарна команда (судинний хірург, інтервенціоналіст).");
            }

            return recommendations.Any()
                ? string.Join(" ", recommendations)
                : "Стандартні рекомендації щодо періопераційної підготовки та моніторингу.";
        }

    private async Task SaveAndExit()
        {
            StateService.IsRevascularizationMethodCompleted = true; // ДОДАНО
            StateService.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
            // Повертаємося на домашню сторінку
            NavigationManager.NavigateTo("/", forceLoad: true);
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChanged;
        }
    }
}