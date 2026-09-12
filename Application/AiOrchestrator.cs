using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MyCivilPlugin.Application.Tools;
using MyCivilPlugin.Infrastructure.Ai;
using MyCivilPlugin.Infrastructure.CadIntegration;

namespace MyCivilPlugin.Application
{
    public class AiOrchestrator
    {
        private readonly ILlmClient _llm;
        private readonly ToolRegistry _tools;
        private readonly List<LlmMessage> _history = new List<LlmMessage>();

        public AiOrchestrator(ILlmClient llm, ToolRegistry tools)
        {
            _llm = llm;
            _tools = tools;
        }

        public async Task<string> SendAsync(string userMessage)
        {
            _history.Add(new LlmMessage { Role = "user", Content = userMessage });

            // 1. Отдаём в LLM историю + список инструментов
            var toolDefs = _tools.All.Select(t => new LlmToolDefinition
            {
                Name = t.Name,
                Description = t.Description,
                ParametersSchema = t.ParametersSchema
            }).ToList();

            var response = await _llm.SendAsync(_history, toolDefs);

            // 2. Если LLM просит вызвать инструмент — вызываем
            if (!string.IsNullOrEmpty(response.ToolName))
            {
                var tool = _tools.Find(response.ToolName);
                if (tool == null)
                {
                    return $"Инструмент '{response.ToolName}' не найден.";
                }

                // TODO: подтверждение для RequiresConfirmation
                // TODO: dry run
                // TODO: логирование

                ToolResult result = CadDispatcher.Execute(() => tool.Execute(response.ToolArgs));

                _history.Add(new LlmMessage
                {
                    Role = "tool",
                    Content = result.Message
                });

                // 3. Возвращаем результат обратно в LLM — пусть сформулирует ответ
                // (на следующем этапе; пока просто возвращаем текст)
                return result.Message;
            }

            // 4. LLM ответила текстом
            _history.Add(new LlmMessage { Role = "assistant", Content = response.Text });
            return response.Text;
        }
    }
}