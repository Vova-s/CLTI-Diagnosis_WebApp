using System.Linq;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Models
{
    public class InfectionData
    {
        // === Локальні ознаки інфекції ===
        public bool HasLocalSwelling { get; set; }
        public bool HasErythema { get; set; }
        public bool HasLocalPain { get; set; }
        public bool HasLocalWarmth { get; set; }
        public bool HasPus { get; set; }

        // === Системні ознаки інфекції (SIRS) ===
        public bool HasTachycardia { get; set; }
        public bool HasTachypnea { get; set; }
        public bool HasTemperatureChange { get; set; }
        public bool HasLeukocytosis { get; set; }

        // === Додаткові дані ===
        public string? SirsAbsentType { get; set; }
        public string? HyperemiaSize { get; set; }

        // === Обчислення наявності інфекції ===
        public bool HasTwoOrMoreInfectionSigns =>
            new[] { HasLocalSwelling, HasErythema, HasLocalPain, HasLocalWarmth, HasPus }
                .Count(sign => sign) >= 2;

        // === Скидання всіх параметрів ===
        public void Reset()
        {
            HasLocalSwelling = false;
            HasErythema = false;
            HasLocalPain = false;
            HasLocalWarmth = false;
            HasPus = false;

            HasTachycardia = false;
            HasTachypnea = false;
            HasTemperatureChange = false;
            HasLeukocytosis = false;

            SirsAbsentType = null;
            HyperemiaSize = null;
        }
    }
}
