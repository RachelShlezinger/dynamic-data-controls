namespace DynamicDataControls.Api.Models.Domain;

/// <summary>
/// Authorization role constants used across controllers and policies.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Role required to create, update, and delete controls.
    /// Hebrew: מנהל_בקרות
    /// </summary>
    public const string ControlsManager = "מנהל_בקרות";

    /// <summary>
    /// Role required for system administration (metadata management, user management).
    /// Hebrew: מנהל_מערכת
    /// </summary>
    public const string SystemAdmin = "מנהל_מערכת";
}
