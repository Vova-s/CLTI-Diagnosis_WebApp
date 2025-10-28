using CLTI.Diagnosis.Client.Features.Diagnosis.Models;


namespace CLTI.Diagnosis.Client.Features.Diagnosis.Services
{
    public class FILevelCalculator
    {
        private readonly InfectionData _infectionData;

        public FILevelCalculator(InfectionData infectionData)
        {
            _infectionData = infectionData;
        }

        public int? Calculate()
        {
            int infectionSignsCount = GetInfectionSignsCount();

            if (infectionSignsCount <= 1)
            {
                return 0;
            }

            int smallSigns = GetSmallSirsSignsCount();
            int largeSigns = GetLargeSirsSignsCount();

            // SIRS наявний: ≥2 ознаки, з них ≥1 велика
            if ((smallSigns + largeSigns) >= 2 && largeSigns >= 1)
            {
                return 3;
            }

            // SIRS відсутній або тільки 2 малі ознаки без великих
            if ((smallSigns + largeSigns) <= 1 || (smallSigns == 2 && largeSigns == 0))
            {
                return CalculateFIForAbsentSirs();
            }

            return null;
        }

        public bool HasSirs()
        {
            int small = GetSmallSirsSignsCount();
            int large = GetLargeSirsSignsCount();
            return (small + large >= 2) && (large >= 1);
        }

        public bool ShouldShowHyperemiaField()
        {
            return _infectionData.HasTwoOrMoreInfectionSigns && !HasSirs() &&
                   !string.IsNullOrEmpty(_infectionData.SirsAbsentType) &&
                   _infectionData.SirsAbsentType == "Шкіра";
        }

        public bool ShouldShowSirsAbsentSection()
        {
            return _infectionData.HasTwoOrMoreInfectionSigns && !HasSirs();
        }

        private int GetInfectionSignsCount()
        {
            int count = 0;
            if (_infectionData.HasLocalSwelling) count++;
            if (_infectionData.HasErythema) count++;
            if (_infectionData.HasLocalPain) count++;
            if (_infectionData.HasLocalWarmth) count++;
            if (_infectionData.HasPus) count++;
            return count;
        }

        private int GetSmallSirsSignsCount()
        {
            int count = 0;
            if (_infectionData.HasTachycardia) count++;
            if (_infectionData.HasTachypnea) count++;
            return count;
        }

        private int GetLargeSirsSignsCount()
        {
            int count = 0;
            if (_infectionData.HasTemperatureChange) count++;
            if (_infectionData.HasLeukocytosis) count++;
            return count;
        }

        private int? CalculateFIForAbsentSirs()
        {
            if (_infectionData.SirsAbsentType == "Шкіра")
            {
                if (string.IsNullOrEmpty(_infectionData.HyperemiaSize)) return null;

                return _infectionData.HyperemiaSize switch
                {
                    "0.5-2" => 1,
                    ">2" => 2,
                    _ => null
                };
            }
            else if (_infectionData.SirsAbsentType == "Кістки")
            {
                return 2;
            }

            return null;
        }
    }
}
