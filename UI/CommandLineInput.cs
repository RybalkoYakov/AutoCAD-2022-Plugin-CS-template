using Autodesk.AutoCAD.EditorInput;
using MyCivilPlugin.Models;

namespace MyCivilPlugin.UI
{
    internal static class CommandLineInput
    {
        public static bool TryGetElevationOffsetOptions(Editor ed, out ElevationOffsetOptions options)
        {
            options = null;

            double? min = PromptDouble(ed, "\nВведите минимальное смещение по высоте (м)", 0);
            if (!min.HasValue) return false;

            double? max = PromptDouble(ed, "\nВведите максимальное смещение по высоте (м)", 0);
            if (!max.HasValue) return false;

            options = new ElevationOffsetOptions
            {
                MinOffset = min.Value,
                MaxOffset = max.Value
            };

            if (!options.IsValid(out string error))
            {
                ed.WriteMessage($"\nОшибка: {error}");
                return false;
            }

            return true;
        }

        public static bool TryGetHorizontalOffsetOptions(Editor ed, out HorizontalOffsetOptions options)
        {
            options = null;

            double? minX = PromptDouble(ed, "\nВведите минимальное смещение по X (м)", -0.05);
            if (!minX.HasValue) return false;

            double? maxX = PromptDouble(ed, "\nВведите максимальное смещение по X (м)", 0.05);
            if (!maxX.HasValue) return false;

            double? minY = PromptDouble(ed, "\nВведите минимальное смещение по Y (м)", -0.05);
            if (!minY.HasValue) return false;

            double? maxY = PromptDouble(ed, "\nВведите максимальное смещение по Y (м)", 0.05);
            if (!maxY.HasValue) return false;

            options = new HorizontalOffsetOptions
            {
                MinOffsetX = minX.Value,
                MaxOffsetX = maxX.Value,
                MinOffsetY = minY.Value,
                MaxOffsetY = maxY.Value
            };

            if (!options.IsValid(out string error))
            {
                ed.WriteMessage($"\nОшибка: {error}");
                return false;
            }

            return true;
        }

        public static bool TryGetPointCreationOptions(Editor ed, out PointCreationOptions options)
        {
            options = null;

            // 1. Имя группы
            PromptStringOptions nameOpts = new PromptStringOptions("\nВведите имя новой группы точек")
            {
                AllowSpaces = true
            };
            PromptResult nameRes = ed.GetString(nameOpts);
            if (nameRes.Status != PromptStatus.OK) return false;

            // 2. Диапазон X
            double? minX = PromptDouble(ed, "\nМинимальное смещение по X (м)", -0.05);
            if (!minX.HasValue) return false;
            double? maxX = PromptDouble(ed, "\nМаксимальное смещение по X (м)", 0.05);
            if (!maxX.HasValue) return false;

            // 3. Диапазон Y
            double? minY = PromptDouble(ed, "\nМинимальное смещение по Y (м)", -0.05);
            if (!minY.HasValue) return false;
            double? maxY = PromptDouble(ed, "\nМаксимальное смещение по Y (м)", 0.05);
            if (!maxY.HasValue) return false;

            // 4. Диапазон Z
            double? minZ = PromptDouble(ed, "\nМинимальное смещение по Z (м)", -0.02);
            if (!minZ.HasValue) return false;
            double? maxZ = PromptDouble(ed, "\nМаксимальное смещение по Z (м)", 0.05);
            if (!maxZ.HasValue) return false;

            options = new PointCreationOptions
            {
                GroupName = nameRes.StringResult,
                MinOffsetX = minX.Value,
                MaxOffsetX = maxX.Value,
                MinOffsetY = minY.Value,
                MaxOffsetY = maxY.Value,
                MinOffsetZ = minZ.Value,
                MaxOffsetZ = maxZ.Value
            };

            if (!options.IsValid(out string error))
            {
                ed.WriteMessage($"\nОшибка: {error}");
                return false;
            }

            return true;
        }

        public static bool AskConfirmation(Editor ed, string message)
        {
            PromptKeywordOptions opts = new PromptKeywordOptions(
            message + "\nВы уверены? (Y = Да, N = Нет, Enter = отмена)")
            {
                AllowNone = false
            };
            opts.Keywords.Add("Yes", "Да", "Да");
            opts.Keywords.Add("No", "Нет", "Нет");
            opts.Keywords.Default = "No";

            PromptResult res = ed.GetKeywords(opts);
            return res.Status == PromptStatus.OK && res.StringResult == "Yes";
        }

        private static double? PromptDouble(Editor ed, string message, double defaultValue)
        {
            PromptDoubleOptions opts = new PromptDoubleOptions(message)
            {
                AllowNegative = true,
                AllowZero = true,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };

            PromptDoubleResult res = ed.GetDouble(opts);
            if (res.Status != PromptStatus.OK) return null;
            return res.Value;
        }
    }
}