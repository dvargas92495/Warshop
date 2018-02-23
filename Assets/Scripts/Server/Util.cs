using UnityEngine;

class Util
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
}
