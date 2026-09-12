using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Microsoft.Extensions.DependencyInjection;
using MyCivilPlugin.Application;
using MyCivilPlugin.Infrastructure.Ai;
using System;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(MyCivilPlugin.PluginExtension))]

namespace MyCivilPlugin
{
    public class PluginExtension : IExtensionApplication
    {
        public static ServiceProvider Services { get; private set; }

        public void Initialize()
        {
            var services = new ServiceCollection();

            // Application
            services.AddSingleton<ToolRegistry>();
            services.AddSingleton<AiOrchestrator>();

            // Infrastructure
            services.AddSingleton<ILlmClient, StubLlmClient>(); // заменим на OpenAI позже

            Services = services.BuildServiceProvider();

            var ed = AcApp.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage($"\nMyCivilPlugin загружен. Инструментов в реестре: {Services.GetRequiredService<ToolRegistry>().All.Count}");
        }

        public void Terminate()
        {
            Services?.Dispose();
        }
    }
}