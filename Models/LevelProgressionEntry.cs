namespace EmeraldServer.Models;

public class LevelProgressionEntry
{
    public int Level { get; set; }
    public int RequiredXp { get; set; }
    public int? GiftDropId { get; set; }
}
