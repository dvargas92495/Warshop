public class Tuple<L, R>
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

    public void SetRight(R right)
    {
        _right = right;
    }

    public override string ToString()
    {
        return "{ " + _left.ToString() + " | " + _right.ToString() + " }"; 
    }

    public override bool Equals(object obj)
    {
        if (!obj.GetType().Equals(GetType())) return false;
        Tuple<L,R> other = (Tuple<L,R>)obj;
        return _left.Equals(other._left) && _right.Equals(other._right);
    }

    public override int GetHashCode()
    {
        return _left.GetHashCode() + _right.GetHashCode();
    }
}