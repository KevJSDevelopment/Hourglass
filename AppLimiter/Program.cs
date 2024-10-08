using AppLimiter;
using AppLimiterLibrary;
using System;
using System.Windows;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
DatabaseManager.Initialize(host.Services.GetRequiredService<IConfiguration>());
await host.RunAsync();
