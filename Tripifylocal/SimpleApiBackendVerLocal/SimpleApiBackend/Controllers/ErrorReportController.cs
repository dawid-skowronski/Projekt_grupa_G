using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SimpleApiBackend.Models;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ErrorReportController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ErrorReportController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("error-report")]
    public IActionResult ReportError([FromBody] ErrorReportModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Subject) || string.IsNullOrWhiteSpace(model.Description))
        {
            return BadRequest(new { message = "Wszystkie pola są wymagane!" });
        }

        try
        {
            string supportEmail = _configuration["Email:Email"]; // noreply@projekt-tripfy.pl
            string supportPassword = _configuration["Email:Password"];
            string smtpServer = _configuration["Email:SmtpServer"];
            int smtpPort = int.Parse(_configuration["Email:SmtpPort"]);

            var mailMessage = new MailMessage(model.Email, supportEmail)
            {
                Subject = $"🚨 [Zgłoszenie błędu] {model.Subject}",
                Body = $@"
                            🚨 Nowe zgłoszenie błędu!

                            📧 Od użytkownika: {model.Email}
                            📝 Temat zgłoszenia: {model.Subject}

                            🛠️ Opis błędu:
                            🔹 {model.Description}

                            📅 Wysłano: {DateTime.Now:yyyy-MM-dd HH:mm:ss}


                                                                    ",
                IsBodyHtml = false
            };




            using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(supportEmail, supportPassword);
                smtpClient.EnableSsl = true;
                smtpClient.Send(mailMessage);
            }

            return Ok(new { message = "Twoje zgłoszenie zostało wysłane!" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd wysyłania maila: {ex.Message}");
            return StatusCode(500, new { message = "Wystąpił błąd podczas wysyłania zgłoszenia.", error = ex.Message });
        }
    }
}