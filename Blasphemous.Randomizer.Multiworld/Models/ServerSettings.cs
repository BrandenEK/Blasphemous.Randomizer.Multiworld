
namespace Blasphemous.Randomizer.Multiworld.Models;

/// <summary>
/// Settings that are determined by the server
/// </summary>
public class ServerSettings(Config config, int requiredEnding, bool deathLinkEnabled)
{
    /// <summary>
    /// The randomizer configuration
    /// </summary>
    public Config Config { get; } = config;

    /// <summary>
    /// The ending that must be reached to goal
    /// </summary>
    public int RequiredEnding { get; } = requiredEnding;

    /// <summary>
    /// Whether deathlink is active or not
    /// </summary>
    public bool DeathLinkEnabled { get; } = deathLinkEnabled;
}
