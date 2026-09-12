namespace MyCivilPlugin.Models
{
    public class ElevationOffsetOptions
    {
        public double MinOffset { get; set; } = 0;
        public double MaxOffset { get; set; } = 0;

        public bool IsValid(out string error)
        {
            if (MinOffset > MaxOffset)
            {
                error = $"Минимальное значение ({MinOffset}) больше максимального ({MaxOffset}).";
                return false;
            }
            error = null;
            return true;
        }
    }
}