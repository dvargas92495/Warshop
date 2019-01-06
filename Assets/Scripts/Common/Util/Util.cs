using UnityEngine;
using UnityEngine.Events;

public class Util
{
    private static readonly Logger log = new Logger(typeof(Util).ToString());

    public static List<U> ToList<U>(params U[] items)
    {
        return new List<U>(items);
    }

    public static List<int> ToIntList(int length)
    {
        return ToList(Int(length));
    }

    protected static int[] Int(int length)
    {
        int[] arr = new int[length];
        for (int i=0;i<length;i++)
        {
            arr[i] = i;
        }
        return arr;
    }

    protected static void ForEach(int length, UnityAction<int> callback)
    {
        ForEach(Int(length), callback);
    }

    protected static void ForEach<T>(T[] arr, UnityAction<T> callback)
    {
        foreach(T item in arr)
        {
            callback(item);
        }
    }

    protected static void ForEach<T, U>(T[] arr, U[] arr1, UnityAction<T, U> callback)
    {
        int length = Mathf.Min(arr.Length, arr1.Length);
        for (int i = 0; i < length; i++)
        {
            callback(arr[i], arr1[i]);
        }
    }

    public static T[] Add<T>(T[] arr, T item)
    {
        T[] newArr = new T[arr.Length + 1];
        int index = 0;
        ForEach(arr, (T oldItem) =>
        {
            newArr[index] = oldItem;
            index++;
        });
        newArr[index] = item;
        return newArr;
    }

    protected static T[] Add<T>(T[] arr, T item, int i)
    {
        T[] newArr = new T[arr.Length + 1];
        int index = 0;
        ForEach(arr, (T oldItem) =>
        {
            if (i == index)
            {
                newArr[index] = item;
                index++;
            }
            newArr[index] = oldItem;
            index++;
        });
        if (i == index) newArr[index] = item;
        return newArr;
    }

    protected static T[] Add<T>(T[] arr, T[] arr2, int index)
    {
        T[] newArr = new T[arr.Length + arr2.Length];
        for (int i = 0; i < index; i++)
        {
            newArr[i] = arr[i];
        }
        for (int i = index; i < index + arr2.Length; i++)
        {
            newArr[i] = arr2[i - index];
        }
        for (int i = index + arr2.Length; i < newArr.Length; i++)
        {
            newArr[i] = arr[i - arr2.Length];
        }
        return newArr;
    }

    protected static T[] Add<T>(T[] arr, T[] items)
    {
        T[] newArr = new T[arr.Length + items.Length];
        int index = 0;
        ForEach(arr, (T oldItem) =>
        {
            newArr[index] = oldItem;
            index++;
        });
        ForEach(items, (T newItem) =>
        {
            newArr[index] = newItem;
            index++;
        });
        return newArr;
    }

    protected static T[] Remove<T>(T[] arr, T item)
    {
        T[] newArr = new T[arr.Length - 1];
        int index = 0;
        ForEach(arr, (T oldItem) =>
        {
            if (!oldItem.Equals(item))
            {
                newArr[index] = oldItem;
                index++;
            }
        });
        return newArr;
    }

    protected static T[] RemoveAt<T>(T[] arr, int index)
    {
        return RemoveAt(arr, index, 1);
    }

    protected static T[] RemoveAt<T>(T[] arr, int index, int size)
    {
        T[] newArr = new T[arr.Length - size];
        for (int i = 0; i < index; i++)
        {
            newArr[i] = arr[i];
        }
        for (int i = index + size; i < arr.Length; i++)
        {
            newArr[i - size] = arr[i];
        }
        return newArr;
    }

    protected static U[] Map<T, U>(T[] arr, ReturnAction<T, U> mapper)
    {
        U[] newArr = new U[arr.Length];
        int index = 0;
        ForEach(arr, (T oldItem) =>
        {
            newArr[index] = mapper(oldItem);
            index++;
        });
        return newArr;
    }

    public static W[] Map<T, U, V, W>(T[] arr, U[] arr1, V[] arr2, ReturnAction<T, U, V, W> callback)
    {
        W[] returnArr = new W[Mathf.Min(arr.Length, arr1.Length, arr2.Length)];
        for (int i = 0; i < returnArr.Length; i++)
        {
            returnArr[i] = callback(arr[i], arr1[i], arr2[i]);
        }
        return returnArr;
    }

    protected static bool Any<T>(T[] arr, ReturnAction<T, bool> callback)
    {
        foreach (T item in arr)
        {
            if (callback(item)) return true;
        }
        return false;
    }

    protected static T Find<T>(T[] arr, ReturnAction<T, bool> callback)
    {
        foreach (T item in arr)
        {
            if (callback(item)) return item;
        }
        return default(T);
    }

    protected static int FindIndex<T>(T[] arr, ReturnAction<T, bool> callback)
    {
        for (int i=0; i<arr.Length; i++)
        {
            if (callback(arr[i])) return i;
        }
        return -1;
    }

    protected static int FindIndex<T>(T[] arr, T item)
    {
        int index = FindIndex(arr, i => i.Equals(item));
        if (index == -1)
        {
            log.Error("Could not find item " + item + " in " + ToArrayString(arr, ","));
        }
        return index;
    }

    protected static T Reduce<T,U>(U[] arr, T initialValue, ReturnAction<T, U, T> callback)
    {
        T val = initialValue;
        ForEach(arr, (U item) =>
        {
            val = callback(val, item);
        });
        return val;
    }

    protected static int Count<T>(T[] arr, ReturnAction<T, bool> callback)
    {
        int count = 0;
        foreach(T item in arr)
        {
            if (callback(item)) count++;
        }
        return count;
    }

    protected static T[] Filter<T>(T[] arr, ReturnAction<T, bool> callback)
    {
        T[] newArr = new T[arr.Length];
        int count = 0;
        foreach(T item in arr)
        {
            if (callback(item))
            {
                newArr[count] = item;
                count++;
            }
        }
        T[] returnArr = new T[count];
        for (int index = 0; index<returnArr.Length; index++)
        {
            returnArr[index] = newArr[index];
        }
        return returnArr;
    }

    protected static T[] Flatten<T>(T[][] arr)
    {
        int count = 0;
        foreach (T[] subarr in arr)
        {
            count += subarr.Length;
        }
        T[] returnArr = new T[count];
        int i = 0;
        foreach (T[] subarr in arr)
        {
            foreach (T item in subarr)
            {
                returnArr[i] = item;
                i++;
            }
        }
        return returnArr;
    }

    protected static T[] Get<T>(T[] arr, int index, int size)
    {
        T[] returnArr = new T[size];
        for (int i = index; i < index + size; i++) returnArr[i - index] = arr[i];
        return returnArr;
    }

    protected static string ToArrayString<T>(T[] arr, string delim)
    {
        string s = "";
        foreach(T item in arr)
        {
            s += item.ToString();
            s += delim;
        }
        return s;
    }

    protected static bool Contains<T>(T[] arr, T item)
    {
        foreach(T i in arr)
        {
            if (i.Equals(item)) return true;
        }
        return false;
    }

    protected static T[] Reverse<T>(T[] arr)
    {
        T[] returnArr = new T[arr.Length];
        for (int i = arr.Length - 1; i >= 0; i--)
        {
            returnArr[arr.Length - 1 - i] = arr[i];
        }
        return returnArr;
    }
}
