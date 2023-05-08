namespace AuthenticationMicroservice.Models.BaseObjects;

public class BaseGuidDto
{
    public BaseGuidDto()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }
}

public class BaseIntDto
{
    public BaseIntDto()
    {
        Id = new int();
    }

    public int Id { get; set; }
}