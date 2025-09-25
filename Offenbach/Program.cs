using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using Offenbach;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddDiscordGateway()
    .AddApplicationCommands()
    .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
    .AddComponentInteractions<ModalInteraction, ModalInteractionContext>();

var host = builder.Build();

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();
