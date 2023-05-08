namespace AuthenticationMicroservice.Models.DTOs;

public class EmailDTO
{
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? To { get; set; }
    public string? Receiver { get; set; }
}