public class Counter
{
    int value;

    public Counter(int v)
    {
        value = v;
    }

    public void Decrement()
    {
        value--;
    }

    public int Get()
    {
        return value;
    }

    public override string ToString()
    {
        return value.ToString(); 
    }

    public override bool Equals(object obj)
    {
        if (!obj.GetType().Equals(GetType())) return false;
        Counter other = (Counter)obj;
        return other.value == value;
    }

    public override int GetHashCode()
    {
        return value;
    }
}
