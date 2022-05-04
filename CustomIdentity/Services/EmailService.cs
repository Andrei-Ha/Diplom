using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace CustomIdentity.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration configuration)
        {
            _config = configuration;
        }
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Служба АСУ Пинских ЭС", _config.GetSection("Email:Login").Value));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = message
            };

            using (var client = new SmtpClient())
            {
                // Set our custom SSL certificate validation callback.
                client.ServerCertificateValidationCallback = MySslCertificateValidationCallback;
                await client.ConnectAsync("10.181.0.20", 25, false);
                await client.AuthenticateAsync(_config.GetSection("Email:Login").Value,_config.GetSection("Email:Password").Value);
                await client.SendAsync(emailMessage);

                await client.DisconnectAsync(true);
            }
        }

        static bool MySslCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
