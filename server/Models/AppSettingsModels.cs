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

    /// <summary>2026-09-02: dead column. Was meant to pick a background treatment for the first
    /// set of 10 fixed templates; that whole set (and the picker UI that wrote this) was replaced
    /// the same day with a second, fully-composed set of 10 that none of it applies to. Left as-is
    /// rather than migrated away — nothing reads or writes it anymore.</summary>
    public string CardBackgroundStyle { get; set; } = "vivid";

    /// <summary>Whether the 10 fixed templates show the patient's name / HN — independent toggles,
    /// admin-configurable. Does NOT affect "custom" (the free-form layout already has its own
    /// per-field visibility toggle in FieldLayoutJson). Default true for both: matches how most of
    /// the 10 templates already behaved before this was a setting at all — this was added because
    /// a few of them (added 2026-09-02) turned out to never show name/HN at all, an oversight, not
    /// a deliberate design some hospital might already be relying on.</summary>
    public bool ShowPatientName { get; set; } = true;
    public bool ShowHn { get; set; } = true;
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
    string? CardTemplate = null,
    string? CardBackgroundStyle = null,
    bool? ShowPatientName = null,
    bool? ShowHn = null
);

public sealed record AdminLoginRequest(string Password);

public sealed record AdminLoginResponse(string Token);
