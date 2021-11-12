using System;
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
        public static Task Main(string[] args) {
            WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.Services.AddLogging()
                            .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
                            .AddLocalization()
                            .AddScoped(typeof(PngCompression.PngCompression))
                            .AddScoped(typeof(Models.PngProcessor))
                            .AddSingleton(sp => (IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>())
                            .AddBlazorDownloadFile();
            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

            return builder.Build().RunAsync();
        }
    }
}
