using Microsoft.AspNetCore.Mvc;
using riskportal.Models;
using riskportal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using riskportal.Data;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;

namespace riskportal.Controllers
{
    public class AdminController : Controller
    {
        private readonly LdapAuthenticationService _ldapAuthenticationService;
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context, IConfiguration configuration, LdapAuthenticationService ldapAuthenticationService)
        {
            _ldapAuthenticationService = ldapAuthenticationService;
            _context = context;
        }

        public IActionResult Data_table()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                // Redirect to the login page if not authenticated
                return RedirectToAction("Admin");
            }

            // Get the claims from the user's identity
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var cnClaim = claimsIdentity.FindFirst("cn");

            // Get the value of the cn claim (if it exists)
            string cn = cnClaim?.Value;

            // Create an instance of the view model
            var viewModel = new DataTableViewModel
            {
                Username = User.Identity.Name,
                CN = cn
            };

            // Pass the view model to the view
            return View(viewModel);
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

        public IActionResult Admin()
        {
            return View();
        }


        [Authorize]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear(); // Clear the session
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Sign out the user
            return RedirectToAction("Admin"); // Redirect to the login page
        }



        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt LDAP authentication
                (bool isAuthenticated, string cn) = _ldapAuthenticationService.AuthenticateAdmin(model.Username, model.Password);

                if (isAuthenticated)
                {
                    // Append "@premiumpension.com" to the username
                    string username = model.Username + " ";

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
                    return RedirectToAction("Data_table");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid credentials, please try again.");
                }
            }
            return View("Admin");
        }
    }
}
