using System.ComponentModel.DataAnnotations;

namespace SimpleApiBackend.Models
{
    public class TripCreateModel
    {
        public string Name { get; set; }
        public string Description { get; set; }


        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
    }

}
