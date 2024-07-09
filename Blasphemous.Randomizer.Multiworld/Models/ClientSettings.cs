
namespace Blasphemous.Randomizer.Multiworld.Models;

/// <summary>
/// Settings that are determined by the client
/// </summary>
public class ClientSettings(string server, string name, string password)
{
    /// <summary>
    /// The ip address of the AP server
    /// </summary>
    public string Server { get; } = server;

    /// <summary>
    /// The slot name of the player
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The optional password to the server
    /// </summary>
    public string Password { get; } = password;
}
