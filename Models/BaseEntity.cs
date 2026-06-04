namespace GlobalSolution.SenseSpot.API.Models;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
