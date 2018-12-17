﻿using System;
using UnityEngine;

public class TestBase
{

    internal static Game testgame;

    public void BeforeAllTests(string[] t1, string[] t2)
    {
        testgame = new Game();
        testgame.board = new Map(
            "5 8\n" +
            "A B V C D\n" +
            "W W P W W\n" +
            "W W W W W\n" +
            "W W W W W\n" +
            "W W W W W\n" +
            "W W W W W\n" +
            "W W p W W\n" +
            "a b V c d\n"
        );
        testgame.Join(t1, "primary", 1);
        testgame.Join(t2, "secondary", 2);
    }

    public void BeforeEachTest()
    {
        BeforeEachTest(new Dictionary<short, Vector2Int>(0));
    }

    public void BeforeEachTest(Dictionary<short, Vector2Int> pos)
    {
        Action<Game.Player, bool> reset = (Game.Player p, bool isPrimary) =>
        {
            p.battery = GameConstants.POINTS_TO_WIN;
            p.team.ForEach(r =>
            {
                r.health = r.startingHealth;
                r.position = pos.Contains(r.id) ? pos.Get(r.id) : Map.NULL_VEC;
                if (pos.Contains(r.id)) testgame.board.UpdateObjectLocation(r.position.x, r.position.y, r.id);
                else testgame.board.RemoveObjectLocation(r.id);
            });
        };
        reset(testgame.primary, true);
        reset(testgame.secondary, false);
    }

    internal static Command.Spawn SpawnCommand(byte d, short r)
    {
        Command.Spawn s = new Command.Spawn(d);
        s.robotId = r;
        return s;
    }

    internal static Command.Move MoveCommand(byte d, short r)
    {
        Command.Move m = new Command.Move(d);
        m.robotId = r;
        return m;
    }

    internal static Command.Attack AttackCommand(byte d, short r)
    {
        Command.Attack a = new Command.Attack(d);
        a.robotId = r;
        return a;
    }

    internal static List<GameEvent> SimulateCommands(params Command[] cmds)
    {
        List<Command> primaryCmds = new List<Command>();
        List<Command> secondaryCmds = new List<Command>();
        Array.ForEach(cmds, (Command c) =>
        {
            if (testgame.primary.team.Any(r => r.id == c.robotId)) primaryCmds.Add(c);
            else secondaryCmds.Add(c);
        });
        testgame.primary.StoreCommands(primaryCmds);
        testgame.secondary.StoreCommands(secondaryCmds);
        return new List<GameEvent>(testgame.CommandsToEvents().ToArray());
    }

}