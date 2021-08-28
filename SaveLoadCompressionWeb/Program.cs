using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using BlazorDownloadFile;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace SaveLoadCompressionWeb {
    public class Program {
        public static async Task Main(string[] args) {
            WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddLocalization();
            builder.Services.AddScoped(typeof(SaveLoadCompression.SaveLoadCompression));
            builder.Services.AddSingleton(sp => (IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>());
            builder.Services.AddBlazorDownloadFile();

            await builder.Build().RunAsync();
        }
    }
}
