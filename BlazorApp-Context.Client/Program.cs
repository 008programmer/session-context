using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BlazorApp_Context.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddScoped(sp => new HttpClient());

            await builder.Build().RunAsync();
        }
    }
}
