using UnityEngine.Events;

public class Set<T> : Util
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
        items = Remove(items, item);
        count--;
    }

    public Set<T> Filter(ReturnAction<T, bool> callback)
    {
        return new Set<T>(Filter(items, callback));
    }

    public void ForEach(UnityAction<T> callback)
    {
        ForEach(Filter(items, i => i != null), callback);
    }
}