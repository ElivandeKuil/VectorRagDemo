using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.BLL
{
    /// <summary>
    /// Business logic for widget configuration: null-default coercion and property mapping.
    /// </summary>
    public static class WidgetConfigService
    {
        /// <summary>
        /// Ensures no string field on the config is null, applying sensible defaults.
        /// Call this before persisting a form-bound WidgetConfig.
        /// </summary>
        public static void ApplyNullDefaults(WidgetConfig config)
        {
            config.GreetingMessage    = config.GreetingMessage    ?? string.Empty;
            config.HeaderTitle        = config.HeaderTitle        ?? string.Empty;
            config.HeaderLogoUrl      = config.HeaderLogoUrl      ?? string.Empty;
            config.ButtonLogoUrl      = config.ButtonLogoUrl      ?? string.Empty;
            config.ButtonShape        = config.ButtonShape        ?? "circle";
            config.ButtonBorderColor  = config.ButtonBorderColor  ?? "#ffffff";
            config.HideSide           = config.HideSide           ?? "right";
            config.IntroMessage       = config.IntroMessage       ?? string.Empty;
            config.ButtonSvgIdle      = config.ButtonSvgIdle      ?? string.Empty;
            config.ButtonSvgPeek      = config.ButtonSvgPeek      ?? string.Empty;
            config.ButtonSvgOpen      = config.ButtonSvgOpen      ?? string.Empty;
            config.WhatsAppNumber     = config.WhatsAppNumber     ?? string.Empty;
            config.WhatsAppButtonText = config.WhatsAppButtonText ?? "Chat via WhatsApp";
            config.WhatsAppCtaText    = config.WhatsAppCtaText    ?? "Neem gelijk contact op met mijn collega";
            config.EmailAddress       = config.EmailAddress       ?? string.Empty;
            config.EmailSubject       = config.EmailSubject       ?? "Vraag via chat";
            config.EmailButtonText    = config.EmailButtonText    ?? "Stuur een e-mail";
            config.EmailCtaText       = config.EmailCtaText       ?? "Neem gelijk contact op met mijn collega";
        }

        /// <summary>
        /// Copies all editable fields from <paramref name="updates"/> onto <paramref name="existing"/>.
        /// Clamping is applied to numeric range fields.
        /// </summary>
        public static void ApplyUpdates(WidgetConfig existing, WidgetConfig updates)
        {
            existing.WidgetPosition      = updates.WidgetPosition;
            existing.OffsetX             = updates.OffsetX;
            existing.OffsetY             = updates.OffsetY;
            existing.ButtonColor         = updates.ButtonColor;
            existing.ButtonSize          = updates.ButtonSize;
            existing.ButtonShape         = updates.ButtonShape;
            existing.ButtonBorderWidth   = Math.Clamp(updates.ButtonBorderWidth, 0, 8);
            existing.ButtonBorderColor   = updates.ButtonBorderColor;
            existing.ButtonIconPadding   = Math.Clamp(updates.ButtonIconPadding, 0, 30);
            existing.PopupWidth          = updates.PopupWidth;
            existing.PopupHeight         = updates.PopupHeight;
            existing.PopupBorderRadius   = updates.PopupBorderRadius;
            existing.HeaderBgColor       = updates.HeaderBgColor;
            existing.HeaderTextColor     = updates.HeaderTextColor;
            existing.UserBubbleBgColor   = updates.UserBubbleBgColor;
            existing.UserBubbleTextColor = updates.UserBubbleTextColor;
            existing.BotBubbleBgColor    = updates.BotBubbleBgColor;
            existing.BotBubbleTextColor  = updates.BotBubbleTextColor;
            existing.WidgetFontSize      = updates.WidgetFontSize;
            existing.GreetingMessage     = updates.GreetingMessage;
            existing.HeaderTitle         = updates.HeaderTitle;
            existing.HeaderLogoUrl       = updates.HeaderLogoUrl;
            existing.ChatBodyBgColor     = updates.ChatBodyBgColor;
            existing.InputAreaBgColor    = updates.InputAreaBgColor;
            existing.SendButtonColor     = updates.SendButtonColor;
            existing.SendButtonIconColor = updates.SendButtonIconColor;
            existing.ButtonLogoUrl       = updates.ButtonLogoUrl;
            existing.IconHideable        = updates.IconHideable;
            existing.HideSide            = updates.HideSide;
            existing.PeekAmount          = Math.Clamp(updates.PeekAmount, 20, 70);
            existing.ButtonSvgIdle       = updates.ButtonSvgIdle;
            existing.ButtonSvgPeek       = updates.ButtonSvgPeek;
            existing.ButtonSvgOpen       = updates.ButtonSvgOpen;
            existing.IntroMessage        = updates.IntroMessage;
            existing.IntroMessageDelay   = Math.Clamp(updates.IntroMessageDelay, 0, 30);
            existing.WhatsAppEnabled     = updates.WhatsAppEnabled;
            existing.WhatsAppNumber      = updates.WhatsAppNumber;
            existing.WhatsAppButtonText  = updates.WhatsAppButtonText;
            existing.WhatsAppCtaText     = updates.WhatsAppCtaText;
            existing.EmailEnabled        = updates.EmailEnabled;
            existing.EmailAddress        = updates.EmailAddress;
            existing.EmailSubject        = updates.EmailSubject;
            existing.EmailButtonText     = updates.EmailButtonText;
            existing.EmailCtaText        = updates.EmailCtaText;
        }
    }
}
