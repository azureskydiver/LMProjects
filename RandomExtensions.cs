using System;

namespace Lobstermania
{
    public static class RandomExtensions
    {
        public static void Shuffle<T>(this Random rand, T[] items)
        {
            for (int i = 0; i < items.Length - 1; i++)
            {
                int j = rand.Next(i, items.Length);
                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }
    }
}
