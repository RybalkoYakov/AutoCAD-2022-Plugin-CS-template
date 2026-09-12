using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using MyCivilPlugin.Models;
using MyCivilPlugin.Services;
using MyCivilPlugin.UI;
using System.Collections.Generic;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyCivilPlugin.Commands
{
    public class CogoPointCommands
    {
        [CommandMethod("RANDOMIZEPOINTZ")]
        public void RandomizePointElevation()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            if (!CommandLineInput.TryGetElevationOffsetOptions(ed, out ElevationOffsetOptions options))
            {
                ed.WriteMessage("\nОперация отменена.");
                return;
            }

            string message = $"\nБудут изменены высоты всех точек COGO. " +
                             $"Смещение в диапазоне [{options.MinOffset:F3}, {options.MaxOffset:F3}] м.";
            if (!CommandLineInput.AskConfirmation(ed, message))
            {
                ed.WriteMessage("\nОперация отменена.");
                return;
            }

            using (Transaction ts = AcApp.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                CogoPointElevationService.Result result =
                    CogoPointElevationService.RandomizeElevation(civilDoc, ts, options);
                ts.Commit();

                ReportResult(ed, result);
            }
        }

        [CommandMethod("RANDOMIZEPOINTXY")]
        public void RandomizePointHorizontal()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            // 1. Ввод параметров
            if (!CommandLineInput.TryGetHorizontalOffsetOptions(ed, out HorizontalOffsetOptions options))
            {
                ed.WriteMessage("\nОперация отменена.");
                return;
            }

            // 2. Подтверждение
            string message = $"\nБудут изменены координаты X/Y всех точек COGO в текущей ПСК. " +
                             $"X: [{options.MinOffsetX:F3}, {options.MaxOffsetX:F3}] м, " +
                             $"Y: [{options.MinOffsetY:F3}, {options.MaxOffsetY:F3}] м.";
            if (!CommandLineInput.AskConfirmation(ed, message))
            {
                ed.WriteMessage("\nОперация отменена.");
                return;
            }

            // 3. Получаем матрицу текущей ПСК
            Matrix3d ucsToWcs = ed.CurrentUserCoordinateSystem;

            // 4. Выполнение в транзакции
            using (Transaction ts = AcApp.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                CogoPointHorizontalService.Result result =
                    CogoPointHorizontalService.RandomizeHorizontal(civilDoc, ts, options, ucsToWcs);
                ts.Commit();

                ReportHorizontalResult(ed, result);
            }
        }

        [CommandMethod("CREATEDOFFSETPOINTS")]
        public void CreateOffsetPoints()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            // 1. Выбор исходных точек
            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nВыберите точки COGO для смещения: "
            };
            SelectionFilter filter = new SelectionFilter(
                new[] { new TypedValue((int)DxfCode.Start, "AECC_COGO_POINT") });

            PromptSelectionResult selRes = ed.GetSelection(selOpts, filter);
            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nТочки не выбраны. Операция отменена.");
                return;
            }

            List<ObjectId> selectedPoints = new List<ObjectId>(selRes.Value.GetObjectIds());

            // 2. Параметры
            if (!CommandLineInput.TryGetPointCreationOptions(ed, out PointCreationOptions options))
            {
                ed.WriteMessage("\nОперация отменена.");
                return;
            }

            // 3. Подтверждение
            string message =
                $"\nБудет создано {selectedPoints.Count} новых точек в группе '{options.GroupName}'. " +
                $"\nДиапазон X: [{options.MinOffsetX:F3}, {options.MaxOffsetX:F3}] м, " +
                $"\nДиапазон Y: [{options.MinOffsetY:F3}, {options.MaxOffsetY:F3}] м, " +
                $"\nДиапазон Z: [{options.MinOffsetZ:F3}, {options.MaxOffsetZ:F3}] м.";

            if (!CommandLineInput.AskConfirmation(ed, message))
            {
                ed.WriteMessage("\nОперация отменена.");
                return;
            }

            // 4. Выполнение
            using (Transaction ts = AcApp.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                CogoPointCreationService.Result result =
                    CogoPointCreationService.CreateOffsetPoints(civilDoc, ts, selectedPoints, options);
                ts.Commit();

                ed.WriteMessage("\n--- Создание точек завершено ---");
                ed.WriteMessage($"\nСоздано точек: {result.CreatedCount}");
                ed.WriteMessage($"\nГруппа: '{options.GroupName}'");
            }
        }

        private void ReportHorizontalResult(Editor ed, CogoPointHorizontalService.Result result)
        {
            ed.WriteMessage("\n--- Горизонтальное смещение точек COGO завершено ---");

            if (result.Processed == 0)
            {
                ed.WriteMessage("\nНе найдено ни одной точки COGO для обработки.");
                return;
            }

            ed.WriteMessage($"\nОбработано точек: {result.Processed}");
            if (result.SkippedLocked > 0)
                ed.WriteMessage($"\nПропущено заблокированных точек: {result.SkippedLocked}");
        }

        private void ReportResult(Editor ed, CogoPointElevationService.Result result)
        {
            ed.WriteMessage("\n--- Смещение высот точек COGO завершено ---");

            if (result.Processed == 0)
            {
                ed.WriteMessage("\nНе найдено ни одной точки COGO для обработки.");
                return;
            }

            ed.WriteMessage($"\nОбработано точек: {result.Processed}");
            if (result.SkippedLocked > 0)
                ed.WriteMessage($"\nПропущено заблокированных точек: {result.SkippedLocked}");
        }
    }
}