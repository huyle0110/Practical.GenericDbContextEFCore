using Practical.GenericDbContextEFCore.Database.Models;
using Practical.GenericDbContextEFCore.DbContext;
using Practical.GenericDbContextEFCore.Service;
using Practical.GenericDbContextEFCore.Service.Interface;

namespace Practical.GenericDbContextEFCore.Extension
{
    public static class DIExtension
    {
        public static void InjectDbContext(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            serviceCollection.AddScoped<IGenericDbContext<AdventureWorksDbContext>, GenericDbContext<AdventureWorksDbContext>>();
            serviceCollection.AddScoped<ICacheData, CacheData>();
            serviceCollection.AddSqlServer<AdventureWorksDbContext>(configuration.GetConnectionString("AdventureWorksConnection")?.ToString());
        }
    }
}
