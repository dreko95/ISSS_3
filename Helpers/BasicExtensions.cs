using System.Collections.Generic;
using System.Linq;

namespace ISSS3.Helpers;

public static class BasicExtensions {
    public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
    public static bool IsNotNullOrEmpty(this string str) => !string.IsNullOrEmpty(str);
    public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

    public static bool IsEmpty<T>(this IEnumerable<T> collection) => collection == null || !collection.Any();
}