using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using MyCivilPlugin.Domain.Models;
using MyCivilPlugin.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyCivilPlugin.Application.Tools
{
    public class RandomizePointElevationTool : IPluginTool
    {
        public string Name => "randomize_point_elevation";

        public string Description =>
            "Применяет случайное смещение по высоте (Z) к точкам COGO. " +
            "По умолчанию работает с текущим выделением в AutoCAD. " +
            "Можно явно указать номера точек через параметр 'pointNumbers'. " +
            "Используется для имитации погрешности измерений.";

        public bool RequiresConfirmation => true;

        public JsonElement ParametersSchema => JsonDocument.Parse(@"
        {
          ""type"": ""object"",
          ""properties"": {
            ""minOffset"": {
              ""type"": ""number"",
              ""description"": ""Минимальное смещение по Z в метрах. Может быть отрицательным.""
            },
            ""maxOffset"": {
              ""type"": ""number"",
              ""description"": ""Максимальное смещение по Z в метрах. Должно быть >= minOffset.""
            },
            ""pointNumbers"": {
              ""type"": ""array"",
              ""items"": { ""type"": ""integer"" },
              ""description"": ""Опциональный список номеров точек COGO. Если не указан — используются выделенные точки.""
            }
          },
          ""required"": [""minOffset"", ""maxOffset""]
        }").RootElement;

        public ToolResult Execute(JsonElement args)
        {
            // 1. Валидация диапазона
            if (!args.TryGetProperty("minOffset", out var minEl) || !minEl.TryGetDouble(out double min))
                return ToolResult.Fail("Не указан или некорректен параметр 'minOffset'.");

            if (!args.TryGetProperty("maxOffset", out var maxEl) || !maxEl.TryGetDouble(out double max))
                return ToolResult.Fail("Не указан или некорректен параметр 'maxOffset'.");

            var options = new ElevationOffsetOptions { MinOffset = min, MaxOffset = max };
            if (!options.IsValid(out string error))
                return ToolResult.Fail(error);

            // 2. Определяем, с какими точками работать
            List<ObjectId> pointIds;
            try
            {
                pointIds = ResolveTargetPoints(args);
            }
            catch (Exception ex)
            {
                return ToolResult.Fail(ex.Message);
            }

            if (pointIds.Count == 0)
                return ToolResult.Fail("Не выбрано ни одной точки COGO для обработки.");

            // 3. Выполнение
            try
            {
                using (Transaction ts = AcApp.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
                {
                    var result = CogoPointElevationService.RandomizeElevation(ts, pointIds, options);
                    ts.Commit();

                    if (result.Processed == 0)
                        return ToolResult.Fail("Не найдено ни одной подходящей точки COGO.");

                    string msg = $"Обработано точек: {result.Processed}.";
                    if (result.SkippedLocked > 0)
                        msg += $" Пропущено заблокированных: {result.SkippedLocked}.";

                    return ToolResult.Ok(msg);
                }
            }
            catch (Exception ex)
            {
                return ToolResult.Fail($"Ошибка выполнения: {ex.Message}");
            }
        }

        /// <summary>
        /// Возвращает список ObjectId точек, которые нужно обработать.
        /// Приоритет: явный список номеров в аргументах → текущее выделение.
        /// </summary>
        private List<ObjectId> ResolveTargetPoints(JsonElement args)
        {
            // Вариант A: явный список номеров точек
            if (args.TryGetProperty("pointNumbers", out var numbersEl)
                && numbersEl.ValueKind == JsonValueKind.Array
                && numbersEl.GetArrayLength() > 0)
            {
                var numbers = new List<uint>();
                foreach (var el in numbersEl.EnumerateArray())
                {
                    if (!el.TryGetUInt32(out uint n))
                        throw new InvalidOperationException("Номер точки должен быть целым положительным числом.");
                    numbers.Add(n);
                }

                var ids = new List<ObjectId>();
                Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;

                using (Transaction ts = AcApp.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
                {
                    foreach (var cogoId in CivilApplication.ActiveDocument.CogoPoints)
                    {
                        var p = ts.GetObject(cogoId, OpenMode.ForRead) as CogoPoint;
                        if (p != null && numbers.Contains(p.PointNumber))
                            ids.Add(cogoId);
                    }
                }

                if (ids.Count == 0)
                    throw new InvalidOperationException(
                        $"Ни одна из указанных точек не найдена в чертеже: {string.Join(", ", numbers)}.");

                return ids;
            }

            // Вариант B: текущее выделение в AutoCAD
            Editor editor = AcApp.DocumentManager.MdiActiveDocument.Editor;
            PromptSelectionResult selRes = editor.SelectImplied();

            if (selRes.Status != PromptStatus.OK || selRes.Value == null)
                throw new InvalidOperationException(
                    "Нет выделенных точек COGO. Выделите точки в AutoCAD или укажите параметр 'pointNumbers'.");

            // Фильтруем только точки COGO
            var selectedIds = new List<ObjectId>();
            using (Transaction ts = AcApp.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in selRes.Value.GetObjectIds())
                {
                    var p = ts.GetObject(id, OpenMode.ForRead) as CogoPoint;
                    if (p != null) selectedIds.Add(id);
                }
            }

            if (selectedIds.Count == 0)
                throw new InvalidOperationException(
                    "Среди выделенных объектов нет ни одной точки COGO.");

            return selectedIds;
        }
    }
}