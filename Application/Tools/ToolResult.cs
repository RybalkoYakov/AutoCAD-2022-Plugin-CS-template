using System.Text.Json;

namespace MyCivilPlugin.Application.Tools
{
    /// <summary>
    /// Универсальный результат выполнения инструмента.
    /// Возвращается в LLM и в UI.
    /// </summary>
    public class ToolResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }          // человекочитаемый текст
        public JsonElement? Data { get; set; }       // структурированные данные (опционально)

        public static ToolResult Ok(string message) =>
            new ToolResult { Success = true, Message = message };

        public static ToolResult Ok(string message, JsonElement data) =>
            new ToolResult { Success = true, Message = message, Data = data };

        public static ToolResult Fail(string message) =>
            new ToolResult { Success = false, Message = message };
    }
}