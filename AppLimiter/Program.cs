using AppLimiter;
using System;
using System.Windows;
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();
        builder.Services.AddHttpClient();
        var host = builder.Build();
        host.Run();
    }
}