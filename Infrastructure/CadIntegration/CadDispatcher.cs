using System;
using Autodesk.AutoCAD.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyCivilPlugin.Infrastructure.CadIntegration
{
    /// <summary>
    /// Единая точка маршалинга вызовов в главный поток AutoCAD/Civil 3D.
    /// </summary>
    public static class CadDispatcher
    {
        public static T Execute<T>(Func<T> func)
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                throw new InvalidOperationException("Нет активного документа AutoCAD.");

            T result = default(T);

            // Выполняем в главном потоке AutoCAD.
            // ExecuteInApplicationContext принимает ApplicationEventHandler (sender, args),
            // поэтому лямбда должна иметь два параметра.
            AcApp.DocumentManager.ExecuteInApplicationContext(
                data => { result = func(); },
                null);

            return result;
        }

        public static void Execute(Action action)
        {
            Execute<object>(() => { action(); return null; });
        }
    }
}