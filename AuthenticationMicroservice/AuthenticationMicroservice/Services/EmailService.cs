namespace AuthenticationMicroservice.Services;

using System.Net;
using Exceptions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Models.DTOs;
using Settings;

public interface IEmailService
{
    Task SendEmail(EmailDTO emailDTO);
}

public class EmailService : IEmailService
{
    private readonly EmailConfig _emailConfig;

    public EmailService(IOptions<EmailConfig> emailConfig)
    {
        _emailConfig = emailConfig.Value;
    }

    public async Task SendEmail(EmailDTO emailDTO)
    {
        if (string.IsNullOrWhiteSpace(_emailConfig.Email) && string.IsNullOrWhiteSpace(_emailConfig.Password))
            throw new AuthenticationException("Email configuration failed.", HttpStatusCode.InternalServerError);
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress("aa03980", "smtp.gmail.com"));
        email.To.Add(new MailboxAddress(emailDTO.Receiver, emailDTO.To));
        email.Subject = emailDTO.Subject;
        email.Body = new TextPart(TextFormat.Html) {Text = emailDTO.Body};
        using var smtp = new SmtpClient();
        try
        {
            smtp.CheckCertificateRevocation = false;
            await smtp.ConnectAsync("smtp.gmail.com", 587, false);
            await smtp.AuthenticateAsync(_emailConfig.Email, _emailConfig.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            throw new AuthenticationException($"Failed to send email to: {emailDTO.To}. " + ex.Message,
                HttpStatusCode.InternalServerError);
        }
    }
}