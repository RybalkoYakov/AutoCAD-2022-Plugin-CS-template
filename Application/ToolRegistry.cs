using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyCivilPlugin.Application.Tools;

namespace MyCivilPlugin.Application
{
    /// <summary>
    /// Реестр всех инструментов плагина.
    /// </summary>
    public class ToolRegistry
    {
        private readonly Dictionary<string, IPluginTool> _tools;

        public ToolRegistry()
        {
            _tools = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IPluginTool).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && !t.IsInterface)
                .Select(t => (IPluginTool)Activator.CreateInstance(t))
                .ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyCollection<IPluginTool> All => _tools.Values.ToList();

        public IPluginTool Find(string name)
        {
            _tools.TryGetValue(name, out var tool);
            return tool;
        }
    }
}