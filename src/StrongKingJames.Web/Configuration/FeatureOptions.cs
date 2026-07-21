namespace StrongKingJames.Web.Configuration;

/// <summary>Optional feature toggles, bound from the "Features" config section.</summary>
public class FeatureOptions
{
    /// <summary>
    /// When false, the Notes feature is hidden: the Notes nav link, the /notes page form,
    /// the inline "Add note" on Browse, and the "use my notes" options in Chat and Search.
    /// </summary>
    public bool NotesEnabled { get; set; } = true;
}
