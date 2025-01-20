using Microsoft.AspNetCore.Mvc;

namespace SimpleApiBackend.Models
{
    public class ErrorReportModel
    {
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
    }
}