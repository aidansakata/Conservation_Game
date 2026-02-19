public class Hex
{
    public int Id { get; set; }
    public int Col { get; set; }
    public int Row { get; set; }

    public int Optimal { get; set; }
    public string? Type { get; set; } // nullable
    public int Utility { get; set; }

    public override string ToString()
    {
        string typeDisplay = Type ?? "?";
        return $"ID:{Id} ({Col},{Row}) Type:{typeDisplay} Opt:{Optimal} Util:{Utility}";
    }
}