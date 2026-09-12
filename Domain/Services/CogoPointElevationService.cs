using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using MyCivilPlugin.Domain.Models;

namespace MyCivilPlugin.Domain.Services
{
    internal static class CogoPointElevationService
    {
        public class Result
        {
            public int Processed { get; set; }
            public int SkippedLocked { get; set; }
        }

        /// <summary>
        /// Применяет случайное смещение к высотам указанных точек COGO.
        /// </summary>
        public static Result RandomizeElevation(
            Transaction ts,
            IReadOnlyList<ObjectId> pointIds,
            ElevationOffsetOptions options)
        {
            Random rnd = new Random();
            Result result = new Result();

            foreach (ObjectId pointId in pointIds)
            {
                CogoPoint cogoPoint = ts.GetObject(pointId, OpenMode.ForWrite) as CogoPoint;
                if (cogoPoint == null) continue;

                if (cogoPoint.IsLocked)
                {
                    result.SkippedLocked++;
                    continue;
                }

                double randomOffset = options.MinOffset
                    + (rnd.NextDouble() * (options.MaxOffset - options.MinOffset));

                cogoPoint.Elevation += randomOffset;
                result.Processed++;
            }

            return result;
        }
    }
}