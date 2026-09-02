namespace QueOnline.Api.Models;

public sealed class AppSettingsRecord
{
    public string HospitalName { get; set; } = "โรงพยาบาลวิภาราม";
    public string Tagline { get; set; } = "ระบบติดตามคิวออนไลน์";
    public string Greeting { get; set; } = "สวัสดีค่ะ ยินดีต้อนรับ";
    public string ArrivedTitle { get; set; } = "ถึงคิวของท่านแล้ว";
    public string ArrivedBody { get; set; } = "กรุณาเข้ารับบริการ";
    /// <summary>Navy "กรมท่า" — color-picked directly from the HiNav reference screenshot
    /// (176636.jpg) rather than guessed; the reference's navy is a much darker, less saturated
    /// blue than a standard royal blue.</summary>
    public string PrimaryColor { get; set; } = "#102a4d";

    /// <summary>Second accent color for the "hero" elements that should visually pop against
    /// PrimaryColor — the giant queue number and the wait-time estimate (mirrors the
    /// navy-structure / orange-highlight two-tone pattern from the HiNav reference design).
    /// Color-picked from the reference — a cleaner, less red orange than the previous guess.</summary>
    public string AccentColor { get; set; } = "#e8720f";

    public string FontFamily { get; set; } = "Anuphan";
    public string FontSizeScale { get; set; } = "medium"; // small | medium | large

    /// <summary>One of the presets in client/src/composables/useQueueAlert.js SOUND_PRESETS, or "custom" to play CustomSoundUrl.</summary>
    public string SoundChoice { get; set; } = "soft";

    public string? LogoUrl { get; set; }
    public string? CustomSoundUrl { get; set; }

    /// <summary>Opaque JSON blob (per-field {visible,x,y,fontSize,color}) owned entirely by the
    /// frontend's client/src/config/fieldLayout.js — the backend never parses or validates its
    /// shape, just stores and returns it, so new fields can be added client-side without a
    /// server change.</summary>
    public string? FieldLayoutJson { get; set; }

    /// <summary>Which queue-status card design renders: one of the 5 fixed preset ids owned by
    /// client/src/config/cardTemplates.js ("classic" | "ticket" | "hero" | "dashboard" |
    /// "timelineFocus"), or "custom" to use the free-form drag-and-drop layout (FieldLayoutJson)
    /// instead. The backend treats this as an opaque string, same as FieldLayoutJson.
    /// Defaults to "custom" (NOT one of the presets) — this column was added after the free-form
    /// layout system was already the only thing that existed, so every hospital that had already
    /// built a custom layout must keep seeing exactly that until an admin explicitly opts into a
    /// preset. Defaulting to a preset would silently replace their configured layout on
    /// deploy.</summary>
    public string CardTemplate { get; set; } = "custom";
}

public sealed record UpdateSettingsRequest(
    string? HospitalName,
    string? Tagline,
    string? Greeting,
    string? ArrivedTitle,
    string? ArrivedBody,
    string? PrimaryColor,
    string? AccentColor,
    string? FontFamily,
    string? FontSizeScale,
    string? SoundChoice,
    string? FieldLayoutJson = null,
    string? CardTemplate = null
);

public sealed record AdminLoginRequest(string Password);

public sealed record AdminLoginResponse(string Token);
