using Microsoft.AspNetCore.Mvc;
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

        private readonly ILogger<TestingController> _logger;

        public TestingController(IGenericDbContext<AdventureWorksDbContext> genericDbContext,
            ILogger<TestingController> logger)
        {
            _logger = logger;
            _genericDbContext = genericDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _genericDbContext.Repository<Currency>().ToListAsync();
            return Ok(result);
        }
    }
}
