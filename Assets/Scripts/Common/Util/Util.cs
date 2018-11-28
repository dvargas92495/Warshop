using UnityEngine;
using UnityEngine.Events;

public class Util
{

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

    public delegate U ReturnAction<T, U>(T arg);
    public delegate V ReturnAction<T, U, V>(T arg, U arg1);
    public delegate W ReturnAction<T, U, V, W>(T arg, U arg1, V arg2);

    public class Dictionary<T, U>
    {
        private T[] keys;
        private U[] vals;
        private bool[] entries;

        public Dictionary (int length)
        {
            keys = new T[length];
            vals = new U[length];
            entries = new bool[length];
        }

        public void Add(T key, U val)
        {
            int i = FindIndex(entries, e => !e);
            entries[i] = true;
            keys[i] = key;
            vals[i] = val;
        }

        public void Put(T key, U val)
        {
            vals[GetIndex(key)] = val;
        }

        public bool Contains(T key)
        {
            return Util.Contains(keys, key);
        }

        public bool ContainsValue(U val)
        {
            return Util.Contains(vals, val);
        }

        public U Get(T key)
        {
            return vals[GetIndex(key)];
        }

        public T GetKey(U val)
        {
            return keys[FindIndex(vals, val)];
        }

        public int GetLength()
        {
            return entries.Length;
        }

        public int GetIndex(T key)
        {
            return FindIndex(keys, key);
        }

        public void ForEach(UnityAction<T,U> callback)
        {
            Util.ForEach(keys, vals, callback);
        }

        public void ForEachValue(UnityAction<U> callback)
        {
            Util.ForEach(vals, callback);
        }

        public V ReduceEachValue<V>(V initialValue, ReturnAction<V,U,V> callback)
        {
            return Reduce(vals, initialValue, callback);
        }

        public void Remove(T key)
        {
            int i = GetIndex(key);
            vals[i] = default(U);
            keys[i] = default(T);
            entries[i] = false;
        }

        public bool AnyValue(ReturnAction<U,bool> callback)
        {
            foreach(U val in vals)
            {
                if (callback(val)) return true;
            }
            return false;
        }
    }
    
    public class Tuple<L,R>
    {
        L _left;
        R _right;

        public Tuple(L left, R right)
        {
            _left = left;
            _right = right;
        }

        public L GetLeft()
        {
            return _left;
        }

        public R GetRight()
        {
            return _right;
        }
    }

    public class List<T>
    {
        private T[] items;

        public List()
        {
            items = new T[0];
        }

        public List(params T[] i)
        {
            items = i;
        }

        public List(List<T> l)
        {
            items = l.items;
        }

        public T Get(int i)
        {
            return items[i];
        }

        public List<T> Get(int i, int size)
        {
            return new List<T>(Util.Get(items, i, size));
        }

        public T[] ToArray()
        {
            return Util.Get(items, 0, items.Length);
        }

        public List<R> Map<R>(ReturnAction<T, R> callback)
        {
            return new List<R>(Util.Map(items, callback));
        }

        public List<R> MapFlattened<R>(ReturnAction<T, List<R>> callback)
        {
            ReturnAction<T, R[]> modifiedCallback = i => callback(i).items;
            return new List<R>(Flatten(Util.Map(items, modifiedCallback)));
        }

        public List<T> Filter(ReturnAction<T,bool> callback)
        {
            return new List<T>(Util.Filter(items, callback));
        }

        public T Find(ReturnAction<T, bool> callback)
        {
            return Util.Find(items, callback);
        }

        public int FindIndex(T item)
        {
            return Util.FindIndex(items, item);
        }

        public void RemoveAt(int i)
        {
            items = Util.RemoveAt(items, i);
        }

        public void RemoveAt(int i, int size)
        {
            items = Util.RemoveAt(items, i, size);
        }

        public void RemoveAll(ReturnAction<T, bool> callback)
        {
            items = Util.Filter(items, (T item) => !callback(item));
        }

        public string ToString(string delim)
        {
            return ToArrayString(items, delim);
        }

        public List<T> Concat(List<T> newItems)
        {
            return new List<T>(Util.Add(items, newItems.items));
        }

        public void Add(T item)
        {
            items = Util.Add(items, item);
        }

        public void Add(List<T> l)
        {
            items = Util.Add(items, l.items);
        }

        public void Add(T item, int i)
        {
            items = Util.Add(items, item, i);
        }

        public void Add(List<T> l, int i)
        {
            items = Util.Add(items, l.items, i);
        }

        public bool Contains(T item)
        {
            return Util.Contains(items, item);
        }

        public void ForEach(UnityAction<T> callback)
        {
            Util.ForEach(items, callback);
        }

        public void Clear()
        {
            items = new T[0];
        }

        public int GetLength()
        {
            return items.Length;
        }

        public bool IsEmpty()
        {
            return items.Length == 0;
        }
    }

    public class Set<T>
    {
        int count = 0;
        T[] items;
        public Set(int size)
        {
            items = new T[size];
        }

        public Set(T[] other)
        {
            items = other;
            count = other.Length;
        }

        public Set(Set<T> other)
        {
            items = other.items;
            count = other.count;
        }

        public void Add(T item)
        {
            items[count] = item;
            count++;
        }

        public void Remove(T item)
        {
            items = Util.Remove(items, item);
            count--;
        }

        public Set<T> Filter(ReturnAction<T, bool> callback)
        {
            return new Set<T>(Util.Filter(items, callback));
        }

        public void ForEach(UnityAction<T> callback)
        {
            Util.ForEach(Util.Filter(items, i => i != null), callback);
        }
    }

    public static List<T> ToList<T>(T[] arr)
    {
        return new List<T>(arr);
    }

    internal static int[] Int(int length)
    {
        int[] arr = new int[length];
        for (int i=0;i<length;i++)
        {
            arr[i] = i;
        }
        return arr;
    }

    internal static void ForEach(int length, UnityAction<int> callback)
    {
        ForEach(Int(length), callback);
    }

    internal static void ForEach<T>(T[] arr, UnityAction<T> callback)
    {
        foreach(T item in arr)
        {
            callback(item);
        }
    }
    
    internal static void ForEach<T, U>(T[] arr, U[] arr1, UnityAction<T, U> callback)
    {
        int length = Mathf.Min(arr.Length, arr1.Length);
        for (int i = 0; i < length; i++)
        {
            callback(arr[i], arr1[i]);
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
        newArr[index] = item;
        return newArr;
    }

    internal static T[] Add<T>(T[] arr, T item, int i)
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

    internal static T[] Add<T>(T[] arr, T[] arr2, int index)
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

    internal static T[] Add<T>(T[] arr, T[] items)
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

    internal static T[] Remove<T>(T[] arr, T item)
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

    internal static T[] RemoveAt<T>(T[] arr, int index)
    {
        return RemoveAt(arr, index, 1);
    }

    internal static T[] RemoveAt<T>(T[] arr, int index, int size)
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

    internal static int FindIndex<T>(T[] arr, ReturnAction<T, bool> callback)
    {
        for (int i=0; i<arr.Length; i++)
        {
            if (callback(arr[i])) return i;
        }
        return -1;
    }

    internal static int FindIndex<T>(T[] arr, T item)
    {
        return FindIndex(arr, i => i.Equals(item));
    }

    internal static T Reduce<T,U>(U[] arr, T initialValue, ReturnAction<T, U, T> callback)
    {
        T val = initialValue;
        ForEach(arr, (U item) =>
        {
            val = callback(val, item);
        });
        return val;
    }

    internal static int Count<T>(T[] arr, ReturnAction<T, bool> callback)
    {
        int count = 0;
        foreach(T item in arr)
        {
            if (callback(item)) count++;
        }
        return count;
    }

    internal static T[] Filter<T>(T[] arr, ReturnAction<T, bool> callback)
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

    internal static T[] Flatten<T>(T[][] arr)
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

    internal static T[] Get<T>(T[] arr, int index, int size)
    {
        T[] returnArr = new T[size];
        for (int i = index; i < index + size; i++) returnArr[i - index] = arr[i];
        return returnArr;
    }

    internal static string ToArrayString<T>(T[] arr, string delim)
    {
        string s = "";
        foreach(T item in arr)
        {
            s += item.ToString();
            s += delim;
        }
        return s;
    }

    internal static bool Contains<T>(T[] arr, T item)
    {
        foreach(T i in arr)
        {
            if (i.Equals(item)) return true;
        }
        return false;
    }

    internal static int Max(int a, int b)
    {
        if (a > b) return a;
        return b;
    }
}
