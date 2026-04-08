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

        // Hideable icon behaviour
        /// <summary>When true the icon peeks from the edge of the screen and only slides to its full position on hover.</summary>
        public bool IconHideable { get; set; } = false;
        /// <summary>The screen edge the icon slides behind when hidden. "left" or "right".</summary>
        public string HideSide { get; set; } = "right";
        /// <summary>How much of the button (%) remains visible in the peek/hidden state. 20–70.</summary>
        public int PeekAmount { get; set; } = 50;

        // Button
        public string ButtonColor { get; set; } = "#0d6efd";
        public int ButtonSize { get; set; } = 56;

        // Per-state button icon SVGs (inline SVG markup, may contain CSS animations).
        // Falls back: state SVG → ButtonLogoUrl image → built-in chat bubble.
        // Peek/Open fall back to Idle when not set.
        public string ButtonSvgIdle { get; set; } = "";
        public string ButtonSvgPeek { get; set; } = "";
        public string ButtonSvgOpen { get; set; } = "";

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

        // Header extras
        public string HeaderTitle { get; set; } = "";       // overrides BotName in header when set
        public string HeaderLogoUrl { get; set; } = "";     // URL of logo image shown in header

        // Chat area
        public string ChatBodyBgColor { get; set; } = "#ffffff";

        // Input area
        public string InputAreaBgColor { get; set; } = "#ffffff";
        public string SendButtonColor { get; set; } = "#0d6efd";
        public string SendButtonIconColor { get; set; } = "#ffffff";

        // Button extras
        public string ButtonLogoUrl { get; set; } = "";     // URL of image shown in floating button instead of default icon

        // Extra communication (escalation buttons)
        /// <summary>
        /// Admin-only master switch. When false the bot never shows any escalation button,
        /// regardless of the individual channel settings below.
        /// </summary>
        public bool ExtraCommunicationEnabled { get; set; } = false;

        // WhatsApp channel
        public bool WhatsAppEnabled { get; set; } = false;
        /// <summary>International number without leading + or spaces, e.g. "31612345678".</summary>
        public string WhatsAppNumber { get; set; } = string.Empty;
        public string WhatsAppButtonText { get; set; } = "Chat via WhatsApp";
        public string WhatsAppCtaText { get; set; } = "Neem gelijk contact op met mijn collega";

        // Email channel
        public bool EmailEnabled { get; set; } = false;
        public string EmailAddress { get; set; } = string.Empty;
        /// <summary>Optional subject line pre-filled in the mailto: link.</summary>
        public string EmailSubject { get; set; } = "Vraag via chat";
        public string EmailButtonText { get; set; } = "Stuur een e-mail";
        public string EmailCtaText { get; set; } = "Neem gelijk contact op met mijn collega";

        // Navigation
        public Project? Project { get; set; }
    }
}
