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

    internal static byte Flip(byte d)
    {
        switch (d)
        {
            case Command.Move.UP:
                return Command.Move.DOWN;
            case Command.Move.DOWN:
                return Command.Move.UP;
            case Command.Move.LEFT:
                return Command.Move.RIGHT;
            case Command.Move.RIGHT:
                return Command.Move.LEFT;
            default:
                return d;
        }
    }

    internal static Command Flip(Command c)
    {
        if (c is Command.Move) return Flip((Command.Move)c);
        else return c;
    }
}
