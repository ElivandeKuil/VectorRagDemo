namespace VectorRagDemo.Models.Entities
{
    public class WidgetConfig
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }

        // Layout
        public string WidgetPosition { get; set; } = "bottom-right";   // bottom-right | bottom-left
        public int OffsetX { get; set; } = 24;
        public int OffsetY { get; set; } = 24;

        // Button
        public string ButtonColor { get; set; } = "#0d6efd";
        public int ButtonSize { get; set; } = 56;

        // Popup
        public int PopupWidth { get; set; } = 380;
        public int PopupHeight { get; set; } = 560;
        public int PopupBorderRadius { get; set; } = 12;

        // Header
        public string HeaderBgColor { get; set; } = "#0d6efd";
        public string HeaderTextColor { get; set; } = "#ffffff";

        // Chat bubbles
        public string UserBubbleBgColor { get; set; } = "#0d6efd";
        public string UserBubbleTextColor { get; set; } = "#ffffff";
        public string BotBubbleBgColor { get; set; } = "#f1f3f5";
        public string BotBubbleTextColor { get; set; } = "#212529";

        // Typography
        public string WidgetFontSize { get; set; } = "md";        // sm | md | lg

        // Content
        public string GreetingMessage { get; set; } = "Hallo! Hoe kan ik u helpen?";

        // Navigation
        public Project? Project { get; set; }
    }
}
