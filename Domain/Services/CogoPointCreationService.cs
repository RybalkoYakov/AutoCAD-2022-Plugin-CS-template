using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using MyCivilPlugin.Domain.Models;

namespace MyCivilPlugin.Domain.Services
{
    internal static class CogoPointCreationService
    {
        public class Result
        {
            public int CreatedCount { get; set; }
            public ObjectId GroupId { get; set; }
        }

        /// <summary>
        /// Создаёт новые точки COGO со случайным смещением от выбранных в осях текущей ПСК
        /// и помещает их в новую группу точек.
        /// </summary>
        /// <param name="ucsToWcs">Матрица перехода из ПСК в МСК (Editor.CurrentUserCoordinateSystem).</param>
        public static Result CreateOffsetPoints(
            CivilDocument civilDoc,
            Transaction ts,
            List<ObjectId> selectedPoints,
            PointCreationOptions options,
            Matrix3d ucsToWcs)
        {
            Result result = new Result();
            Random rnd = new Random();

            // 1. Создаём новую группу точек
            ObjectId groupId = civilDoc.PointGroups.Add(options.GroupName);
            result.GroupId = groupId;
            PointGroup group = ts.GetObject(groupId, OpenMode.ForWrite) as PointGroup;

            // 2. Готовим обратную матрицу (МСК -> ПСК)
            Matrix3d wcsToUcs = ucsToWcs.Inverse();

            // 3. Создаём новые точки
            CogoPointCollection cogoPoints = civilDoc.CogoPoints;
            List<uint> newPointNumbers = new List<uint>();

            foreach (ObjectId pointId in selectedPoints)
            {
                CogoPoint original = ts.GetObject(pointId, OpenMode.ForRead) as CogoPoint;
                if (original == null) continue;

                // Случайные смещения в осях ПСК
                double dx = options.MinOffsetX + rnd.NextDouble() * (options.MaxOffsetX - options.MinOffsetX);
                double dy = options.MinOffsetY + rnd.NextDouble() * (options.MaxOffsetY - options.MinOffsetY);
                double dz = options.MinOffsetZ + rnd.NextDouble() * (options.MaxOffsetZ - options.MinOffsetZ);

                // Координаты исходной точки из МСК -> в ПСК
                Point3d wcsPoint = new Point3d(original.Easting, original.Northing, original.Elevation);
                Point3d ucsPoint = wcsPoint.TransformBy(wcsToUcs);

                // Применяем смещение в осях ПСК
                Point3d shiftedUcs = new Point3d(ucsPoint.X + dx, ucsPoint.Y + dy, ucsPoint.Z + dz);

                // Обратно в МСК
                Point3d shiftedWcs = shiftedUcs.TransformBy(ucsToWcs);

                // Создаём точку
                ObjectId newPointId = cogoPoints.Add(shiftedWcs, false);
                result.CreatedCount++;

                // Записываем описание со ссылкой на исходную точку
                CogoPoint newPoint = ts.GetObject(newPointId, OpenMode.ForWrite) as CogoPoint;
                if (newPoint != null)
                {
                    newPoint.RawDescription = $"№{original.PointNumber}, \ndx: {dx:F3}, \ndy: {dy:F3}, \ndz: {dz:F3}";
                    newPointNumbers.Add(newPoint.PointNumber);
                }
            }

            // 4. Настраиваем запрос группы
            if (newPointNumbers.Count > 0)
            {
                StandardPointGroupQuery query = new StandardPointGroupQuery();
                query.IncludeNumbers = BuildNumberList(newPointNumbers);
                group.SetQuery(query);
                group.Update();
            }

            return result;
        }

        private static string BuildNumberList(List<uint> numbers)
        {
            numbers.Sort();
            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < numbers.Count)
            {
                uint start = numbers[i];
                uint end = start;

                while (i + 1 < numbers.Count && numbers[i + 1] == end + 1)
                {
                    end = numbers[i + 1];
                    i++;
                }

                if (sb.Length > 0) sb.Append(',');
                if (start == end)
                    sb.Append(start.ToString(CultureInfo.InvariantCulture));
                else
                    sb.Append(start.ToString(CultureInfo.InvariantCulture))
                      .Append('-')
                      .Append(end.ToString(CultureInfo.InvariantCulture));

                i++;
            }
            return sb.ToString();
        }
    }
}