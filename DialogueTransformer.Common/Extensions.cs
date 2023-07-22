using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogueTransformer.Common
{
    public static class Extensions
    {
        /*
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> list, int totalSize, int chunkSize)
        {
            int i = 0;
            while (i < totalSize)
            {
                //if(i == 0)
                //    yield return list.Skip((totalSize % chunkSize) + chunkSize).Take((totalSize % chunkSize) + chunkSize).ToList();
                //else
                    yield return list.Skip(i).Take(chunkSize).ToList();
                i += chunkSize;
            }
        }
*/
        // convenience for "countable" lists
        /*
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this ICollection<T> list, int chunkSize)
        {
            return Chunk(list, list.Count, chunkSize);
        }
        */
        /*
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> list, int chunkSize)
        {
            return Chunk(list, list.Count(), chunkSize);
        }
        */

    }
}
