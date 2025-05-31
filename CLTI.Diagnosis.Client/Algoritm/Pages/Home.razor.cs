using CLTI.Diagnosis.Client.Shared;
using CLTI.Diagnosis.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Components.Pages
{
    public partial class Home : ComponentBase
    {
        public ContextMenu? contextMenuRef; // Made nullable
        public string? selectedName;
        public void OpenContextMenu(double x, double y, string name)
        {
            selectedName = name;
            contextMenuRef?.Show(x, y, name);
        }
    }
}