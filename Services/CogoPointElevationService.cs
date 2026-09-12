using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using MyCivilPlugin.Models;

namespace MyCivilPlugin.Services
{
    internal static class CogoPointElevationService
    {
        public class Result
        {
            public int Processed { get; set; }
            public int SkippedLocked { get; set; }
        }

        public static Result RandomizeElevation(
            CivilDocument civilDoc,
            Transaction ts,
            ElevationOffsetOptions options)
        {
            Random rnd = new Random();
            Result result = new Result();

            foreach (ObjectId pointId in civilDoc.CogoPoints)
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