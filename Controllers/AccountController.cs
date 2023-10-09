using Microsoft.AspNetCore.Mvc;
using riskportal.Models;
using riskportal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using riskportal.Data;
using System.Linq;
using System.Collections.Generic;
using OfficeOpenXml;


namespace riskportal.Controllers
{
    public class AccountController : Controller
    {
        private readonly LdapAuthenticationService _ldapAuthenticationService;
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context, IConfiguration configuration, LdapAuthenticationService ldapAuthenticationService)
        {
            _ldapAuthenticationService = ldapAuthenticationService;
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        public IActionResult Loginpage()
        {
            return View();
        }

        public IActionResult Portal()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                // Redirect to the login page if not authenticated
                return RedirectToAction("Loginpage");
            }

            // Get the claims from the user's identity
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var cnClaim = claimsIdentity.FindFirst("cn");

            // Get the value of the cn claim (if it exists)
            string cn = cnClaim?.Value;

            // Create an instance of the view model
            var viewModel = new PortalViewModel
            {
                Username = User.Identity.Name,
                CN = cn
            };

            // Pass the view model to the view
            return View(viewModel);
        }

        public IActionResult Success()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt LDAP authentication
                (bool isAuthenticated, string cn) = _ldapAuthenticationService.Authenticate(model.Username, model.Password);

                if (isAuthenticated)
                {
                    // Append "@premiumpension.com" to the username
                    string username = model.Username + "";

                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username), // Username
                new Claim("cn", cn) // Add the cn claim
            };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                    HttpContext.Session.SetString("IsAuthenticated", "true");

                    // Successful
                    return RedirectToAction("Portal");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid credentials, please try again.");
                }
            }
            return View("Loginpage");
        }


        [Authorize]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear(); // Clear the session
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Sign out the user
            return RedirectToAction("Loginpage"); // Redirect to the login page
        }

        [HttpPost]
        public IActionResult UpdateRemarks(int id, string remarks)
        {
            // Find the incident in the database by id
            var incident = _context.Incidents.FirstOrDefault(i => i.Id == id);

            if (incident != null)
            {
                // Update the Remarks property
                incident.Remarks = remarks;

                // Save changes to the database
                _context.SaveChanges();

                return Json(new { success = true, message = "Remarks updated successfully." });
            }
            else
            {
                // Handle the case where the incident with the given id is not found
                return Json(new { success = false, message = "Incident not found." });
            }
        }


        [HttpPost]
        public IActionResult Submit(Incident model)
        {
            if (ModelState.IsValid)
            {
                // Parse the date format
                if (DateTime.TryParse(model.DateofIncident.ToString("yyyy-MM-dd"), out DateTime incidentDate))
                {
                    // Set the 'incidentDate' value to the 'DateofIncident' property in your 'Incident' model
                    model.DateofIncident = incidentDate;
                    model.Status = "pending";
                    model.Email = model.Email;
                    UpdateStatus(model.Id, model.Status);
                    // Add the model data to the DbContext
                    _context.Incidents.Add(model);

                    // Save changes to the database
                    _context.SaveChanges();

                    // Redirect to a success page or return a success message
                    return RedirectToAction("Success");
                }
                else
                {
                    // Invalid date format; handle the validation error
                    ModelState.AddModelError("DateofIncident", "Invalid date format. Please use YYYY-MM-DD.");
                }
            }

            // If ModelState is not valid, return to the form with validation errors
            return View("Portal", model); // Return to the form with errors
      }


        public IActionResult Data_table()
        {
            // Update status for all incidents (assuming you want to update all)
            var allIncidents = _context.Incidents.ToList();
            foreach (var incident in allIncidents)
            {
                // Update the status here, either by fetching it from the database or other logic
                incident.Status = GetStatusFromDatabase(incident.Id);
            }
            // Save changes to the database
            _context.SaveChanges();

            // Map Incident objects to DataTableViewModel objects
            var dataTableList = allIncidents.Select(incident => new DataTableViewModel
            {
                Id = incident.Id,
                Surname = incident.Surname,
                OtherNames = incident.OtherNames,
                Location = incident.Location,
                Department = incident.Department,
                Phone = incident.Phone,
                Email = incident.Email,
                DateOfIncident = (DateTime)incident.DateofIncident,
                RiskDescription = incident.RiskDescription,
                Action = incident.Action,
                Control = incident.Control,
                Status = incident.Status,
                Remarks = incident.Remarks,
            }).ToList();

            return View(dataTableList);
        }
        private string GetStatusFromDatabase(int incidentId)
        {
            // Assuming you have a method or logic to fetch the status from the database by incidentId
            // Replace this with your actual logic to fetch the status
            var incident = _context.Incidents.FirstOrDefault(i => i.Id == incidentId);
            if (incident != null)
            {
                return incident.Status;
            }

            // Return a default value if the incident is not found
            return "Pending"; // You can change this default value as needed
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            // Find the incident in the database by id
            var incident = _context.Incidents.FirstOrDefault(i => i.Id == id);
            Console.WriteLine($"Received id: {id}, status: {status}");

            if (incident != null)
            {
                // Update the status property
                incident.Status = status;

                // Save changes to the database
                _context.SaveChanges();

                return Json(new { success = true, message = "Status updated successfully." });
            }
            else
            {
                // Handle the case where the incident with the given id is not found
                return Json(new { success = false, message = "Incident not found." });
            }
        }
        public IActionResult ExportToExcel()
        {
            // Assuming you have a method or service to retrieve data
            var data = GetDataForExport(); // Replace with the actual method or service call

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                // Header row
                var headerRow = new List<string[]>
        {
            new string[] {"Id", "Surname", "Other Names", "Location", "Department", "Phone", "Email", "Date of Incident", "Risk Description", "Action to Minimize Risk", "Existing Control", "Status", "Remarks" }
        };
                foreach (var header in headerRow)
                {
                    worksheet.Cells[1, 1, 1, header.Length].Merge = true;
                    worksheet.Cells[1, 1].Value = "Reported Incidents"; // Optional: Set a title for the Excel sheet

                    // Convert the 'header' array to IEnumerable<object[]>
                    var headerData = new List<object[]>
            {
                header.Select(item => (object)item).ToArray()
            };

                    // Load the header data into the Excel worksheet
                    worksheet.Cells[2, 1, 2, header.Length].LoadFromArrays(headerData);
                }

                int i = 0;
                foreach (var item in data)
                {
                    var rowData = new List<object[]>
            {
                new object[]
                {
                   item.Id, item.Surname, item.OtherNames, item.Location, item.Department, item.Phone, item.Email,
                    item.DateOfIncident.ToString("yyyy-MM-dd"), item.RiskDescription, item.Action,
                    item.Control, item.Status, item.Remarks
                }
            };

                    worksheet.Cells[i + 3, 1, i + 3, rowData[0].Length].LoadFromArrays(rowData);
                    i++;
                }

                var excelBytes = package.GetAsByteArray();
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exported-data.xlsx");
            }
        }

        // This is a placeholder method; replace it with your actual data retrieval logic
        private IEnumerable<DataTableViewModel> GetDataForExport()
        {
            // Fetch the incidents from the database
            var incidents = _context.Incidents.ToList();

            // Convert the List<Incident> to List<DataTableViewModel>
            var dataTableList = incidents.Select(incident => new DataTableViewModel
            {
                Id = incident.Id,
                Surname = incident.Surname,
                OtherNames = incident.OtherNames,
                Location = incident.Location,
                Department = incident.Department,
                Phone = incident.Phone,
                Email = incident.Email,
                DateOfIncident = (DateTime)incident.DateofIncident,
                RiskDescription = incident.RiskDescription,
                Action = incident.Action,
                Control = incident.Control,
                Status = incident.Status,
                Remarks = incident.Remarks,
            }).ToList();

            // Return the converted list as IEnumerable<DataTableViewModel>
            return dataTableList;
        }
    }
}

 
