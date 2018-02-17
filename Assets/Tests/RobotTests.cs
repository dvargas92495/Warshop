using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;

public class GruntTest : TestBase
{

    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        BeforeAllTests(new string[]
        {
            "Bronze Grunt",
            "Silver Grunt",
            "Golden Grunt",
            "Platinum Grunt"
        }, new string[]
        {
            "Bronze Grunt",
            "Silver Grunt",
            "Golden Grunt",
            "Platinum Grunt"
        });
    }

    [Test]
    public void TestRotateSimple()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int>() { { primaryBronze.id, Vector2Int.one } });
        Robot.Orientation expected = Robot.Orientation.EAST;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Rotate.CLOCKWISE, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[0]);
        Assert.AreEqual(expected, primaryBronze.orientation);
    }

    [Test]
    public void TestRotateFlip()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int>() { { primaryBronze.id, Vector2Int.one } });
        Robot.Orientation expected = Robot.Orientation.SOUTH;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Rotate.FLIP, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
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
            RotateCommand(Command.Rotate.COUNTERCLOCKWISE, primaryBronze.id),
            RotateCommand(Command.Rotate.COUNTERCLOCKWISE, primaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[0]);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[2]);
        Assert.AreEqual(expected, primaryBronze.orientation);
    }

    [Test]
    public void TestRotateFail()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int>() { { primaryBronze.id, Vector2Int.one } });
        Robot.Orientation expected = Robot.Orientation.WEST;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Rotate.FLIP, primaryBronze.id),
            RotateCommand(Command.Rotate.CLOCKWISE, primaryBronze.id),
            RotateCommand(Command.Rotate.CLOCKWISE, primaryBronze.id)
        );
        Assert.AreEqual(6, events.Count);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[0]);
        Assert.IsInstanceOf<GameEvent.Rotate>(events[2]);
        Assert.IsInstanceOf<GameEvent.Fail>(events[4]);
        Assert.AreEqual(expected, primaryBronze.orientation);
    }

    [Test]
	public void TestMoveSimpleMove() {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest();
        Vector2Int primaryExpected = primaryBronze.position + Vector2Int.up * 2;
        Vector2Int secondaryExpected = secondaryBronze.position + Vector2Int.down * 2;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.UP, primaryBronze.id),
            MoveCommand(Command.Move.UP, primaryBronze.id),

            MoveCommand(Command.Move.DOWN, secondaryBronze.id),
            MoveCommand(Command.Move.DOWN, secondaryBronze.id)
        );
        Assert.AreEqual(6, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Move>(events[1]);
        Assert.IsInstanceOf<GameEvent.Move>(events[3]);
        Assert.IsInstanceOf<GameEvent.Move>(events[4]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveSimpleMoveFail()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest();
        Vector2Int primaryExpected = primaryBronze.position + Vector2Int.up * 2;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.UP, primaryBronze.id),
            MoveCommand(Command.Move.UP, primaryBronze.id),
            MoveCommand(Command.Move.UP, primaryBronze.id)
        );
        Assert.AreEqual(6, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Move>(events[2]);
        Assert.IsInstanceOf<GameEvent.Fail>(events[4]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
    }

    [Test]
    public void TestMoveWallBlock()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest();
        Vector2Int expected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
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
            MoveCommand(Command.Move.UP, primaryBronze.id),
            MoveCommand(Command.Move.DOWN, primaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[2]);
        Assert.AreEqual(expected, primaryBronze.position);
    }

    [Test]
    public void TestMoveBatteryBlock()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int>() { { primaryBronze.id, Vector2Int.one } });
        Vector2Int expected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
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
            RotateCommand(Command.Rotate.CLOCKWISE, primaryPlatinum.id),
            MoveCommand(Command.Move.UP, primaryBronze.id)
        );
        Assert.AreEqual(6, events.Count);
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
            RotateCommand(Command.Rotate.FLIP, primaryPlatinum.id),
            MoveCommand(Command.Move.UP, primaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[2]);
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
            MoveCommand(Command.Move.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[0]);
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
            RotateCommand(Command.Rotate.FLIP, secondaryBronze.id),
            RotateCommand(Command.Rotate.FLIP, primaryBronze.id),
            MoveCommand(Command.Move.RIGHT, primaryBronze.id),
            MoveCommand(Command.Move.LEFT, secondaryBronze.id)
        );
        Assert.AreEqual(6, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[3]);
        Assert.IsInstanceOf<GameEvent.Block>(events[4]);
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
            RotateCommand(Command.Rotate.CLOCKWISE, secondaryBronze.id),
            RotateCommand(Command.Rotate.FLIP, primaryBronze.id),
            MoveCommand(Command.Move.RIGHT, primaryBronze.id),
            MoveCommand(Command.Move.LEFT, secondaryBronze.id)
        );
        Assert.AreEqual(6, events.Count);
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
            RotateCommand(Command.Rotate.FLIP, secondaryBronze.id),
            RotateCommand(Command.Rotate.CLOCKWISE, primaryBronze.id),
            MoveCommand(Command.Move.RIGHT, primaryBronze.id),
            MoveCommand(Command.Move.LEFT, secondaryBronze.id)
        );
        Assert.AreEqual(6, events.Count);
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
            { primaryBronze.id, new Vector2Int(1,2) },
            { secondaryBronze.id, new Vector2Int(2,2) }
        });
        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.LEFT, secondaryBronze.id),
            MoveCommand(Command.Move.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[0]);
        Assert.IsInstanceOf<GameEvent.Block>(events[1]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveSwapPositionsFacing()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) },
            { secondaryBronze.id, new Vector2Int(1,2) }
        });
        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.DOWN, secondaryBronze.id),
            MoveCommand(Command.Move.UP, primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[0]);
        Assert.IsInstanceOf<GameEvent.Block>(events[1]);
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
            MoveCommand(Command.Move.RIGHT, secondaryBronze.id),
            MoveCommand(Command.Move.UP, primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Move>(events[1]);
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
        SimulateCommands(RotateCommand(Command.Rotate.FLIP, secondaryBronze.id));
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.UP, secondaryBronze.id),
            MoveCommand(Command.Move.UP, primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Move>(events[1]);
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
            RotateCommand(Command.Rotate.FLIP, primaryPlatinum.id),
            RotateCommand(Command.Rotate.FLIP, secondaryBronze.id),
            RotateCommand(Command.Rotate.COUNTERCLOCKWISE, primaryBronze.id),
            MoveCommand(Command.Move.UP, secondaryBronze.id),
            MoveCommand(Command.Move.LEFT, primaryBronze.id)
        );
        Assert.AreEqual(10, events.Count);
        Assert.That(events[5], Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[6], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Push>());
        Assert.That(events[7], Is.TypeOf<GameEvent.Push>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[8], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Block>());
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveBothWantPushGetBlocked()
    {
        Robot primarySilver = testgame.primary.team[1];
        Robot primaryPlatinum = testgame.primary.team[3];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primarySilver.id, new Vector2Int(2,2) },
            { primaryPlatinum.id, new Vector2Int(1,2) },
            { secondaryBronze.id, new Vector2Int(1,3) }
        });
        Vector2Int primaryExpected = primarySilver.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            RotateCommand(Command.Rotate.COUNTERCLOCKWISE, primaryPlatinum.id),
            RotateCommand(Command.Rotate.COUNTERCLOCKWISE, primarySilver.id),
            MoveCommand(Command.Move.DOWN, secondaryBronze.id),
            MoveCommand(Command.Move.LEFT, primarySilver.id)
        );
        Assert.AreEqual(7, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[4]);
        Assert.IsInstanceOf<GameEvent.Block>(events[5]);
        Assert.AreEqual(primaryExpected, primarySilver.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
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
            AttackCommand(primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[1]);
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
            RotateCommand(Command.Rotate.COUNTERCLOCKWISE, primaryBronze.id),
            AttackCommand(primaryBronze.id)
        );
        Assert.AreEqual(5, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[2]);
        Assert.IsInstanceOf<GameEvent.Miss>(events[3]);
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
            RotateCommand(Command.Rotate.CLOCKWISE, primaryBronze.id),
            AttackCommand(primaryBronze.id)
        );
        Assert.AreEqual(5, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[2]);
        Assert.IsInstanceOf<GameEvent.Battery>(events[3]);
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
            AttackCommand(secondarySilver.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[1]);
        Assert.IsInstanceOf<GameEvent.Death>(events[2]);
        Assert.AreEqual(primaryBronze.startingHealth, primaryBronze.health);
        Assert.AreEqual(Vector2Int.zero, primaryBronze.position);
        Assert.AreEqual(Robot.Orientation.NORTH, primaryBronze.orientation);
    }

    [Test]
    public void TestAttackDeathFailedMove()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondarySilver = testgame.secondary.team[1];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,1) },
            { secondarySilver.id, new Vector2Int(1,2) }
        });
        List<GameEvent> events = SimulateCommands(
            AttackCommand(secondarySilver.id),
            MoveCommand(Command.Move.UP, primaryBronze.id)
        );
        Assert.AreEqual(6, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[1]);
        Assert.IsInstanceOf<GameEvent.Death>(events[2]);
        Assert.IsInstanceOf<GameEvent.Fail>(events[4]);
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
        Assert.AreEqual(5, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Miss>(events[1]);
        Assert.IsInstanceOf<GameEvent.Fail>(events[3]);
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
        Assert.AreEqual(2, events.Count);
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
            AttackCommand(secondaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Miss>(events[1]);
        Assert.AreEqual(expected, primaryBronze.health);
    }
}

public class PithonTest : TestBase
{

    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        BeforeAllTests(new string[]
        {
            "Pithon"
        }, new string[]
        {
            "Pithon",
            "Silver Grunt"
        });
    }

    [Test]
    public void TestAttackPoisons()
    {
        Robot primaryPithon = testgame.primary.team[0];
        Robot secondarySilver = testgame.secondary.team[1];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryPithon.id, new Vector2Int(1,1) },
            { secondarySilver.id, new Vector2Int(1,2) }
        });
        short expected = (short)(secondarySilver.health - primaryPithon.attack - 1);
        List<GameEvent> events = SimulateCommands(
            AttackCommand(primaryPithon.id)
        );
        Assert.AreEqual(6, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[1]);
        Assert.IsInstanceOf<GameEvent.Poison>(events[2]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[4]);
        Assert.AreEqual(expected, secondarySilver.health);
    }

    [Test]
    public void TestPoisonKills()
    {
        Robot primaryPithon = testgame.primary.team[0];
        Robot secondarySilver = testgame.secondary.team[1];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryPithon.id, new Vector2Int(1,1) },
            { secondarySilver.id, new Vector2Int(1,2) }
        });
        SimulateCommands(
            MoveCommand(Command.Move.UP, primaryPithon.id),
            AttackCommand(primaryPithon.id)
        );
        for (int i = 0; i < 5; i++) SimulateCommands();
        Assert.AreEqual(secondarySilver.startingHealth, secondarySilver.health);
        SimulateCommands();
        Assert.AreEqual(secondarySilver.startingHealth, secondarySilver.health);
    }
}

public class JaguarTest : TestBase
{

    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        BeforeAllTests(new string[]
        {
            "Jaguar"
        }, new string[]
        {
            "Jaguar"
        });
    }

    [Test]
    public void TestThreeMoves()
    {
        Robot primaryJaguar = testgame.primary.team[0];
        BeforeEachTest();
        Vector2Int expected = primaryJaguar.position + Vector2Int.up * 3;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.UP, primaryJaguar.id),
            MoveCommand(Command.Move.UP, primaryJaguar.id),
            MoveCommand(Command.Move.UP, primaryJaguar.id)
        );
        Assert.AreEqual(6, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Move>(events[2]);
        Assert.IsInstanceOf<GameEvent.Move>(events[4]);
        Assert.AreEqual(expected, primaryJaguar.position);
    }

    [Test]
    public void TestAttackThirdMoveFails()
    {
        Robot primaryJaguar = testgame.primary.team[0];
        BeforeEachTest();
        Vector2Int expected = primaryJaguar.position + Vector2Int.up * 2;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.UP, primaryJaguar.id),
            AttackCommand(primaryJaguar.id),
            MoveCommand(Command.Move.UP, primaryJaguar.id),
            MoveCommand(Command.Move.UP, primaryJaguar.id)
        );
        Assert.AreEqual(9, events.Count);
        Assert.IsInstanceOf<GameEvent.Fail>(events[7]);
        Assert.AreEqual(expected, primaryJaguar.position);
    }

    [Test]
    public void TestAttackFailsAfterThirdMove()
    {
        Robot primaryJaguar = testgame.primary.team[0];
        BeforeEachTest();
        Vector2Int expected = primaryJaguar.position + Vector2Int.up * 3;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.UP, primaryJaguar.id),
            MoveCommand(Command.Move.UP, primaryJaguar.id),
            MoveCommand(Command.Move.UP, primaryJaguar.id),
            AttackCommand(primaryJaguar.id)
        );
        Assert.AreEqual(8, events.Count);
        Assert.IsInstanceOf<GameEvent.Fail>(events[6]);
        Assert.AreEqual(expected, primaryJaguar.position);
    }
}

public class SlinkbotTest : TestBase
{

    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        BeforeAllTests(new string[]
        {
            "Slinkbot",
            "Silver Grunt"
        }, new string[]
        {
            "Silver Grunt"
        });
    }

    [Test]
    public void TestMoveSimpleMove()
    {
        Robot primarySlink = testgame.primary.team[0];
        BeforeEachTest();
        Vector2Int expected = primarySlink.position + Vector2Int.up * 2;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.Move.UP, primarySlink.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Move>(events[1]);
        Assert.AreEqual(expected, primarySlink.position);
    }

}
