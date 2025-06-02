using CLTI.Diagnosis.Client.Algoritm.Models;

namespace CLTI.Diagnosis.Client.Algoritm.Steps
{
    public class WLevelCalculator
    {
        private readonly WoundData _woundData;

        public WLevelCalculator(WoundData woundData)
        {
            _woundData = woundData;
        }

        public int? Calculate()
        {
            if (!_woundData.HasNecrosis) return 0;

            if (_woundData.NecrosisType == "Гангрена")
            {
                return _woundData.GangreneSpread switch
                {
                    "Поширюється на плесно" => 3,
                    "Поширюється лише на пальці" => 2,
                    _ => null
                };
            }

            if (_woundData.NecrosisType == "Виразка")
            {
                if (_woundData.UlcerLocation == "На п'яті")
                {
                    return _woundData.UlcerDepth switch
                    {
                        "До кістки" => 3,
                        "Не до кістки" => 2,
                        _ => null
                    };
                }

                if (_woundData.UlcerLocation == "Не на п'яті")
                {
                    if (_woundData.UlcerAffectsBone == "Не захоплює")
                    {
                        return 1;
                    }

                    if (_woundData.UlcerAffectsBone == "Захоплює")
                    {
                        return _woundData.UlcerLocation2 switch
                        {
                            "Плесна і передплесна" => 3,
                            "Дистальних фаланг" => 2,
                            _ => null
                        };
                    }
                }
            }

            return null;
        }
    }
}