using UnityEngine;
using UnityEngine.Events;

class Util
{
    public delegate U ReturnAction<T, U>(T arg);
    public delegate W ReturnAction<T, U, V, W>(T arg, U arg1, V arg2);

    internal static Vector2Int Flip(Vector2Int v)
    {
        return new Vector2Int(-v.x, -v.y);
    }

    internal static Command.Move Flip(Command.Move m)
    {
        return new Command.Move(Flip(m.direction));
    }

    internal static Command.Attack Flip(Command.Attack a)
    {
        return new Command.Attack(Flip(a.direction));
    }

    internal static byte Flip(byte d)
    {
        switch (d)
        {
            case Command.UP:
                return Command.DOWN;
            case Command.DOWN:
                return Command.UP;
            case Command.LEFT:
                return Command.RIGHT;
            case Command.RIGHT:
                return Command.LEFT;
            default:
                return d;
        }
    }

    internal static Command Flip(Command c)
    {
        if (c is Command.Move) return Flip((Command.Move)c);
        if (c is Command.Attack) return Flip((Command.Attack)c);
        else return c;
    }

    internal static void ChangeLayer(GameObject g, int l)
    {
        if (g.layer == l) return;
        g.layer = l;
        for (int i = 0; i < g.transform.childCount; i++)
        {
            ChangeLayer(g.transform.GetChild(i).gameObject, l);
        }
    }

    internal static void ForEach<T>(T[] arr, UnityAction<T> callback)
    {
        foreach(T item in arr)
        {
            callback(item);
        }
    }

    internal static T[] Add<T>(T[] arr, T item)
    {
        T[] newArr = new T[arr.Length + 1];
        int index = 0;
        ForEach(arr, (T oldItem) =>
        {
            newArr[index] = oldItem;
            index++;
        });
        return newArr;
    }

    internal static T[] Remove<T>(T[] arr, T item)
    {
        T[] newArr = new T[arr.Length - 1];
        int index = 0;
        ForEach(arr, (T oldItem) =>
        {
            if (oldItem.Equals(item))
            {
                newArr[index] = oldItem;
                index++;
            }
        });
        return newArr;
    }

    internal static U[] Map<T, U>(T[] arr, ReturnAction<T, U> mapper)
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

    internal static W[] Map<T, U, V, W>(T[] arr, U[] arr1, V[] arr2, ReturnAction<T, U, V, W> callback)
    {
        W[] returnArr = new W[Mathf.Min(arr.Length, arr1.Length, arr2.Length)];
        for (int i = 0; i < returnArr.Length; i++)
        {
            returnArr[i] = callback(arr[i], arr1[i], arr2[i]);
        }
        return returnArr;
    }

    internal static T Find<T>(T[] arr, ReturnAction<T, bool> callback)
    {
        foreach (T item in arr)
        {
            if (callback(item)) return item;
        }
        return default(T);
    }
}
