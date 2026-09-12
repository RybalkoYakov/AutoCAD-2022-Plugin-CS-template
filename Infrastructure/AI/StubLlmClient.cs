using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyCivilPlugin.Infrastructure.Ai
{
    /// <summary>
    /// Заглушка LLM-клиента. Всегда отвечает фиксированным текстом.
    /// Заменяется на реальный клиент (OpenAI/Claude/Ollama) на следующем этапе.
    /// </summary>
    public class StubLlmClient : ILlmClient
    {
        public Task<LlmResponse> SendAsync(
            IReadOnlyList<LlmMessage> history,
            IReadOnlyList<LlmToolDefinition> tools)
        {
            return Task.FromResult(new LlmResponse
            {
                Text = "[StubLlmClient] ИИ-слой ещё не подключён."
            });
        }
    }
}