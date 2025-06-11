using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlContent);
}

        public class EmailService : IEmailService
        {
            private readonly string _emailFrom = "duytruongton@gmail.com";        // Email gửi
            private readonly string _emailPassword = "sxnxigwesyqozhgk";          // App Password

            public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailFrom));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            // Gán HTML vào Text khi dùng "html"
            email.Body = new TextPart("html")
            {
                Text = htmlContent
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailFrom, _emailPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
}
