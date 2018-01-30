using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections.Generic;

public class Model
{

    private static Game testgame;

    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        testgame = new Game();
        testgame.board = new Map(
            "5 8\n" +
            "Q Q V Q Q\n" +
            "S W A W S\n" +
            "W W W W W\n" +
            "W W W W W\n" +
            "W W W W W\n" +
            "W W W W W\n" +
            "s W B W s\n" +
            "q q V q q\n"
        );
        testgame.Join(new string[]
        {
            "Bronze Grunt",
            "Silver Grunt",
            "Golden Grunt",
            "Platinum Grunt"
        }, "primary", 1);
        testgame.Join(new string[]
        {
            "Bronze Grunt",
            "Silver Grunt",
            "Golden Grunt",
            "Platinum Grunt"
        }, "secondary", 2);
    }

    public void BeforeEachTest()
    {
        BeforeEachTest(new Dictionary<short, Vector2Int>());
    }

    public void BeforeEachTest(Dictionary<short,Vector2Int> pos)
    {
        Action<Robot[], bool> reset = (Robot[] team, bool isPrimary) =>
        {
            Array.ForEach(team, (Robot r) =>
            {
                r.health = r.startingHealth;
                r.position = pos.ContainsKey(r.id) ? pos[r.id] : testgame.board.GetQueuePosition(r.queueSpot, isPrimary);
                testgame.board.UpdateObjectLocation(r.position.x, r.position.y, r.id);
            });
        };
        reset(testgame.primary.team, true);
        reset(testgame.secondary.team, false);
    }

    [Test]
	public void TestMoveSimpleMove() {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest();
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id),

            MoveCommand(Command.Direction.UP, secondaryBronze.id),
            MoveCommand(Command.Direction.UP, secondaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        events.ForEach((GameEvent e) =>
        {
            Assert.IsInstanceOf<GameEvent.Move>(e);
            Vector2Int expected = e.primaryRobotId == primaryBronze.id ? Vector2Int.up : Vector2Int.down;
            Assert.AreEqual(expected, ((GameEvent.Move)e).destinationPos - ((GameEvent.Move)e).sourcePos);
        });
    }

    [Test]
    public void TestMoveWallBlock()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest();
        Vector2Int expected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Direction.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(1, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[0]);
        Assert.AreEqual(expected, primaryBronze.position);
    }

    [Test]
    public void TestMoveQueueBlock()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest();
        Vector2Int expected = Vector2Int.up;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.DOWN, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[1]);
        Assert.AreEqual(expected, primaryBronze.position);
    }

    [Test]
    public void TestMoveBatteryBlock()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int>() { { primaryBronze.id, Vector2Int.one } });
        Vector2Int expected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Direction.LEFT, primaryBronze.id)
        );
        Assert.AreEqual(1, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[0]);
        Assert.AreEqual(expected, primaryBronze.position);
    }

    [Test]
    public void TestMovePush()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot primaryPlatinum = testgame.primary.team[3];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) },
            { primaryPlatinum.id, new Vector2Int(1,2) }
        });
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.RIGHT, primaryPlatinum.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOf<GameEvent.Push>(events[1]);
        Assert.AreNotEqual(primaryBronze.position, primaryPlatinum.position);
        Assert.AreEqual(Vector2Int.up, primaryPlatinum.position - primaryBronze.position);
    }

    [Test]
    public void TestMoveRobotBlockFacing()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot primaryPlatinum = testgame.primary.team[3];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) },
            { primaryPlatinum.id, new Vector2Int(1,2) }
        });
        Vector2Int expected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.DOWN, primaryPlatinum.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[1]);
        Assert.AreEqual(expected, primaryBronze.position);
    }

    [Test]
    public void TestModelRobotBlockSoft()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot primaryPlatinum = testgame.primary.team[3];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(0,2) },
            { primaryPlatinum.id, new Vector2Int(1,2) }
        });
        Vector2Int expected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.UP, primaryPlatinum.id),
            RotateCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[2]);
        Assert.AreEqual(expected, primaryBronze.position);
    }

    [Test]
    public void TestModelRobotBlockEachOther()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(0,2) },
            { secondaryBronze.id, new Vector2Int(2,2) }
        });
        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.UP, secondaryBronze.id),
            RotateCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.LEFT, primaryBronze.id),
            MoveCommand(Command.Direction.LEFT, secondaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[2]);
        Assert.IsInstanceOf<GameEvent.Block>(events[3]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveRobotBlockFacingWins()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(0,2) },
            { secondaryBronze.id, new Vector2Int(2,2) }
        });
        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = new Vector2Int(1,2);
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.LEFT, secondaryBronze.id),
            RotateCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.LEFT, primaryBronze.id),
            MoveCommand(Command.Direction.LEFT, secondaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveRobotBlockEachOtherWithPush()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot primaryPlatinum = testgame.primary.team[3];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(0,2) },
            { primaryPlatinum.id, new Vector2Int(1,2) },
            { secondaryBronze.id, new Vector2Int(3,2) }
        });
        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.UP, secondaryBronze.id),
            RotateCommand(Command.Direction.UP, primaryPlatinum.id),
            RotateCommand(Command.Direction.LEFT, primaryBronze.id),
            MoveCommand(Command.Direction.LEFT, primaryBronze.id),
            MoveCommand(Command.Direction.LEFT, secondaryBronze.id)
        );
        Assert.AreEqual(5, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[3]);
        Assert.IsInstanceOf<GameEvent.Block>(events[4]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveSwapPositions()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) },
            { secondaryBronze.id, new Vector2Int(1,2) }
        });
        Vector2Int primaryExpected = secondaryBronze.position;
        Vector2Int secondaryExpected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.UP, secondaryBronze.id),
            RotateCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.UP, secondaryBronze.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[2]);
        Assert.IsInstanceOf<GameEvent.Move>(events[3]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    private static Command.Rotate RotateCommand(Command.Direction d, short r)
    {
        Command.Rotate c = new Command.Rotate(d);
        c.robotId = r;
        return c;
    }

    private static Command.Move MoveCommand(Command.Direction d, short r)
    {
        Command.Move m = new Command.Move(d);
        m.robotId = r;
        return m;
    }

    private static List<GameEvent> SimulateCommands(params Command[] cmds)
    {
        List<Command> primaryCmds = new List<Command>();
        List<Command> secondaryCmds = new List<Command>();
        Array.ForEach(cmds, (Command c) =>
        {
            if (Array.Exists(testgame.primary.team, (Robot r) => r.id==c.robotId)) primaryCmds.Add(c);
            else secondaryCmds.Add(c);
        });
        testgame.primary.StoreCommands(primaryCmds);
        testgame.secondary.StoreCommands(secondaryCmds);
        return testgame.CommandsToEvents();
    }
}
