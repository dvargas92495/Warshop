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
        if (Contains(key)) vals[GetIndex(key)] = val;
        else Add(key, val);
    }

    public bool Contains(T key)
    {
        return Contains(keys, key) && entries[GetIndex(key)];
    }

    public bool ContainsValue(U val)
    {
        return Contains(vals, val);
    }

    public U Get(T key)
    {
        U item = vals[GetIndex(key)];
        if (item == null) throw new ZException("Couldn't get value for key {0}. In keys {1} but not in vals {2}", key, ToArrayString(keys), ToArrayString(vals));
        return item;
    }

    public U Get(ReturnAction<U, bool> callback)
    {
        return Find(vals, callback);
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
        try
        {
            return FindIndex(keys, key);
        }
        catch (ZException)
        {
            throw new ZException("Could not get index with key {0} from keys[{1}]", key, ToArrayString(keys));
        }
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
        if (Contains(key))
        {
            int i = GetIndex(key);
            vals[i] = default;
            keys[i] = default;
            entries[i] = false;
        }
    }

    public List<T> ToKeyListFiltered(ReturnAction<T, bool> callback)
    {
        return new List<T>(Filter(keys, callback));
    }

    public List<U> ToValueListFiltered(ReturnAction<U, bool> callback)
    {
        return new List<U>(Filter(vals, callback));
    }

    public List<U> ToValueList()
    {
        return new List<U>(vals);
    }

    public bool AnyValue(ReturnAction<U, bool> callback)
    {
        return Any(vals, callback);
    }

    public int CountValues(ReturnAction<U, bool> callback)
    {
        return Count(vals, callback);
    }

    public override string ToString()
    {
        string s = "{";
        ForEach((k, v) => s += string.Format("{0}:{1},", k, v));
        return s.Substring(0, s.Length - 1) + "}";
    }
}
