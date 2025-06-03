namespace CLTI.Diagnosis.Client.Algoritm.Models
{
    public class WoundData
    {
        // === Некроз / Гангрена ===
        public bool HasNecrosis { get; set; }
        public string? NecrosisType { get; set; }
        public string? GangreneSpread { get; set; }

        // === Виразка ===
        public string? UlcerLocation { get; set; }
        public string? UlcerAffectsBone { get; set; }
        public string? UlcerDepth { get; set; }
        public string? UlcerLocation2 { get; set; }

        // === Скидання всіх значень ===
        public void Reset()
        {
            HasNecrosis = false;
            NecrosisType = null;
            GangreneSpread = null;

            UlcerLocation = null;
            UlcerAffectsBone = null;
            UlcerDepth = null;
            UlcerLocation2 = null;
        }
    }
}
