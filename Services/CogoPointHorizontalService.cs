using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using MyCivilPlugin.Models;

namespace MyCivilPlugin.Services
{
    /// <summary>
    /// Сервис для массового горизонтального смещения точек COGO в текущей ПСК.
    /// </summary>
    internal static class CogoPointHorizontalService
    {
        public class Result
        {
            public int Processed { get; set; }
            public int SkippedLocked { get; set; }
        }

        /// <summary>
        /// Применяет случайное смещение X/Y к точкам COGO в осях текущей ПСК.
        /// ВАЖНО: транзакция ts должна быть активной; Commit выполняет вызывающий код.
        /// </summary>
        /// <param name="ucsToWcs">Матрица перехода из ПСК в МСК (Editor.CurrentUserCoordinateSystem).</param>
        public static Result RandomizeHorizontal(
            CivilDocument civilDoc,
            Transaction ts,
            HorizontalOffsetOptions options,
            Matrix3d ucsToWcs)
        {
            Random rnd = new Random();
            Result result = new Result();

            Matrix3d wcsToUcs = ucsToWcs.Inverse();

            foreach (ObjectId pointId in civilDoc.CogoPoints)
            {
                CogoPoint cogoPoint = ts.GetObject(pointId, OpenMode.ForWrite) as CogoPoint;
                if (cogoPoint == null) continue;

                if (cogoPoint.IsLocked)
                {
                    result.SkippedLocked++;
                    continue;
                }

                // 1. Координаты точки в МСК -> в ПСК
                Point3d wcsPoint = new Point3d(cogoPoint.Easting, cogoPoint.Northing, cogoPoint.Elevation);
                Point3d ucsPoint = wcsPoint.TransformBy(wcsToUcs);

                // 2. Случайные смещения в осях ПСК
                double dx = options.MinOffsetX + (rnd.NextDouble() * (options.MaxOffsetX - options.MinOffsetX));
                double dy = options.MinOffsetY + (rnd.NextDouble() * (options.MaxOffsetY - options.MinOffsetY));

                Point3d shiftedUcs = new Point3d(ucsPoint.X + dx, ucsPoint.Y + dy, ucsPoint.Z);

                // 3. Обратно в МСК
                Point3d shiftedWcs = shiftedUcs.TransformBy(ucsToWcs);

                // 4. Записываем новые координаты (высоту не трогаем)
                cogoPoint.Easting = shiftedWcs.X;
                cogoPoint.Northing = shiftedWcs.Y;

                result.Processed++;
            }

            return result;
        }
    }
}