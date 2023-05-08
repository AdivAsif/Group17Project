namespace Group17profile.Models.DefaultObjects;

public class DefaultGuidDTO
{
    public DefaultGuidDTO()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }
}

public class DefaultIntDTO
{
    public DefaultIntDTO()
    {
        Id = new int();
    }

    public int Id { get; set; }
}