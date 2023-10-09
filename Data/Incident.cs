using System;
using System.ComponentModel.DataAnnotations;

namespace riskportal.Models
{
    public class Incident
    {
        [Key]
        public int Id { get; set; }

        public string Surname { get; set; }

        public string OtherNames { get; set; }

        public string Location { get; set; }

        public string Department { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public DateTime DateofIncident { get; set; }


        public string RiskDescription { get; set; }

        public string Action { get; set; }

        public string Control { get; set; }

        public string Status { get; set; }
        public string Remarks { get; set; }


    }
}
