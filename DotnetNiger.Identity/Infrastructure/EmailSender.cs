using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.Infrastructure;

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "noreply@dotnetniger.com";
    public string FromName { get; set; } = "DotnetNiger";
    public string AppName { get; set; } = "DotnetNiger";
    public string AppSubtitle { get; set; } = "";
    public string AppBaseUrl { get; set; } = "http://localhost:5075";
}

public class EmailSender : IEmailSender<ApplicationUser>
{
    private readonly SmtpOptions _smtp;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<SmtpOptions> smtp, ILogger<EmailSender> logger)
    {
        _smtp = smtp.Value;
        _logger = logger;
    }

    private string BuildTemplate(string title, string bodyHtml, string? ctaUrl = null, string? ctaText = null)
    {
        return $@"<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""></head>
<body style=""margin:0;padding:0;background-color:#f2f2f2;font-family:'Segoe UI',Tahoma,Arial,sans-serif"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f2f2f2;padding:30px 0"">
    <tr><td align=""center"">
      <table width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:8px;box-shadow:0 2px 8px rgba(0,0,0,0.08);overflow:hidden"">
        <tr>
          <td style=""padding:32px 40px 24px;background:linear-gradient(135deg,#512BD4,#6b3ff5)"">
            <h1 style=""color:#ffffff;margin:0;font-size:22px;font-weight:600;letter-spacing:0.5px"">{_smtp.AppName}</h1>
            {(string.IsNullOrEmpty(_smtp.AppSubtitle) ? "" : $"<p style=\"color:rgba(255,255,255,0.8);margin:4px 0 0;font-size:13px\">{_smtp.AppSubtitle}</p>")}
          </td>
        </tr>
        <tr><td style=""padding:32px 40px;color:#333333"">
          <h2 style=""margin:0 0 16px;font-size:20px;color:#512BD4"">{title}</h2>
          {bodyHtml}
        </td></tr>
        <tr>
          <td style=""padding:16px 40px;border-top:1px solid #e8e8e8;font-size:12px;color:#999999;text-align:center"">
            {_smtp.AppName} &mdash; &copy; 2026
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_smtp.Host))
        {
            _logger.LogInformation("[EMAIL] To={To} | Subject={Subject} | Body={Body}", toEmail, subject, htmlBody);
            return;
        }

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var body = new TextPart("html") { Text = htmlBody };
        message.Body = body;

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTlsWhenAvailable);
        if (!string.IsNullOrEmpty(_smtp.Username))
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {To}", toEmail);
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        return SendEmailAsync(email, $"Confirmez votre adresse email — {_smtp.AppName}",
            BuildTemplate(
                $"Bienvenue sur {_smtp.AppName}",
                $@"<p>Bonjour {user.FirstName ?? ""},</p>
<p>Merci de vous être inscrit. Veuillez confirmer votre adresse email pour activer votre compte.</p>
<p style=""text-align:center;margin:24px 0"">
  <a href=""{confirmationLink}"" style=""display:inline-block;padding:12px 28px;background:#512BD4;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:600"">Confirmer mon email</a>
</p>
<p style=""font-size:13px;color:#666"">Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur&nbsp;:</p>
<p style=""font-size:12px;color:#999;word-break:break-all"">{confirmationLink}</p>"));
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        return SendEmailAsync(email, $"Réinitialisation de mot de passe — {_smtp.AppName}",
            BuildTemplate(
                "Réinitialisation de mot de passe",
                $@"<p>Bonjour {user.FirstName ?? ""},</p>
<p>Vous avez demandé la réinitialisation de votre mot de passe.</p>
<p style=""text-align:center;margin:24px 0"">
  <a href=""{resetLink}"" style=""display:inline-block;padding:12px 28px;background:#512BD4;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:600"">Réinitialiser mon mot de passe</a>
</p>
<p style=""font-size:13px;color:#666"">Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>"));
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        return SendEmailAsync(email, $"Code de réinitialisation — {_smtp.AppName}",
            BuildTemplate(
                "Code de réinitialisation",
                $@"<p>Bonjour {user.FirstName ?? ""},</p>
<p>Voici votre code de réinitialisation de mot de passe&nbsp;:</p>
<p style=""text-align:center;margin:24px 0;font-size:32px;font-weight:700;letter-spacing:8px;color:#512BD4;font-family:'Courier New',monospace"">{resetCode}</p>
<p style=""font-size:13px;color:#666"">Ce code expire dans 15 minutes.</p>"));
    }

    public Task SendInviteEmailAsync(string email, string inviteUrl, string role)
    {
        return SendEmailAsync(email, $"Vous avez été invité sur {_smtp.AppName}",
            BuildTemplate(
                "Invitation à rejoindre",
                $@"<p>Bonjour,</p>
<p>Vous avez été invité à rejoindre {_smtp.AppName} en tant que <strong>{role}</strong>.</p>
<p style=""text-align:center;margin:24px 0"">
  <a href=""{inviteUrl}"" style=""display:inline-block;padding:12px 28px;background:#512BD4;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:600"">Accepter l'invitation</a>
</p>
<p style=""font-size:13px;color:#666"">Cette invitation expire dans 48 heures.</p>"));
    }

    public Task SendConfirmationCodeAsync(ApplicationUser user, string email, string code, string? confirmationLink = null)
    {
        return SendEmailAsync(email, $"Votre code de confirmation — {_smtp.AppName}",
            BuildTemplate(
                "Confirmez votre inscription",
                $@"<p>Bonjour {user.FirstName ?? ""},</p>
<p>Utilisez le code ci-dessous pour activer votre compte {_smtp.AppName}&nbsp;:</p>
<p style=""text-align:center;margin:24px 0;padding:16px;background:#f5f2ff;border-radius:8px"">
  <span style=""font-size:36px;font-weight:700;letter-spacing:10px;color:#512BD4;font-family:'Courier New',monospace"">{code}</span>
</p>
{(confirmationLink != null ? $@"<p style=""text-align:center;margin:24px 0"">
  <a href=""{confirmationLink}"" style=""display:inline-block;padding:12px 28px;background:#512BD4;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:600"">Confirmer mon compte</a>
</p>" : "")}
<p style=""font-size:13px;color:#666"">Ce code expire dans 15 minutes. Si vous n'avez pas créé de compte, ignorez cet email.</p>"));
    }
}
