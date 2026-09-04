using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Oxtail.Utils
{
    public static class CollectionExtensions
    {
        //public static T GetRandomElement<T>(this ICollection<T> collection)
        //{
        //    if (collection == null || collection.Count == 0)
        //        throw new InvalidOperationException($"Collection {nameof(collection)} is empty");

        //    var collectionArray = collection.ToArray();
            
        //    return collectionArray[UnityEngine.Random.Range(0, collectionArray.Length)];
        //}

        public static T GetRandomElement<T>(this IEnumerable<T> collection)
        {
            if (collection == null || collection.Count() == 0)
                throw new InvalidOperationException($"Collection {nameof(collection)} is empty");

            int index = UnityEngine.Random.Range(0, collection.Count());
            return collection.ElementAt(index);
        }

        public static T GetRandomElement<T>(this Array collection)
        {
            if (collection == null || collection.Length == 0)
                throw new InvalidOperationException($"Collection {nameof(collection)} is empty");

            int index = UnityEngine.Random.Range(0, collection.Length);
            return (T)collection.GetValue(index);
        }

        public static T GetRandomKey<T, T2>(this ICollection<KeyValuePair<T, T2>> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new InvalidOperationException($"Collection {nameof(collection)} is empty");

            int index = UnityEngine.Random.Range(0, collection.Count);
            return collection.ElementAt(index).Key;
        }

        public static T2 GetRandomValue<T, T2>(this ICollection<KeyValuePair<T, T2>> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new InvalidOperationException($"Collection {nameof(collection)} is empty");

            int index = UnityEngine.Random.Range(0, collection.Count);
            return collection.ElementAt(index).Value;
        }
    }
}
