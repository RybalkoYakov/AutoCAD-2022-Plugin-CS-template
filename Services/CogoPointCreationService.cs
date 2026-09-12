using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using MyCivilPlugin.Models;

namespace MyCivilPlugin.Services
{
    internal static class CogoPointCreationService
    {
        public class Result
        {
            public int CreatedCount { get; set; }
            public ObjectId GroupId { get; set; }
        }

        /// <summary>
        /// Создаёт новые точки COGO со случайным смещением от выбранных
        /// и помещает их в новую группу точек.
        /// </summary>
        public static Result CreateOffsetPoints(
            CivilDocument civilDoc,
            Transaction ts,
            List<ObjectId> selectedPoints,
            PointCreationOptions options)
        {
            Result result = new Result();
            Random rnd = new Random();

            // 1. Создаём новую группу точек
            ObjectId groupId = civilDoc.PointGroups.Add(options.GroupName);
            result.GroupId = groupId;
            PointGroup group = ts.GetObject(groupId, OpenMode.ForWrite) as PointGroup;

            // 2. Создаём новые точки
            CogoPointCollection cogoPoints = civilDoc.CogoPoints;
            List<uint> newPointNumbers = new List<uint>();

            foreach (ObjectId pointId in selectedPoints)
            {
                CogoPoint original = ts.GetObject(pointId, OpenMode.ForRead) as CogoPoint;
                if (original == null) continue;

                // Случайные смещения в заданных диапазонах
                double dx = options.MinOffsetX + rnd.NextDouble() * (options.MaxOffsetX - options.MinOffsetX);
                double dy = options.MinOffsetY + rnd.NextDouble() * (options.MaxOffsetY - options.MinOffsetY);
                double dz = options.MinOffsetZ + rnd.NextDouble() * (options.MaxOffsetZ - options.MinOffsetZ);

                Point3d newLocation = new Point3d(
                    original.Easting + dx,
                    original.Northing + dy,
                    original.Elevation + dz);

                ObjectId newPointId = cogoPoints.Add(newLocation, false);
                CogoPoint newPoint = ts.GetObject(newPointId, OpenMode.ForRead) as CogoPoint;
                if (newPoint != null)
                {
                    newPointNumbers.Add(newPoint.PointNumber);
                }
                result.CreatedCount++;
            }

            // 3. Настраиваем запрос группы — включаем только что созданные точки по номерам
            if (newPointNumbers.Count > 0)
            {
                StandardPointGroupQuery query = new StandardPointGroupQuery();
                query.IncludeNumbers = BuildNumberList(newPointNumbers);
                group.SetQuery(query);
                group.Update();
            }

            return result;
        }

        /// <summary>
        /// Формирует строку вида "1,2,3-7,10" для StandardPointGroupQuery.IncludeNumbers.
        /// </summary>
        private static string BuildNumberList(List<uint> numbers)
        {
            numbers.Sort();
            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < numbers.Count)
            {
                uint start = numbers[i];
                uint end = start;

                // Ищем непрерывный диапазон
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