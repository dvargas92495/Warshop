using UnityEngine;
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
        Action<Game.Player, bool> reset = (Game.Player p, bool isPrimary) =>
        {
            p.battery = GameConstants.POINTS_TO_WIN;
            Array.ForEach(p.team, (Robot r) =>
            {
                r.health = r.startingHealth;
                r.position = pos.ContainsKey(r.id) ? pos[r.id] : testgame.board.GetQueuePosition(r.queueSpot, isPrimary);
                r.orientation = isPrimary ? Robot.Orientation.NORTH : Robot.Orientation.SOUTH;
                testgame.board.UpdateObjectLocation(r.position.x, r.position.y, r.id);
            });
        };
        reset(testgame.primary, true);
        reset(testgame.secondary, false);
    }

    [Test]
    public void TestRotateSimple()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int>() { { primaryBronze.id, Vector2Int.one } });
        Robot.Orientation expected = Robot.Orientation.EAST;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(1, events.Count);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[0]);
        Assert.AreEqual(expected, primaryBronze.orientation);
    }

    [Test]
    public void TestRotateSame()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int>() { { primaryBronze.id, Vector2Int.one } });
        Robot.Orientation expected = primaryBronze.orientation;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.UP, primaryBronze.id)
        );
        Assert.AreEqual(1, events.Count);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[0]);
        Assert.AreEqual(expected, primaryBronze.orientation);
    }

    [Test]
    public void TestRotateDouble()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int>() { { primaryBronze.id, Vector2Int.one } });
        Robot.Orientation expected = Robot.Orientation.SOUTH;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.LEFT, primaryBronze.id),
            RotateCommand(Command.Direction.DOWN, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[0]);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[1]);
        Assert.AreEqual(expected, primaryBronze.orientation);
    }

    [Test]
    public void TestRotateFail()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int>() { { primaryBronze.id, Vector2Int.one } });
        Robot.Orientation expected = Robot.Orientation.WEST;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.DOWN, primaryBronze.id),
            RotateCommand(Command.Direction.LEFT, primaryBronze.id),
            RotateCommand(Command.Direction.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[0]);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[1]);
        Assert.IsInstanceOf<GameEvent.Fail>(events[2]);
        Assert.AreEqual(expected, primaryBronze.orientation);
    }

    [Test]
	public void TestMoveSimpleMove() {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest();
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id),

            MoveCommand(Command.Direction.DOWN, secondaryBronze.id),
            MoveCommand(Command.Direction.DOWN, secondaryBronze.id)
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
    public void TestMoveSimpleMoveFail()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest();
        Vector2Int primaryExpected = primaryBronze.position + Vector2Int.up * 2;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Move>(events[1]);
        Assert.IsInstanceOf<GameEvent.Fail>(events[2]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
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
            MoveCommand(Command.Direction.RIGHT, primaryBronze.id)
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
        Vector2Int primaryExpected = primaryPlatinum.position;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.RIGHT, primaryPlatinum.id),
            RotateCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id)
        );
        Assert.AreEqual(5, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[2]);
        Assert.IsInstanceOf<GameEvent.Push>(events[3]);
        Assert.IsInstanceOf<GameEvent.Move>(events[4]);
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
    public void TestMoveRobotBlockSoft()
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
    public void TestMoveRobotBlockEachOther()
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
            MoveCommand(Command.Direction.RIGHT, primaryBronze.id),
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
            MoveCommand(Command.Direction.RIGHT, primaryBronze.id),
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
            RotateCommand(Command.Direction.RIGHT, primaryBronze.id),
            MoveCommand(Command.Direction.RIGHT, primaryBronze.id),
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
            RotateCommand(Command.Direction.DOWN, secondaryBronze.id),
            RotateCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.DOWN, secondaryBronze.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[2]);
        Assert.IsInstanceOf<GameEvent.Move>(events[3]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveAgainstPreviouslyFacing()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) },
            { secondaryBronze.id, new Vector2Int(1,2) }
        });
        Vector2Int primaryExpected = primaryBronze.position + Vector2Int.up;
        Vector2Int secondaryExpected = secondaryBronze.position + Vector2Int.right;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.DOWN, secondaryBronze.id),
            RotateCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.RIGHT, secondaryBronze.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[2]);
        Assert.IsInstanceOf<GameEvent.Move>(events[3]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveAlongSameDirection()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) },
            { secondaryBronze.id, new Vector2Int(1,2) }
        });
        Vector2Int primaryExpected = primaryBronze.position + Vector2Int.up;
        Vector2Int secondaryExpected = secondaryBronze.position + Vector2Int.up;
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

    [Test]
    public void TestMoveBlockedRobotGetsPushed()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot primaryPlatinum = testgame.primary.team[3];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(2,2) },
            { primaryPlatinum.id, new Vector2Int(1,3) },
            { secondaryBronze.id, new Vector2Int(1,2) }
        });
        Vector2Int primaryExpected = secondaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position + Vector2Int.left;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.DOWN, primaryPlatinum.id),
            RotateCommand(Command.Direction.UP, secondaryBronze.id),
            RotateCommand(Command.Direction.LEFT, primaryBronze.id),
            MoveCommand(Command.Direction.UP, secondaryBronze.id),
            MoveCommand(Command.Direction.LEFT, primaryBronze.id)
        );
        Assert.AreEqual(7, events.Count);
        Assert.That(events[3], Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[4], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Push>());
        Assert.That(events[5], Is.TypeOf<GameEvent.Push>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[6], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Block>());
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveBothWantPushGetBlocked()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot primaryPlatinum = testgame.primary.team[3];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(2,2) },
            { primaryPlatinum.id, new Vector2Int(1,2) },
            { secondaryBronze.id, new Vector2Int(1,3) }
        });
        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.LEFT, primaryPlatinum.id),
            RotateCommand(Command.Direction.DOWN, secondaryBronze.id),
            RotateCommand(Command.Direction.LEFT, primaryBronze.id),
            MoveCommand(Command.Direction.DOWN, secondaryBronze.id),
            MoveCommand(Command.Direction.LEFT, primaryBronze.id)
        );
        Assert.AreEqual(5, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[3]);
        Assert.IsInstanceOf<GameEvent.Block>(events[4]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveInfinite()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot primarySilver = testgame.primary.team[1];
        Robot secondaryBronze = testgame.secondary.team[0];
        Robot secondarySilver = testgame.secondary.team[1];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,2) },
            { primarySilver.id, new Vector2Int(1,3) },
            { secondaryBronze.id, new Vector2Int(2,3) },
            { secondarySilver.id, new Vector2Int(2,2) }
        });
        Vector2Int primaryBronzeExpected = primarySilver.position;
        Vector2Int secondaryBronzeExpected = secondarySilver.position;
        Vector2Int primarySilverExpected = secondaryBronze.position;
        Vector2Int secondarySilverExpected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.UP, primaryBronze.id),
            RotateCommand(Command.Direction.RIGHT, primarySilver.id),
            RotateCommand(Command.Direction.RIGHT, primarySilver.id),
            RotateCommand(Command.Direction.DOWN, secondaryBronze.id),
            RotateCommand(Command.Direction.LEFT, secondarySilver.id),
            RotateCommand(Command.Direction.LEFT, secondarySilver.id),
            MoveCommand(Command.Direction.UP, primaryBronze.id),
            MoveCommand(Command.Direction.RIGHT, primarySilver.id),
            MoveCommand(Command.Direction.DOWN, secondaryBronze.id),
            MoveCommand(Command.Direction.LEFT, secondarySilver.id)
        );
        Assert.AreEqual(10, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[6]);
        Assert.IsInstanceOf<GameEvent.Move>(events[7]);
        Assert.IsInstanceOf<GameEvent.Move>(events[8]);
        Assert.IsInstanceOf<GameEvent.Move>(events[9]);
        Assert.AreEqual(primaryBronzeExpected, primaryBronze.position);
        Assert.AreEqual(secondaryBronzeExpected, secondaryBronze.position);
        Assert.AreEqual(primarySilverExpected, primarySilver.position);
        Assert.AreEqual(secondarySilverExpected, secondarySilver.position);
    }

    [Test]
    public void TestAttackSimple()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        float expected = secondaryBronze.startingHealth - primaryBronze.attack;
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) },
            { secondaryBronze.id, new Vector2Int(1,2) }
        });
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.UP, primaryBronze.id),
            AttackCommand(primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[1]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[2]);
        Assert.AreEqual(expected, secondaryBronze.health);
    }

    [Test]
    public void TestAttackMiss()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        float expected = secondaryBronze.startingHealth;
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) },
            { secondaryBronze.id, new Vector2Int(1,2) }
        });
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.LEFT, primaryBronze.id),
            AttackCommand(primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[1]);
        Assert.IsInstanceOf<GameEvent.Miss>(events[2]);
        Assert.AreEqual(expected, secondaryBronze.health);
    }

    [Test]
    public void TestAttackBattery()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,6) }
        });
        float expected = testgame.secondary.battery - GameConstants.DEFAULT_BATTERY_MULTIPLIER * primaryBronze.attack;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.RIGHT, primaryBronze.id),
            AttackCommand(primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[1]);
        Assert.IsInstanceOf<GameEvent.Battery>(events[2]);
        Assert.AreEqual(expected, testgame.secondary.battery);
    }

    [Test]
    public void TestAttackDeath()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondarySilver = testgame.secondary.team[1];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) },
            { secondarySilver.id, new Vector2Int(1,2) }
        });
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.RIGHT, primaryBronze.id),
            RotateCommand(Command.Direction.DOWN, secondarySilver.id),
            AttackCommand(secondarySilver.id)
        );
        Assert.AreEqual(5, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[2]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[3]);
        Assert.IsInstanceOf<GameEvent.Death>(events[4]);
        Assert.AreEqual(primaryBronze.startingHealth, primaryBronze.health);
        Assert.AreEqual(Vector2Int.zero, primaryBronze.position);
        Assert.AreEqual(Robot.Orientation.NORTH, primaryBronze.orientation);
    }

    [Test]
    public void TestAttackFail()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) }
        });
        List<GameEvent> events = SimulateCommands(
            AttackCommand(primaryBronze.id),
            AttackCommand(primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Miss>(events[1]);
        Assert.IsInstanceOf<GameEvent.Fail>(events[2]);
    }

    [Test]
    public void TestAttackFromQueueFail()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        float expected = secondaryBronze.startingHealth;
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(0,0) },
            { secondaryBronze.id, new Vector2Int(0,1) }
        });
        List<GameEvent> events = SimulateCommands(
            AttackCommand(primaryBronze.id)
        );
        Assert.AreEqual(1, events.Count);
        Assert.IsInstanceOf<GameEvent.Fail>(events[0]);
        Assert.AreEqual(expected, secondaryBronze.health);
    }

    [Test]
    public void TestAttackToQueueMiss()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        float expected = primaryBronze.startingHealth;
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(0,0) },
            { secondaryBronze.id, new Vector2Int(0,1) }
        });
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Direction.DOWN, secondaryBronze.id),
            AttackCommand(secondaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[1]);
        Assert.IsInstanceOf<GameEvent.Miss>(events[2]);
        Assert.AreEqual(expected, primaryBronze.health);
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

    private static Command.Attack AttackCommand(short r)
    {
        Command.Attack a = new Command.Attack();
        a.robotId = r;
        return a;
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
