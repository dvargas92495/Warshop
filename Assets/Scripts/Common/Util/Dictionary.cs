using UnityEngine.Events;

public class Dictionary<T, U> : Util
{
    private T[] keys;
    private U[] vals;
    private bool[] entries;

    public Dictionary(int length)
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
        return Contains(keys, key);
    }

    public bool ContainsValue(U val)
    {
        return Contains(vals, val);
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

    public void ForEach(UnityAction<T, U> callback)
    {
        ForEach(keys, vals, callback);
    }

    public void ForEachValue(UnityAction<U> callback)
    {
        ForEach(vals, callback);
    }

    public V ReduceEachValue<V>(V initialValue, ReturnAction<V, U, V> callback)
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

    public bool AnyValue(ReturnAction<U, bool> callback)
    {
        foreach (U val in vals)
        {
            if (callback(val)) return true;
        }
        return false;
    }
}
