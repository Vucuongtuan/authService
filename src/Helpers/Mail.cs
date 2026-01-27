using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;

namespace authModule.src.Helpers
{
    public class Mail
    {
        private readonly IConfiguration _config;
        private readonly ILogger<Mail> _logger;

        public Mail(IConfiguration config, ILogger<Mail> logger)
        {
            _config = config;
            _logger = logger;
        }

        public (bool Status, string Message) SendMail(string to, string title, dynamic body, bool isHtml)
        {
            try
            {
                var smtpHost = _config["Mail:Host"];
                var smtpPort = _config.GetValue<int>("Mail:Port");
                var smtpUsername = _config["Mail:Form"];
                var smtpPassword = _config["Mail:Key"];
                var senderName = _config["Mail:SenderName"];
                var senderEmail = _config["Mail:SenderEmail"];
                var useSsl = _config.GetValue<bool>("Mail:UseSsl", true);


                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = title;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body.ToString();
                }
                else
                {
                    bodyBuilder.TextBody = body.ToString();
                }
                message.Body = bodyBuilder.ToMessageBody();


                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect(smtpHost, smtpPort, useSsl);
                    client.Authenticate(smtpUsername, smtpPassword);
                    client.Send(message);
                    client.Disconnect(true);
                }

                return (true, "Email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending email to {To}", to);
                return (false, "Failed to send email: " + ex.Message);
            }
        }
    }
}
