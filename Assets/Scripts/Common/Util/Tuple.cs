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
}