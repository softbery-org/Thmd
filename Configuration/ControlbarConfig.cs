// Version: 0.0.0.1
namespace Thmd.Configuration
{
    public class ControlbarConfig
    {
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Width { get; set; } = 800;
        public double Height { get; set; } = 100;
        public bool IsVisible { get; set; } = true;
        public bool AutoHide { get; set; } = true;

        // Opcjonalnie: metody Load/Save – ale nie są potrzebne, bo Config zarządza tym centralnie
        // Jeśli chcesz mieć je dla wygody:
        public void Load()
        {
            var loaded = Config.Instance.ControlbarConfig; // Pobiera z singletona
            X = loaded.X;
            Y = loaded.Y;
            Width = loaded.Width;
            Height = loaded.Height;
            IsVisible = loaded.IsVisible;
            AutoHide = loaded.AutoHide;
        }

        public void Save()
        {
            Config.SaveConfig(Config.ControlbarConfigPath, this);
            // Opcjonalnie: odśwież instancję w Config.Instance
            Config.Instance.GetType().GetProperty(nameof(Config.ControlbarConfig))?
                .SetValue(Config.Instance, this);
        }
    }
}
