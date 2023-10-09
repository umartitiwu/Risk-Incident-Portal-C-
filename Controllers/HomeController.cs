using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using riskportal.Models;
using riskportal.Data;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Configuration;
using System.Linq;


namespace riskportal.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public IActionResult Privacy()
        {
            return View();
        }

         public IActionResult TestDatabaseConnection()
         {
             try
             {
                 var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                 optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));

                 using var context = new ApplicationDbContext(optionsBuilder.Options);
                 context.Database.EnsureCreated(); // Ensure the database is created (if it doesn't exist).
                 ViewData["IsConnected"] = true;  // If no exceptions are thrown, the connection is successful.
             }
             catch (Exception ex)
             {
                 ViewData["IsConnected"] = false;
                 ViewData["ErrorMessage"] = ex.Message;
             }

             return View();
         }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
