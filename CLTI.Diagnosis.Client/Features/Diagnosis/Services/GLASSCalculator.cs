using CLTI.Diagnosis.Client.Features.Diagnosis.Models;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Services
{
    public class GLASSCalculator
    {
        private readonly GLASSData _glassData;

        public GLASSCalculator(GLASSData glassData)
        {
            _glassData = glassData;
        }

        public int? CalculateStage()
        {
            return _glassData.CalculateStage();
        }

        public string GetStageDescription()
        {
            return _glassData.GetStageDescription();
        }

        public string GetDetailedDescription()
        {
            var stage = CalculateStage();

            return stage switch
            {
                1 => "Стадія I включає стенози загальної та/або зовнішньої клубової артерії, " +
                     "хронічну повну оклюзію загальної або зовнішньої клубової артерії (але не обох артерій), " +
                     "стенози інфраренальної аорти, або будь-яку комбінацію перших трьох пунктів.",

                2 => "Стадія II включає хронічну повну оклюзію аорти, хронічну повну оклюзію загальної " +
                     "та зовнішньої клубових артерій, тяжке дифузне ураження та/або малкоїльні арт. в аорто-клубовому сегменті, " +
                     "або виражений дифузний рестеноз стента в аорто-клубовому сегменті.",

                _ => "Стадія не визначена. Потрібно вибрати відповідні критерії для класифікації."
            };
        }

        public string GetTreatmentRecommendation()
        {
            var stage = CalculateStage();

            return stage switch
            {
                1 => "Рекомендується ендоваскулярне лікування (балонна ангіопластика, стентування)",
                2 => "Може знадобитися хірургічне лікування або складні ендоваскулярні процедури",
                _ => "Для визначення тактики лікування необхідно встановити стадію захворювання"
            };
        }
    }
}