using UnityEngine;
using NUnit.Framework;

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
    public void TestSpawnSimple()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        BeforeEachTest();
        Vector2Int primaryExpected = Vector2Int.zero;
        List<GameEvent> events = SimulateCommands(
            SpawnCommand(0, primaryBronze.id)
        );
        Assert.AreEqual(2, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Spawn>(events.Get(0));
        Assert.AreEqual(primaryExpected, primaryBronze.position);
    }

    [Test]
    public void TestSpawnSimpleBlock()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot secondaryBronze = testgame.secondary.team.Get(0);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(1);
        setup.Add(secondaryBronze.id, new Vector2Int(0, 0));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = Map.NULL_VEC;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            SpawnCommand(0, primaryBronze.id)
        );
        Assert.AreEqual(2, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(0));
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
	public void TestMoveSimpleMove()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot secondaryBronze = testgame.secondary.team.Get(0);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(0, 0));
        setup.Add(secondaryBronze.id, new Vector2Int(4, 7));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryBronze.position + Vector2Int.up * 2;
        Vector2Int secondaryExpected = secondaryBronze.position + Vector2Int.down * 2;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.UP, primaryBronze.id),
            MoveCommand(Command.UP, primaryBronze.id),

            MoveCommand(Command.DOWN, secondaryBronze.id),
            MoveCommand(Command.DOWN, secondaryBronze.id)
        );
        Assert.AreEqual(6, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(2));
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(3));
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(4));
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(5));
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveWallBlock()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(1);
        setup.Add(primaryBronze.id, new Vector2Int(0, 0));
        BeforeEachTest(setup);

        Vector2Int expected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.LEFT, primaryBronze.id)
        );
        Assert.AreEqual(4, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(2));
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(3));
        Assert.AreEqual(expected, primaryBronze.position);
    }

    [Test]
    public void TestMoveBatteryBlock()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(1);
        setup.Add(primaryBronze.id, Vector2Int.one);
        BeforeEachTest(setup);

        Vector2Int expected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(4, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(2));
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(3));
        Assert.AreEqual(expected, primaryBronze.position);
    }

    [Test]
    public void TestMovePush()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot primaryPlatinum = testgame.primary.team.Get(3);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 1));
        setup.Add(primaryPlatinum.id, new Vector2Int(1, 2));
        BeforeEachTest(setup);

        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.UP, primaryBronze.id)
        );
        Assert.AreEqual(6, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Push>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(2));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(3));
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(4));
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(5));
        Assert.AreNotEqual(primaryBronze.position, primaryPlatinum.position);
        Assert.AreEqual(Vector2Int.up, primaryPlatinum.position - primaryBronze.position);
    }

    [Test]
    public void TestMoveRobotBlockByThird()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot primaryPlatinum = testgame.primary.team.Get(3);
        Robot secondaryBronze = testgame.secondary.team.Get(0);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(3);
        setup.Add(primaryBronze.id, new Vector2Int(0, 2));
        setup.Add(primaryPlatinum.id, new Vector2Int(1, 2));
        setup.Add(secondaryBronze.id, new Vector2Int(2, 2));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(10, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Push>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(2));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(3));
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(4));
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(5));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(6));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(7));
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(8));
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(9));
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveRobotBlockEachOther()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot secondaryBronze = testgame.secondary.team.Get(0);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(0, 2));
        setup.Add(secondaryBronze.id, new Vector2Int(2, 2));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryBronze.id),
            MoveCommand(Command.LEFT, secondaryBronze.id)
        );
        Assert.AreEqual(7, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(2));
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(3));
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(4));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(5));
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(6));
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveRobotBlockEachOtherWithPush()
    {
        Robot primaryGold = testgame.primary.team.Get(2);
        Robot primaryPlatinum = testgame.primary.team.Get(3);
        Robot secondaryGold = testgame.secondary.team.Get(2);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(3);
        setup.Add(primaryGold.id, new Vector2Int(0, 2));
        setup.Add(primaryPlatinum.id, new Vector2Int(1, 2));
        setup.Add(secondaryGold.id, new Vector2Int(3, 2));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryGold.position;
        Vector2Int secondaryExpected = secondaryGold.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryGold.id),
            MoveCommand(Command.LEFT, secondaryGold.id)
        );
        Assert.AreEqual(12, events.GetLength());
        Assert.That(events.Get(0), Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events.Get(1), Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Push>());
        Assert.That(events.Get(2), Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events.Get(3), Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events.Get(4), Is.TypeOf<GameEvent.Push>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events.Get(5), Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Block>());
        Assert.That(events.Get(6), Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events.Get(7), Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Block>());
        Assert.That(events.Get(8), Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events.Get(9), Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Block>());
        Assert.That(events.Get(10), Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Damage>());
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(11));
        Assert.AreEqual(primaryExpected, primaryGold.position);
        Assert.AreEqual(secondaryExpected, secondaryGold.position);
    }

    [Test]
    public void TestMoveSwapPositions()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot secondaryBronze = testgame.secondary.team.Get(0);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 2));
        setup.Add(secondaryBronze.id, new Vector2Int(2, 2));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.LEFT, secondaryBronze.id),
            MoveCommand(Command.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(7, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(2));
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(3));
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(4));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(5));
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(6));
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveAlongSameDirection()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot secondaryBronze = testgame.secondary.team.Get(0);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 1));
        setup.Add(secondaryBronze.id, new Vector2Int(1, 2));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryBronze.position + Vector2Int.up;
        Vector2Int secondaryExpected = secondaryBronze.position + Vector2Int.up;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.UP, secondaryBronze.id),
            MoveCommand(Command.UP, primaryBronze.id)
        );
        Assert.AreEqual(3, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(2));
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveBlockedRobotGetsPushed()
    {
        Robot primaryGold = testgame.primary.team.Get(2);
        Robot primaryPlatinum = testgame.primary.team.Get(3);
        Robot secondaryGold = testgame.secondary.team.Get(2);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(3);
        setup.Add(primaryGold.id, new Vector2Int(3, 3));
        setup.Add(primaryPlatinum.id, new Vector2Int(2, 2));
        setup.Add(secondaryGold.id, new Vector2Int(2, 3));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = secondaryGold.position;
        Vector2Int secondaryExpected = secondaryGold.position + Vector2Int.left;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.DOWN, secondaryGold.id),
            MoveCommand(Command.LEFT, primaryGold.id)
        );
        Assert.AreEqual(14, events.GetLength());
        Assert.That(events.Get(0), Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events.Get(1), Is.TypeOf<GameEvent.Push>().Or.TypeOf<GameEvent.Push>());
        Assert.That(events.Get(2), Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events.Get(3), Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events.Get(4), Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events.Get(5), Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events.Get(6), Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Push>());
        Assert.That(events.Get(7), Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events.Get(8), Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events.Get(9), Is.TypeOf<GameEvent.Push>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events.Get(10), Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Block>());
        Assert.That(events.Get(11), Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events.Get(12), Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Block>());
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(13));
        Assert.AreEqual(primaryExpected, primaryGold.position);
        Assert.AreEqual(secondaryExpected, secondaryGold.position);
    }

    [Test]
    public void TestMoveBothWantPushGetBlocked()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot primaryPlatinum = testgame.primary.team.Get(3);
        Robot secondaryBronze = testgame.secondary.team.Get(0);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(3);
        setup.Add(primaryBronze.id, new Vector2Int(2, 2));
        setup.Add(primaryPlatinum.id, new Vector2Int(1, 2));
        setup.Add(secondaryBronze.id, new Vector2Int(1, 3));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.DOWN, secondaryBronze.id),
            MoveCommand(Command.LEFT, primaryBronze.id)
        );
        Assert.AreEqual(7, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(2));
        Assert.IsInstanceOf<GameEvent.Move>(events.Get(3));
        Assert.IsInstanceOf<GameEvent.Block>(events.Get(4));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(5));
        Assert.IsInstanceOf<GameEvent.Resolve>(events.Get(6));
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestAttackSimple()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot secondaryBronze = testgame.secondary.team.Get(0);
        float expected = secondaryBronze.startingHealth - primaryBronze.attack;
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 1));
        setup.Add(secondaryBronze.id, new Vector2Int(1, 2));
        BeforeEachTest(setup);

        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.UP, primaryBronze.id)
        );
        Assert.AreEqual(3, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Attack>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(1));
        Assert.AreEqual(expected, secondaryBronze.health);
    }

    [Test]
    public void TestAttackMiss()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot secondaryBronze = testgame.secondary.team.Get(0);
        float expected = secondaryBronze.startingHealth;
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 1));
        setup.Add(secondaryBronze.id, new Vector2Int(1,2));
        BeforeEachTest(setup);

        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.LEFT, primaryBronze.id)
        );
        Assert.AreEqual(3, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Attack>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Miss>(events.Get(1));
        Assert.AreEqual(expected, secondaryBronze.health);
    }

    [Test]
    public void TestAttackBattery()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(1);
        setup.Add(primaryBronze.id, new Vector2Int(1,6));
        BeforeEachTest(setup);

        float expected = testgame.secondary.battery - GameConstants.DEFAULT_BATTERY_MULTIPLIER * primaryBronze.attack;
        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(3, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Attack>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Battery>(events.Get(1));
        Assert.AreEqual(expected, testgame.secondary.battery);
    }

    [Test]
    public void TestAttackDeath()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot secondarySilver = testgame.secondary.team.Get(1);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 1));
        setup.Add(secondarySilver.id, new Vector2Int(1,2));
        BeforeEachTest(setup);

        primaryBronze.health = secondarySilver.attack;
        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.DOWN, secondarySilver.id)
        );
        Assert.AreEqual(4, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Attack>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Death>(events.Get(2));
        Assert.AreEqual(primaryBronze.startingHealth, primaryBronze.health);
        Assert.AreEqual(Map.NULL_VEC, primaryBronze.position);
    }

    [Test]
    public void TestAttackDeathFailedMove()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot secondarySilver = testgame.secondary.team.Get(1);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 1));
        setup.Add(secondarySilver.id, new Vector2Int(1,2));
        BeforeEachTest(setup);

        primaryBronze.health = secondarySilver.attack;
        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.DOWN, secondarySilver.id),
            MoveCommand(Command.UP, primaryBronze.id)
        );
        Assert.AreEqual(4, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Attack>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Death>(events.Get(2));
        Assert.AreEqual(primaryBronze.startingHealth, primaryBronze.health);
        Assert.AreEqual(Map.NULL_VEC, primaryBronze.position);
    }

    [Test]
    public void TestAttackMultipleSamePriority()
    {
        Robot primaryBronze = testgame.primary.team.Get(0);
        Robot secondaryBronze = testgame.secondary.team.Get(0);
        Robot secondarySilver = testgame.secondary.team.Get(1);
        short expected = (short)(secondarySilver.startingHealth - primaryBronze.attack - secondaryBronze.attack);
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(3);
        setup.Add(primaryBronze.id, new Vector2Int(1, 2));
        setup.Add(secondarySilver.id, new Vector2Int(2, 2));
        setup.Add(secondaryBronze.id, new Vector2Int(3,2));
        BeforeEachTest(setup);

        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.RIGHT, primaryBronze.id),
            AttackCommand(Command.LEFT, secondaryBronze.id)
        );
        Assert.AreEqual(5, events.GetLength());
        Assert.IsInstanceOf<GameEvent.Attack>(events.Get(0));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Attack>(events.Get(2));
        Assert.IsInstanceOf<GameEvent.Damage>(events.Get(3));
        Assert.AreEqual(expected, secondarySilver.health);
    }
}