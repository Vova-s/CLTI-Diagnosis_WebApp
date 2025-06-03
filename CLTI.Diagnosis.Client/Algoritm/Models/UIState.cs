namespace CLTI.Diagnosis.Client.Algoritm.Models
{
    public class UIState
    {
        // === KPI (Кісточково-плечовий індекс) ===
        public bool ShowKpiNormal { get; set; }
        public bool ShowKpiLow { get; set; }
        public bool ShowKpiHigh { get; set; }

        // === PPI (Пальце-плечовий індекс) ===
        public bool ShowPpiField { get; set; }
        public bool ShowPpiLow { get; set; }
        public bool ShowPpiNormal { get; set; }
        public bool ShowPpiInSidebar { get; set; }

        // === Інші секції ===
        public bool ShowWifiSection { get; set; }

        // Постійно відображати гемодинамічний блок
        public bool ShowHemodynamicSection => true;

        // === Скидання всіх станів ===
        public void Reset()
        {
            ShowKpiNormal = false;
            ShowKpiLow = false;
            ShowKpiHigh = false;

            ShowPpiField = false;
            ShowPpiLow = false;
            ShowPpiNormal = false;
            ShowPpiInSidebar = false;

            ShowWifiSection = false;
        }
    }
}
