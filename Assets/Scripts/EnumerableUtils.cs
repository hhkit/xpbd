using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class EnumerableUtils
{
  public static Span<TResult> Reinterpret<TSource, TResult>(this TSource[] arr)
     where TResult : struct
     where TSource : struct
  {
    return MemoryMarshal.Cast<TSource, TResult>(arr.AsSpan());
  }


  public static IEnumerable<TSource> Duplicate<TSource>(this IEnumerable<TSource> source, int count)
  {
    return source.Select((v, _) =>
    {
      var res = new TSource[count];
      Array.Fill(res, v);
      return res;
    })
      .SelectMany(i => i);
  }

  public static IEnumerable<TResult> SelectTwo<TSource, TResult>(this IEnumerable<TSource> source,
                                                                   Func<TSource, TSource, TResult> selector)
  {
    if (source == null) throw new ArgumentNullException(nameof(source));
    if (selector == null) throw new ArgumentNullException(nameof(selector));

    return SelectTwoImpl(source, selector);
  }

  private static IEnumerable<TResult> SelectTwoImpl<TSource, TResult>(this IEnumerable<TSource> source,
                                                                      Func<TSource, TSource, TResult> selector)
  {
    using (var iterator = source.GetEnumerator())
    {
      var item2 = default(TSource);
      var i = 0;
      while (iterator.MoveNext())
      {
        var item1 = item2;
        item2 = iterator.Current;
        i++;

        if (i >= 2)
        {
          yield return selector(item1, item2);
        }
      }
    }
  }
}