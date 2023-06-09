namespace Sapling.Tokens;

/// <summary>
/// Class <c>SaplingType</c> represents a valid sapling type (int, float, bool) within the sapling programming language.
/// </summary>
internal class SaplingType: Token
{
    /// <summary>
    /// This construsts a new instance of a sapling type (int, float, bool, etc).
    /// <example>
    /// For example:
    /// <code>
    /// SaplingType int = new SaplingType(0, 0, "int");
    /// </code>
    /// will create a new SaplingType instance with the value of "int".
    /// </example>
    /// </summary>
    public SaplingType(int startIndex, int endIndex, string value): base(startIndex, endIndex, value)
    {
    }
}