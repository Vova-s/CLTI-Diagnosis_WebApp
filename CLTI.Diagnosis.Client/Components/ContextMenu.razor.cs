namespace CLTI.Diagnosis.Client.Components
{
    public partial class ContextMenu
    {
        private bool Hidden { get; set; } = true;
        private string menuTopPx = "0px";
        private string menuLeftPx = "0px";

        private string? targetName;

        public void Show(double x, double y, string name)
        {
            menuLeftPx = $"{x}px";
            menuTopPx = $"{y}px";
            targetName = name;
            Hidden = false;
            StateHasChanged();
        }

        public void Hide()
        {
            Hidden = true;
            StateHasChanged();
        }

        private void OnActionClick(string action)
        {
            Console.WriteLine($"Обрана дія: {action}");
            Hide();
        }

        private async Task OnView()
        {
            Console.WriteLine($"Переглянути: {targetName}");
            Hide();
        }

        private async Task OnEdit()
        {
            Console.WriteLine("Редагувати");
            Hide();
        }

        private async Task OnDelete()
        {
            Console.WriteLine("Видалити");
            Hide();
        }
    }
}
