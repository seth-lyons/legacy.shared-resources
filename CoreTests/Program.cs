using CoreTests.Objects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace CoreTests
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }
        public static IServiceProvider ServiceProvider { get; set; }
        static async Task Main(string[] args)
        {
            var isDevelopment =
                (Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "development").Equals("development", StringComparison.InvariantCultureIgnoreCase);

            Console.WriteLine($"***************{(isDevelopment ? "DEVELOPMENT" : "PRODUCTION*")}**************\n\n");
            SetConfiguration(isDevelopment);
            using (IServiceScope scope = ServiceProvider.CreateScope())
                await ServiceProvider.GetService<Start>().Main(args);
            DisposeServices();
            Console.WriteLine("\n\n****************************************");
        }

        static void SetConfiguration(bool isDevelopment)
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            if (isDevelopment) builder.AddUserSecrets<Program>(true, true);
            builder.AddEnvironmentVariables();

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection()
                 .Configure<Settings>(Configuration) // USE Configuration.GetSection("{SectionName}")) For Section Specific
                 .AddOptions();

            RegisterDependencies(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        static void RegisterDependencies(IServiceCollection services)
        {
            services.AddSingleton<Start>();
            // services.AddDbContext<NLCDataContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DB_Context")), ServiceLifetime.Transient);
            // To Scaffold DB: Scaffold-DbContext "Server={host};Initial Catalog={database};Persist Security Info=False;User ID={username};Password={password};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Contexts -FORCE
        }

        private static void DisposeServices()
        {
            if (ServiceProvider == null)
                return;
            if (ServiceProvider is IDisposable)
                ((IDisposable)ServiceProvider).Dispose();
        }
    }
}
