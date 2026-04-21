using System.Collections;
using System.Reflection;

namespace GVisionWpf.Utils
{
    public static class ObjectComparer
    {
        public static Dictionary<string, Tuple<string, string>> Compare<T>(T previous, T current)
        {
            var differences = new Dictionary<string, Tuple<string, string>>();

            if (previous == null || current == null)
            {
                throw new ArgumentNullException("Both objects must be non-null.");
            }

            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                object previousValue = property.GetValue(previous);
                object currentValue = property.GetValue(current);

                if (IsSimpleType(property.PropertyType))
                {
                    if (!Equals(previousValue, currentValue))
                    {
                        differences.Add(property.Name, new Tuple<string, string>(
                            previousValue?.ToString() ?? "null",
                            currentValue?.ToString() ?? "null"));
                    }
                }
                else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    if (!AreCollectionsEqual((IEnumerable)previousValue, (IEnumerable)currentValue))
                    {
                        differences.Add(property.Name, new Tuple<string, string>(
                            CollectionToString((IEnumerable)previousValue),
                            CollectionToString((IEnumerable)currentValue)));
                    }
                }
                else
                {
                    var nestedDifferences = CompareNestedObjects(property.Name, previousValue, currentValue);
                    foreach (var nestedDifference in nestedDifferences)
                    {
                        differences.Add(nestedDifference.Key, nestedDifference.Value);
                    }
                }
            }

            return differences;
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(decimal)
                   || type == typeof(DateTime);
        }

        private static bool AreCollectionsEqual(IEnumerable previous, IEnumerable current)
        {
            if (previous == null && current == null) return true;
            if (previous == null || current == null) return false;

            var previousList = new List<object>();
            var currentList = new List<object>();

            foreach (var item in previous) previousList.Add(item);
            foreach (var item in current) currentList.Add(item);

            if (previousList.Count != currentList.Count) return false;

            previousList.Sort();
            currentList.Sort();

            for (int i = 0; i < previousList.Count; i++)
            {
                if (!Equals(previousList[i], currentList[i])) return false;
            }

            return true;
        }

        private static string CollectionToString(IEnumerable collection)
        {
            if (collection == null) return "null";
            var items = new List<string>();
            foreach (var item in collection)
            {
                items.Add(item?.ToString() ?? "null");
            }

            return $"[{string.Join(", ", items)}]";
        }

        private static Dictionary<string, Tuple<string, string>> CompareNestedObjects(string parentPropertyName, object previous, object current)
        {
            var differences = new Dictionary<string, Tuple<string, string>>();

            if (previous == null || current == null)
            {
                if (previous != current) // one is null, and the other is not
                {
                    differences.Add(parentPropertyName, new Tuple<string, string>(
                        previous?.ToString() ?? "null",
                        current?.ToString() ?? "null"));
                }

                return differences;
            }

            Type type = previous.GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                object previousValue = property.GetValue(previous);
                object currentValue = property.GetValue(current);

                if (IsSimpleType(property.PropertyType))
                {
                    if (!Equals(previousValue, currentValue))
                    {
                        differences.Add($"{parentPropertyName}.{property.Name}", new Tuple<string, string>(
                            previousValue?.ToString() ?? "null",
                            currentValue?.ToString() ?? "null"));
                    }
                }
                else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    if (!AreCollectionsEqual((IEnumerable)previousValue, (IEnumerable)currentValue))
                    {
                        differences.Add($"{parentPropertyName}.{property.Name}", new Tuple<string, string>(
                            CollectionToString((IEnumerable)previousValue),
                            CollectionToString((IEnumerable)currentValue)));
                    }
                }
                else
                {
                    var nestedDifferences = CompareNestedObjects($"{parentPropertyName}.{property.Name}", previousValue, currentValue);
                    foreach (var nestedDifference in nestedDifferences)
                    {
                        differences.Add(nestedDifference.Key, nestedDifference.Value);
                    }
                }
            }

            return differences;
        }
    }
}
