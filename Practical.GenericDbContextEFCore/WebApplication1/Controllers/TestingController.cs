using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Practical.GenericDbContextEFCore.Database.Models;
using Practical.GenericDbContextEFCore.DbContext;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestingController : ControllerBase
    {
        private readonly IGenericDbContext<AdventureWorksDbContext> _genericDbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TestingController> _logger;

        public TestingController(IGenericDbContext<AdventureWorksDbContext> genericDbContext,
            IConfiguration configuration,
            ILogger<TestingController> logger)
        {
            _logger = logger;
            _genericDbContext = genericDbContext;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _genericDbContext.Repository<Currency>().ToListAsync();
            return Ok(result);
        }

        [HttpGet("checkdapper")]
        public async Task<IActionResult> TestDapper()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("AdventureWorksConnection")))
            {
                // Query to fetch data
                var sql = "SELECT * FROM [Sales].Currency";
                var currencies = connection.Query<Currency>(sql).ToList();

                foreach (var currency in currencies)
                {
                    Console.WriteLine($"{currency.CurrencyCode}: {currency.Name}, {currency.CurrencyCode}");
                }
            }
            return Ok();
        }
    }
}
