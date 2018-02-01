using System;
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

    internal static Command.Rotate Flip(Command.Rotate r)
    {
        return new Command.Rotate(Flip(r.direction));
    }

    internal static Command.Direction Flip(Command.Direction d)
    {
        switch (d)
        {
            case Command.Direction.UP:
                return Command.Direction.DOWN;
            case Command.Direction.DOWN:
                return Command.Direction.UP;
            case Command.Direction.LEFT:
                return Command.Direction.RIGHT;
            case Command.Direction.RIGHT:
                return Command.Direction.LEFT;
            default:
                return d;
        }
    }

    internal static Command Flip(Command c)
    {
        if (c is Command.Rotate) return Flip((Command.Rotate)c);
        else if (c is Command.Move) return Flip((Command.Move)c);
        else return c;
    }
}
