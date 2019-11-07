using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using CQELight;

namespace CQELight_ASPNETCore3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureCQELight(b =>
                {
                    b.UseAutofacAsIoC();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
