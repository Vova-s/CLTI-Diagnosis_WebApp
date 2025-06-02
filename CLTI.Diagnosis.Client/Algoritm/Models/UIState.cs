namespace CLTI.Diagnosis.Client.Algoritm.Models
{
    public class UIState
    {
        public bool ShowKpiNormal { get; set; }
        public bool ShowKpiLow { get; set; }
        public bool ShowKpiHigh { get; set; }
        public bool ShowPpiField { get; set; }
        public bool ShowPpiLow { get; set; }
        public bool ShowPpiNormal { get; set; }
        public bool ShowPpiInSidebar { get; set; }
        public bool ShowWifiSection { get; set; }

        public bool ShowHemodynamicSection => true;

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