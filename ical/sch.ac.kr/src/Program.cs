using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace sch.ac.kr
{
    class Program
    {
        static Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string?>(new[] {"-i", "--input"}),
                new Option<string?>(new[] {"-o", "--output"}),
                new Option<string?>(new[] {"-f", "--file"}),
                new Option<bool>(new[] {"-n", "--null-input"}, () => false),
                new Option<DateTime?>(new[] {"-s", "--start-from"}),
                new Option<TimeSpan>(new[] {"-M", "--max-elapsed-time"}, () => TimeSpan.FromDays(60)),
            };

            rootCommand.Handler = CommandHandler.Create(async (IHost host) =>
            {
                var app = host.Services.GetRequiredService<App>();
                await app.RunAsync();
                return Environment.ExitCode;
            });

            return CreateCommandLineBuilder(rootCommand).Build().InvokeAsync(args);
        }

        static CommandLineBuilder CreateCommandLineBuilder(Command rootCommand) =>
            new CommandLineBuilder(rootCommand)
                .UseHelp()
                .UseParseErrorReporting()
                .UseHost(Host.CreateDefaultBuilder, ConfigureHost);

        static void ConfigureHost(IHostBuilder host) =>
            host.ConfigureServices(services =>
                {
                    services.AddOptions<CalendarManagerOptions>()
                        .Configure<ParseResult>((option, p) =>
                        {
                            option.InputFileName = p.ValueForOption<string?>("--input");
                            option.OutputFileName = p.ValueForOption<string?>("--output");
                            option.FileName = p.ValueForOption<string?>("--file");
                            option.NullInput = p.ValueForOption<bool>("--null-input");
                        });
                    services.AddOptions<CalendarServiceOptions>()
                        .Configure<ParseResult>((option, p) =>
                        {
                            option.MinimumDtStart = p.ValueForOption<DateTime?>("--start-from");
                            option.MaximumElapsedTimeSinceDtStartToToday =
                                p.ValueForOption<TimeSpan>("--max-elapsed-time");
                        });
                    services.AddTransient<CalendarManager>();
                    services.AddSingleton<CalendarService>();
                    services.AddSingleton<App>();
                });
    }
}
