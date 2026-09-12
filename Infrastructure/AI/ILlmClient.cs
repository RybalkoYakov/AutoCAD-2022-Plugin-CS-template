using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyCivilPlugin.Infrastructure.Ai
{
    /// <summary>
    /// Абстракция над LLM-провайдером (OpenAI, Anthropic, Ollama).
    /// </summary>
    public interface ILlmClient
    {
        /// <summary>
        /// Отправить диалог и доступные инструменты в LLM.
        /// Возвращает либо текстовый ответ, либо запрос на вызов инструмента.
        /// </summary>
        Task<LlmResponse> SendAsync(
            IReadOnlyList<LlmMessage> history,
            IReadOnlyList<LlmToolDefinition> tools);
    }

    public class LlmMessage
    {
        public string Role { get; set; }   // "user", "assistant", "system", "tool"
        public string Content { get; set; }
    }

    public class LlmToolDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public System.Text.Json.JsonElement ParametersSchema { get; set; }
    }

    public class LlmResponse
    {
        public string Text { get; set; }             // если LLM ответила текстом
        public string ToolName { get; set; }         // если LLM запросила вызов инструмента
        public System.Text.Json.JsonElement ToolArgs { get; set; }
    }
}