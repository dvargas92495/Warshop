public class Math
{
    public static int Max(int a, int b)
    {
        if (a > b) return a;
        return b;
    }

    public static int Log2(int x)
    {
        if (x <= 1) return 0;
        return 1 + Log2(x / 2);
    }
}
