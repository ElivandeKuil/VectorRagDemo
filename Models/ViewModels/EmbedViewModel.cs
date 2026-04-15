using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.ViewModels
{
    public class EmbedViewModel
    {
        public string BotName { get; set; } = "Assistant";
        public int ProjectId { get; set; }
        public string EmbedKey { get; set; } = string.Empty;
        public WidgetConfig WidgetConfig { get; set; } = new();
        public bool StreamingEnabled { get; set; }

        public string FontSizeCss => WidgetConfig.WidgetFontSize == "sm" ? "13px" :
                                     WidgetConfig.WidgetFontSize == "lg" ? "16px" : "14px";
    }
}
