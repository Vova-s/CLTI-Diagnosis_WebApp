using CLTI.Diagnosis.Client.Features.Diagnosis.Models;


namespace CLTI.Diagnosis.Client.Features.Diagnosis.Services
{
    public class ILevelCalculator
    {
        private readonly VascularData _vascularData;

        public ILevelCalculator(VascularData vascularData)
        {
            _vascularData = vascularData;
        }

        public int? Calculate()
        {
            if (_vascularData.HasArterialCalcification && _vascularData.TcPO2Value.HasValue)
            {
                return _vascularData.TcPO2Value.Value switch
                {
                    >= 60 => 0,
                    >= 40 and < 60 => 1,
                    >= 30 and < 40 => 2,
                    < 30 => 3,
                    _ => null
                };
            }

            if (_vascularData.KpiValue <= 1.3)
            {
                return _vascularData.KpiValue switch
                {
                    >= 0.9 => 0,
                    >= 0.6 and < 0.9 => 1,
                    >= 0.4 and < 0.6 => 2,
                    < 0.4 and > 0 => 3,
                    _ => null
                };
            }

            if (_vascularData.KpiValue > 1.3)
            {
                if (string.IsNullOrEmpty(_vascularData.PsatValue))
                {
                    return null;
                }

                return _vascularData.PsatValue switch
                {
                    "≥60" => 0,
                    "40-59" => 1,
                    "30-39" => 2,
                    "<30" => 3,
                    _ => null
                };
            }

            return null;
        }
    }
}