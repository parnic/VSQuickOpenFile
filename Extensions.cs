using System.Collections.Generic;

namespace PerniciousGames.OpenFileInSolution
{
    public static class Extensions
    {
        public static void AddRangeUnique<T>(this List<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                }
            }
        }
    }
}
