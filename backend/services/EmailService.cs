using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = false; 
    public string User { get; set; } = "";
    public string Pass { get; set; } = "";
    public string From { get; set; } = "";
}

public interface IEmailService
{
    Task<string> SendAsync(string to, string subject, string? textBody = null);
}

public class EmailService : IEmailService
{
    private readonly SmtpOptions _opt;
    public EmailService(IOptions<SmtpOptions> opt) => _opt = opt.Value;

    public async Task<string> SendAsync(string to, string subject, string? textBody = null)
    {
        var message = new MimeMessage();
        message.From.Clear();
        message.From.Add(MailboxAddress.Parse(_opt.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Sender = new MailboxAddress(_opt.User, _opt.User);
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = textBody,
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        var secureOption = _opt.UseSsl ? SecureSocketOptions.SslOnConnect 
                                       : SecureSocketOptions.StartTls;

        await client.ConnectAsync(_opt.Host, _opt.Port, secureOption);
        await client.AuthenticateAsync(_opt.User, _opt.Pass);
        var response = await client.SendAsync(message);
        await client.DisconnectAsync(true);

        return response; 
    }
}
