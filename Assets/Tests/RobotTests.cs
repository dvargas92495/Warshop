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
    public void TestSpawnSimple()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest();
        Vector2Int primaryExpected = Vector2Int.zero;
        List<GameEvent> events = SimulateCommands(
            SpawnCommand(0, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOf<GameEvent.Spawn>(events[0]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
    }

    [Test]
    public void TestSpawnSimpleBlock()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { secondaryBronze.id, new Vector2Int(0,0) }
        });
        Vector2Int primaryExpected = Map.NULL_VEC;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            SpawnCommand(0, primaryBronze.id)
        );
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOf<GameEvent.Block>(events[0]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
	public void TestMoveSimpleMove() {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(0,0) },
            { secondaryBronze.id, new Vector2Int(4,7) }
        });
        Vector2Int primaryExpected = primaryBronze.position + Vector2Int.up * 2;
        Vector2Int secondaryExpected = secondaryBronze.position + Vector2Int.down * 2;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.UP, primaryBronze.id),
            MoveCommand(Command.UP, primaryBronze.id),

            MoveCommand(Command.DOWN, secondaryBronze.id),
            MoveCommand(Command.DOWN, secondaryBronze.id)
        );
        Assert.AreEqual(6, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Move>(events[1]);
        Assert.IsInstanceOf<GameEvent.Resolve>(events[2]);
        Assert.IsInstanceOf<GameEvent.Move>(events[3]);
        Assert.IsInstanceOf<GameEvent.Move>(events[4]);
        Assert.IsInstanceOf<GameEvent.Resolve>(events[5]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveWallBlock()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(0,0) }
        });
        Vector2Int expected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.LEFT, primaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Block>(events[1]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[2]);
        Assert.IsInstanceOf<GameEvent.Resolve>(events[3]);
        Assert.AreEqual(expected, primaryBronze.position);
    }

    [Test]
    public void TestMoveBatteryBlock()
    {
        Robot primaryBronze = testgame.primary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int>() { { primaryBronze.id, Vector2Int.one } });
        Vector2Int expected = primaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Block>(events[1]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[2]);
        Assert.IsInstanceOf<GameEvent.Resolve>(events[3]);
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
            MoveCommand(Command.UP, primaryBronze.id)
        );
        Assert.AreEqual(6, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Push>(events[1]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[2]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[3]);
        Assert.IsInstanceOf<GameEvent.Move>(events[4]);
        Assert.IsInstanceOf<GameEvent.Resolve>(events[5]);
        Assert.AreNotEqual(primaryBronze.position, primaryPlatinum.position);
        Assert.AreEqual(Vector2Int.up, primaryPlatinum.position - primaryBronze.position);
    }

    [Test]
    public void TestMoveRobotBlockByThird()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot primaryPlatinum = testgame.primary.team[3];
        Robot secondaryBronze = testgame.secondary.team[0];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(0,2) },
            { primaryPlatinum.id, new Vector2Int(1,2) },
            { secondaryBronze.id, new Vector2Int(2,2) }
        });
        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(10, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Push>(events[1]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[2]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[3]);
        Assert.IsInstanceOf<GameEvent.Move>(events[4]);
        Assert.IsInstanceOf<GameEvent.Block>(events[5]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[6]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[7]);
        Assert.IsInstanceOf<GameEvent.Block>(events[8]);
        Assert.IsInstanceOf<GameEvent.Resolve>(events[9]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
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
            MoveCommand(Command.RIGHT, primaryBronze.id),
            MoveCommand(Command.LEFT, secondaryBronze.id)
        );
        Assert.AreEqual(7, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Block>(events[1]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[2]);
        Assert.IsInstanceOf<GameEvent.Move>(events[3]);
        Assert.IsInstanceOf<GameEvent.Block>(events[4]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[5]);
        Assert.IsInstanceOf<GameEvent.Resolve>(events[6]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveRobotBlockEachOtherWithPush()
    {
        Robot primaryGold = testgame.primary.team[2];
        Robot primaryPlatinum = testgame.primary.team[3];
        Robot secondaryGold = testgame.secondary.team[2];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryGold.id, new Vector2Int(0,2) },
            { primaryPlatinum.id, new Vector2Int(1,2) },
            { secondaryGold.id, new Vector2Int(3,2) }
        });
        Vector2Int primaryExpected = primaryGold.position;
        Vector2Int secondaryExpected = secondaryGold.position;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryGold.id),
            MoveCommand(Command.LEFT, secondaryGold.id)
        );
        Assert.AreEqual(12, events.Count);
        Assert.That(events[0], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[1], Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Push>());
        Assert.That(events[2], Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events[3], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events[4], Is.TypeOf<GameEvent.Push>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[5], Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Block>());
        Assert.That(events[6], Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events[7], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Block>());
        Assert.That(events[8], Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[9], Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Block>());
        Assert.That(events[10], Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Damage>());
        Assert.IsInstanceOf<GameEvent.Resolve>(events[11]);
        Assert.AreEqual(primaryExpected, primaryGold.position);
        Assert.AreEqual(secondaryExpected, secondaryGold.position);
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
            MoveCommand(Command.LEFT, secondaryBronze.id),
            MoveCommand(Command.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(7, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Block>(events[1]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[2]);
        Assert.IsInstanceOf<GameEvent.Move>(events[3]);
        Assert.IsInstanceOf<GameEvent.Block>(events[4]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[5]);
        Assert.IsInstanceOf<GameEvent.Resolve>(events[6]);
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
            MoveCommand(Command.UP, secondaryBronze.id),
            MoveCommand(Command.UP, primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Move>(events[1]);
        Assert.IsInstanceOf<GameEvent.Resolve>(events[2]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
    }

    [Test]
    public void TestMoveBlockedRobotGetsPushed()
    {
        Robot primaryGold = testgame.primary.team[2];
        Robot primaryPlatinum = testgame.primary.team[3];
        Robot secondaryGold = testgame.secondary.team[2];
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryGold.id, new Vector2Int(3,3) },
            { primaryPlatinum.id, new Vector2Int(2,2) },
            { secondaryGold.id, new Vector2Int(2,3) }
        });
        Vector2Int primaryExpected = secondaryGold.position;
        Vector2Int secondaryExpected = secondaryGold.position + Vector2Int.left;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.DOWN, secondaryGold.id),
            MoveCommand(Command.LEFT, primaryGold.id)
        );
        Assert.AreEqual(14, events.Count);
        Assert.That(events[0], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[1], Is.TypeOf<GameEvent.Push>().Or.TypeOf<GameEvent.Push>());
        Assert.That(events[2], Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events[3], Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events[4], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[5], Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[6], Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Push>());
        Assert.That(events[7], Is.TypeOf<GameEvent.Block>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events[8], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events[9], Is.TypeOf<GameEvent.Push>().Or.TypeOf<GameEvent.Move>());
        Assert.That(events[10], Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Block>());
        Assert.That(events[11], Is.TypeOf<GameEvent.Damage>().Or.TypeOf<GameEvent.Damage>());
        Assert.That(events[12], Is.TypeOf<GameEvent.Move>().Or.TypeOf<GameEvent.Block>());
        Assert.IsInstanceOf<GameEvent.Resolve>(events[13]);
        Assert.AreEqual(primaryExpected, primaryGold.position);
        Assert.AreEqual(secondaryExpected, secondaryGold.position);
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
            MoveCommand(Command.DOWN, secondaryBronze.id),
            MoveCommand(Command.LEFT, primaryBronze.id)
        );
        Assert.AreEqual(7, events.Count);
        Assert.IsInstanceOf<GameEvent.Move>(events[0]);
        Assert.IsInstanceOf<GameEvent.Block>(events[1]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[2]);
        Assert.IsInstanceOf<GameEvent.Move>(events[3]);
        Assert.IsInstanceOf<GameEvent.Block>(events[4]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[5]);
        Assert.IsInstanceOf<GameEvent.Resolve>(events[6]);
        Assert.AreEqual(primaryExpected, primaryBronze.position);
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
            AttackCommand(Command.UP, primaryBronze.id)
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
            AttackCommand(Command.LEFT, primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Miss>(events[1]);
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
            AttackCommand(Command.RIGHT, primaryBronze.id)
        );
        Assert.AreEqual(3, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Battery>(events[1]);
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
        primaryBronze.health = secondarySilver.attack;
        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.DOWN, secondarySilver.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[1]);
        Assert.IsInstanceOf<GameEvent.Death>(events[2]);
        Assert.AreEqual(primaryBronze.startingHealth, primaryBronze.health);
        Assert.AreEqual(Map.NULL_VEC, primaryBronze.position);
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
        primaryBronze.health = secondarySilver.attack;
        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.DOWN, secondarySilver.id),
            MoveCommand(Command.UP, primaryBronze.id)
        );
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[1]);
        Assert.IsInstanceOf<GameEvent.Death>(events[2]);
        Assert.AreEqual(primaryBronze.startingHealth, primaryBronze.health);
        Assert.AreEqual(Map.NULL_VEC, primaryBronze.position);
    }

    [Test]
    public void TestAttackMultipleSamePriority()
    {
        Robot primaryBronze = testgame.primary.team[0];
        Robot secondaryBronze = testgame.secondary.team[0];
        Robot secondarySilver = testgame.secondary.team[1];
        short expected = (short)(secondarySilver.startingHealth - primaryBronze.attack - secondaryBronze.attack);
        BeforeEachTest(new Dictionary<short, Vector2Int> {
            { primaryBronze.id, new Vector2Int(1,2) },
            { secondarySilver.id, new Vector2Int(2,2) },
            { secondaryBronze.id, new Vector2Int(3,2) }
        });
        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.RIGHT, primaryBronze.id),
            AttackCommand(Command.LEFT, secondaryBronze.id)
        );
        Assert.AreEqual(5, events.Count);
        Assert.IsInstanceOf<GameEvent.Attack>(events[0]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[1]);
        Assert.IsInstanceOf<GameEvent.Attack>(events[2]);
        Assert.IsInstanceOf<GameEvent.Damage>(events[3]);
        Assert.AreEqual(expected, secondarySilver.health);
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
            AttackCommand(Command.UP, primaryPithon.id)
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
        SimulateCommands(AttackCommand(Command.UP, primaryPithon.id));
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
