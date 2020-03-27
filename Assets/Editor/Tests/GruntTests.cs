using UnityEngine;
using NUnit.Framework;

public class GruntTests : RobotTestsBase
{
    Robot primaryBronze;
    Robot primaryGold;
    Robot primaryPlatinum;
    Robot secondaryBronze;
    Robot secondarySilver;
    Robot secondaryGold;

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
        primaryBronze = testgame.primary.team.Get(0);
        primaryGold = testgame.primary.team.Get(2);
        primaryPlatinum = testgame.primary.team.Get(3);
        secondaryBronze = testgame.secondary.team.Get(0);
        secondarySilver = testgame.secondary.team.Get(1);
        secondaryGold = testgame.secondary.team.Get(2);
    }

    [Test]
    public void TestSpawnSimple()
    {
        BeforeEachTest();

        Vector2Int primaryBronzeExpectedPosition = Vector2Int.zero;
        int primaryExpectedScore = testgame.primary.battery - GameConstants.DEFAULT_SPAWN_POWER;
        List<GameEvent> events = SimulateCommands(
            SpawnCommand(Command.UP, primaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            SuccessSpawnEvent(primaryBronzeExpectedPosition, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_SPAWN_POWER, 0),
            SpawnResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(primaryBronzeExpectedPosition, primaryBronze.position);
        Assert.AreEqual(primaryExpectedScore, testgame.primary.battery);
    }

    [Test]
    public void TestSpawnSimpleBlock()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(1);
        setup.Add(secondaryBronze.id, new Vector2Int(0, 0));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = Map.NULL_VEC;
        Vector2Int secondaryExpected = secondaryBronze.position;
        int primaryExpectedScore = testgame.primary.battery - GameConstants.DEFAULT_SPAWN_POWER;
        List<GameEvent> events = SimulateCommands(
            SpawnCommand(0, primaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            FailSpawnEvent(secondaryExpected, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_SPAWN_POWER, 0),
            SuccessBlockEvent(secondaryExpected, primaryBronze.id, secondaryBronze.name, primaryBronze.priority),
            SpawnResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
        Assert.AreEqual(primaryExpectedScore, testgame.primary.battery);
    }

    [Test]
	public void TestMoveSimpleMove()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(0, 0));
        setup.Add(secondaryBronze.id, new Vector2Int(4, 7));
        BeforeEachTest(setup);

        Vector2Int primaryStart = primaryBronze.position;
        Vector2Int secondaryStart = secondaryBronze.position;
        Vector2Int primaryExpected = primaryBronze.position + Vector2Int.up * 2;
        Vector2Int secondaryExpected = secondaryBronze.position + Vector2Int.down * 2;
        int primaryExpectedScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER * 2;
        int secondaryExpectedScore = testgame.secondary.battery - GameConstants.DEFAULT_MOVE_POWER * 2;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.UP, primaryBronze.id),
            MoveCommand(Command.UP, primaryBronze.id),
            MoveCommand(Command.DOWN, secondaryBronze.id),
            MoveCommand(Command.DOWN, secondaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            SuccessMoveEvent(primaryStart, primaryStart + Vector2Int.up, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            SuccessMoveEvent(secondaryStart, secondaryStart + Vector2Int.down, secondaryBronze.id, secondaryBronze.priority, 0, GameConstants.DEFAULT_MOVE_POWER),
            MoveResolveEvent(primaryBronze.priority),
            SuccessMoveEvent(primaryStart + Vector2Int.up, primaryExpected, primaryBronze.id, primaryBronze.priority - 1, GameConstants.DEFAULT_MOVE_POWER, 0),
            SuccessMoveEvent(secondaryStart + Vector2Int.down, secondaryExpected, secondaryBronze.id, secondaryBronze.priority - 1, 0, GameConstants.DEFAULT_MOVE_POWER),
            MoveResolveEvent(primaryBronze.priority - 1)
        );
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
        Assert.AreEqual(primaryExpectedScore, testgame.primary.battery);
        Assert.AreEqual(secondaryExpectedScore, testgame.secondary.battery);
    }

    [Test]
    public void TestMoveWallBlock()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(1);
        setup.Add(primaryBronze.id, new Vector2Int(0, 0));
        BeforeEachTest(setup);

        Vector2Int expectedPosition = primaryBronze.position;
        int expectedHealth = primaryBronze.health - 1;
        int expectedScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.LEFT, primaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            FailMoveEvent(expectedPosition, expectedPosition + Vector2Int.left, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            SuccessBlockEvent(expectedPosition + Vector2Int.left, primaryBronze.id, BlockEvent.WALL, primaryBronze.priority),
            SuccessDamageEvent(primaryBronze.id, 1, expectedHealth, primaryBronze.priority),
            MoveResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(expectedPosition, primaryBronze.position);
        Assert.AreEqual(expectedHealth, primaryBronze.health);
        Assert.AreEqual(expectedScore, testgame.primary.battery);
    }

    [Test]
    public void TestMoveBatteryBlock()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(1);
        setup.Add(primaryBronze.id, Vector2Int.one);
        BeforeEachTest(setup);

        Vector2Int expectedPosition = primaryBronze.position;
        int expectedHealth = primaryBronze.health - 1;
        int expectedScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            FailMoveEvent(expectedPosition, expectedPosition + Vector2Int.left, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            SuccessBlockEvent(expectedPosition + Vector2Int.right, primaryBronze.id, BlockEvent.BATTERY, primaryBronze.priority),
            SuccessDamageEvent(primaryBronze.id, 1, expectedHealth, primaryBronze.priority),
            MoveResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(expectedPosition, primaryBronze.position);
        Assert.AreEqual(expectedHealth, primaryBronze.health);
        Assert.AreEqual(expectedScore, testgame.primary.battery);
    }

    [Test]
    public void TestMovePush()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 1));
        setup.Add(primaryPlatinum.id, new Vector2Int(1, 2));
        BeforeEachTest(setup);

        Vector2Int bronzeStart = primaryBronze.position;
        Vector2Int platinumStart = primaryPlatinum.position;
        Vector2Int expectedPlatinumPosition = primaryPlatinum.position + Vector2Int.up;
        int expectedBronzeHealth = primaryBronze.health - 1;
        int expectedPlatinumHealth = primaryPlatinum.health - 1;
        int expectedScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.UP, primaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            SuccessMoveEvent(bronzeStart, platinumStart, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            SuccessPushEvent(primaryBronze.id, primaryPlatinum.id, Vector2Int.up, primaryBronze.priority),
            SuccessDamageEvent(primaryBronze.id, 1, expectedBronzeHealth, primaryBronze.priority),
            SuccessDamageEvent(primaryPlatinum.id, 1, expectedPlatinumHealth, primaryBronze.priority),
            SuccessMoveEvent(platinumStart, expectedPlatinumPosition, primaryPlatinum.id, primaryBronze.priority, 0, 0),
            MoveResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(platinumStart, primaryBronze.position);
        Assert.AreEqual(expectedPlatinumPosition, primaryPlatinum.position);
        Assert.AreEqual(expectedBronzeHealth, primaryBronze.health);
        Assert.AreEqual(expectedPlatinumHealth, primaryPlatinum.health);
        Assert.AreEqual(expectedScore, testgame.primary.battery);
    }

    [Test]
    public void TestMoveRobotBlockByThird()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(3);
        setup.Add(primaryBronze.id, new Vector2Int(0, 2));
        setup.Add(primaryPlatinum.id, new Vector2Int(1, 2));
        setup.Add(secondarySilver.id, new Vector2Int(2, 2));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int platinumExpected = primaryPlatinum.position;
        Vector2Int secondaryExpected = secondarySilver.position;
        int expectedScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            FailMoveEvent(primaryExpected, primaryExpected + Vector2Int.right, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            FailPushEvent(primaryBronze.id, primaryPlatinum.id, Vector2Int.right, primaryBronze.priority),
            SuccessDamageEvent(primaryBronze.id, 1, primaryBronze.startingHealth - 1, primaryBronze.priority),
            SuccessDamageEvent(primaryPlatinum.id, 1, primaryPlatinum.startingHealth - 1, primaryBronze.priority),
            FailMoveEvent(platinumExpected, platinumExpected + Vector2Int.right, primaryPlatinum.id, primaryBronze.priority, 0, 0),
            SuccessBlockEvent(secondarySilver.position, primaryPlatinum.id, secondarySilver.name, primaryBronze.priority),
            SuccessDamageEvent(primaryPlatinum.id, 1, primaryPlatinum.startingHealth - 2, primaryBronze.priority),
            SuccessDamageEvent(secondarySilver.id, 1, secondarySilver.startingHealth - 1, primaryBronze.priority),
            SuccessBlockEvent(platinumExpected, primaryBronze.id, secondarySilver.name, primaryBronze.priority),
            MoveResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondarySilver.position);
        Assert.AreEqual(platinumExpected, primaryPlatinum.position);
        Assert.AreEqual(primaryBronze.startingHealth - 1, primaryBronze.health);
        Assert.AreEqual(secondarySilver.startingHealth - 1, secondarySilver.health);
        Assert.AreEqual(primaryPlatinum.startingHealth - 2, primaryPlatinum.health);
        Assert.AreEqual(expectedScore, testgame.primary.battery);
    }

    [Test]
    public void TestMoveRobotBlockEachOther()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(0, 2));
        setup.Add(secondaryBronze.id, new Vector2Int(2, 2));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        Vector2Int deniedPos = primaryBronze.position + Vector2Int.right;
        int expectedPrimaryScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER;
        int expectedSecondaryScore = testgame.secondary.battery - GameConstants.DEFAULT_MOVE_POWER;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryBronze.id),
            MoveCommand(Command.LEFT, secondaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            FailMoveEvent(primaryExpected, secondaryExpected, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            SuccessBlockEvent(deniedPos, primaryBronze.id, secondaryBronze.name, primaryBronze.priority),
            SuccessDamageEvent(primaryBronze.id, 1, primaryBronze.startingHealth - 1, primaryBronze.priority),
            FailMoveEvent(secondaryExpected, primaryExpected, secondaryBronze.id, secondaryBronze.priority, 0, GameConstants.DEFAULT_MOVE_POWER),
            SuccessBlockEvent(deniedPos, secondaryBronze.id, primaryBronze.name, secondaryBronze.priority),
            SuccessDamageEvent(secondaryBronze.id, 1, secondaryBronze.startingHealth - 1, secondaryBronze.priority),
            MoveResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
        Assert.AreEqual(primaryBronze.startingHealth - 1, primaryBronze.health);
        Assert.AreEqual(secondaryBronze.startingHealth - 1, secondaryBronze.health);
        Assert.AreEqual(expectedPrimaryScore, testgame.primary.battery);
        Assert.AreEqual(expectedSecondaryScore, testgame.secondary.battery);
    }

    [Test]
    public void TestMoveRobotBlockEachOtherWithPush()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(3);
        setup.Add(primaryGold.id, new Vector2Int(0, 2));
        setup.Add(primaryPlatinum.id, new Vector2Int(1, 2));
        setup.Add(secondaryGold.id, new Vector2Int(3, 2));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryGold.position;
        Vector2Int secondaryExpected = secondaryGold.position;
        Vector2Int platinumExpected = primaryPlatinum.position;
        Vector2Int deniedPos = secondaryGold.position + Vector2Int.left;
        int expectedPrimaryScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER;
        int expectedSecondaryScore = testgame.secondary.battery - GameConstants.DEFAULT_MOVE_POWER;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.RIGHT, primaryGold.id),
            MoveCommand(Command.LEFT, secondaryGold.id)
        );

        AssertExpectedGameEvents(events,
            FailMoveEvent(primaryExpected, primaryExpected + Vector2Int.right, primaryGold.id, primaryGold.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            FailPushEvent(primaryGold.id, primaryPlatinum.id, Vector2Int.right, primaryGold.priority),
            SuccessDamageEvent(primaryGold.id, 1, primaryGold.startingHealth - 1, primaryGold.priority),
            SuccessDamageEvent(primaryPlatinum.id, 1, primaryPlatinum.startingHealth - 1, primaryGold.priority),
            FailMoveEvent(platinumExpected, platinumExpected + Vector2Int.right, primaryPlatinum.id, primaryGold.priority, 0, 0),
            SuccessBlockEvent(deniedPos, primaryPlatinum.id, secondaryGold.name, primaryGold.priority),
            SuccessDamageEvent(primaryPlatinum.id, 1, primaryPlatinum.startingHealth - 2, primaryGold.priority),
            SuccessBlockEvent(platinumExpected, primaryGold.id, secondaryGold.name, primaryGold.priority),
            FailMoveEvent(secondaryExpected, deniedPos, secondaryGold.id, secondaryGold.priority, 0, GameConstants.DEFAULT_MOVE_POWER),
            SuccessBlockEvent(deniedPos, secondaryGold.id, primaryGold.name, secondaryGold.priority),
            SuccessDamageEvent(secondaryGold.id, 1, secondaryGold.startingHealth - 1, secondaryGold.priority),
            MoveResolveEvent(secondaryGold.priority)
        );
        Assert.AreEqual(primaryExpected, primaryGold.position);
        Assert.AreEqual(secondaryExpected, secondaryGold.position);
        Assert.AreEqual(platinumExpected, primaryPlatinum.position);
        Assert.AreEqual(primaryGold.startingHealth - 1, primaryGold.health);
        Assert.AreEqual(primaryPlatinum.startingHealth - 2, primaryPlatinum.health);
        Assert.AreEqual(secondaryGold.startingHealth - 1, secondaryGold.health);
        Assert.AreEqual(expectedPrimaryScore, testgame.primary.battery);
        Assert.AreEqual(expectedSecondaryScore, testgame.secondary.battery);
    }

    [Test]
    public void TestMoveSwapPositions()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 2));
        setup.Add(secondaryBronze.id, new Vector2Int(2, 2));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        int expectedPrimaryScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER;
        int expectedSecondaryScore = testgame.secondary.battery - GameConstants.DEFAULT_MOVE_POWER;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.LEFT, secondaryBronze.id),
            MoveCommand(Command.RIGHT, primaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            FailMoveEvent(primaryExpected, secondaryExpected, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            SuccessBlockEvent(secondaryExpected, primaryBronze.id, secondaryBronze.name, primaryBronze.priority),
            SuccessDamageEvent(primaryBronze.id, 1, primaryBronze.startingHealth - 1, primaryBronze.priority),
            FailMoveEvent(secondaryExpected, primaryExpected, secondaryBronze.id, secondaryBronze.priority, 0, GameConstants.DEFAULT_MOVE_POWER),
            SuccessBlockEvent(primaryExpected, secondaryBronze.id, primaryBronze.name, secondaryBronze.priority),
            SuccessDamageEvent(secondaryBronze.id, 1, secondaryBronze.startingHealth - 1, secondaryBronze.priority),
            MoveResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
        Assert.AreEqual(primaryBronze.startingHealth - 1, primaryBronze.health);
        Assert.AreEqual(secondaryBronze.startingHealth - 1, secondaryBronze.health);
        Assert.AreEqual(expectedPrimaryScore, testgame.primary.battery);
        Assert.AreEqual(expectedSecondaryScore, testgame.secondary.battery);
    }

    [Test]
    public void TestMoveAlongSameDirection()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 1));
        setup.Add(secondaryBronze.id, new Vector2Int(1, 2));
        BeforeEachTest(setup);

        Vector2Int primaryStart = primaryBronze.position;
        Vector2Int primaryExpected = primaryBronze.position + Vector2Int.up;
        Vector2Int secondaryExpected = secondaryBronze.position + Vector2Int.up;
        int expectedPrimaryScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER;
        int expectedSecondaryScore = testgame.secondary.battery - GameConstants.DEFAULT_MOVE_POWER;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.UP, primaryBronze.id),
            MoveCommand(Command.UP, secondaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            SuccessMoveEvent(primaryStart, primaryExpected, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            SuccessMoveEvent(primaryExpected, secondaryExpected, secondaryBronze.id, secondaryBronze.priority, 0, GameConstants.DEFAULT_MOVE_POWER),
            MoveResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
        Assert.AreEqual(expectedPrimaryScore, testgame.primary.battery);
        Assert.AreEqual(expectedSecondaryScore, testgame.secondary.battery);
    }

    [Test]
    public void TestMoveBlockedRobotGetsPushed()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(3);
        setup.Add(primaryGold.id, new Vector2Int(3, 3));
        setup.Add(primaryPlatinum.id, new Vector2Int(2, 2));
        setup.Add(secondaryGold.id, new Vector2Int(2, 3));
        BeforeEachTest(setup);

        Vector2Int platinumExpected = primaryPlatinum.position;
        Vector2Int secondaryStart = secondaryGold.position;
        Vector2Int secondaryExpected = secondaryGold.position + Vector2Int.left;
        int expectedPrimaryScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER;
        int expectedSecondaryScore = testgame.secondary.battery - GameConstants.DEFAULT_MOVE_POWER;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.DOWN, secondaryGold.id),
            MoveCommand(Command.LEFT, primaryGold.id)
        );

        AssertExpectedGameEvents(events,
            SuccessMoveEvent(secondaryStart + Vector2Int.right, secondaryStart, primaryGold.id, primaryGold.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            SuccessPushEvent(primaryGold.id, secondaryGold.id, Vector2Int.left, primaryGold.priority),
            SuccessDamageEvent(primaryGold.id, 1, primaryGold.startingHealth - 1, primaryGold.priority),
            SuccessDamageEvent(secondaryGold.id, 1, secondaryGold.startingHealth - 1, primaryGold.priority),
            SuccessMoveEvent(secondaryStart, secondaryExpected, secondaryGold.id, primaryGold.priority, 0, 0),
            FailMoveEvent(secondaryStart, platinumExpected, secondaryGold.id, secondaryGold.priority, 0, GameConstants.DEFAULT_MOVE_POWER),
            FailPushEvent(secondaryGold.id, primaryPlatinum.id, Vector2Int.down, secondaryGold.priority),
            SuccessDamageEvent(secondaryGold.id, 1, secondaryGold.startingHealth - 2, secondaryGold.priority),
            SuccessDamageEvent(primaryPlatinum.id, 1, primaryPlatinum.startingHealth - 1, secondaryGold.priority),
            FailMoveEvent(platinumExpected, platinumExpected + Vector2Int.down, primaryPlatinum.id, secondaryGold.priority, 0, 0),
            SuccessBlockEvent(platinumExpected + Vector2Int.down, primaryPlatinum.id, BlockEvent.BATTERY, secondaryGold.priority),
            SuccessDamageEvent(primaryPlatinum.id, 1, primaryPlatinum.startingHealth - 2, secondaryGold.priority),
            SuccessBlockEvent(platinumExpected, secondaryGold.id, BlockEvent.BATTERY, secondaryGold.priority),
            MoveResolveEvent(primaryGold.priority)
        );
        Assert.AreEqual(secondaryStart, primaryGold.position);
        Assert.AreEqual(secondaryExpected, secondaryGold.position);
        Assert.AreEqual(platinumExpected, primaryPlatinum.position);
        Assert.AreEqual(primaryGold.startingHealth - 1, primaryGold.health);
        Assert.AreEqual(primaryPlatinum.startingHealth - 2, primaryPlatinum.health);
        Assert.AreEqual(secondaryGold.startingHealth - 2, secondaryGold.health);
        Assert.AreEqual(expectedPrimaryScore, testgame.primary.battery);
        Assert.AreEqual(expectedSecondaryScore, testgame.secondary.battery);
    }

    [Test]
    public void TestMoveBothWantPushGetBlocked()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(3);
        setup.Add(primaryBronze.id, new Vector2Int(2, 2));
        setup.Add(primaryPlatinum.id, new Vector2Int(1, 2));
        setup.Add(secondaryBronze.id, new Vector2Int(1, 3));
        BeforeEachTest(setup);

        Vector2Int primaryExpected = primaryBronze.position;
        Vector2Int secondaryExpected = secondaryBronze.position;
        Vector2Int platinumExpected = primaryPlatinum.position;
        int expectedPrimaryScore = testgame.primary.battery - GameConstants.DEFAULT_MOVE_POWER;
        int expectedSecondaryScore = testgame.secondary.battery - GameConstants.DEFAULT_MOVE_POWER;
        List<GameEvent> events = SimulateCommands(
            MoveCommand(Command.DOWN, secondaryBronze.id),
            MoveCommand(Command.LEFT, primaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            FailMoveEvent(primaryExpected, platinumExpected, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_MOVE_POWER, 0),
            SuccessBlockEvent(platinumExpected, primaryBronze.id, secondaryBronze.name, primaryBronze.priority),
            SuccessDamageEvent(primaryBronze.id, 1, primaryBronze.startingHealth - 1, primaryBronze.priority),
            FailMoveEvent(secondaryExpected, platinumExpected, secondaryBronze.id, secondaryBronze.priority, 0, GameConstants.DEFAULT_MOVE_POWER),
            SuccessBlockEvent(platinumExpected, secondaryBronze.id, primaryBronze.name, secondaryBronze.priority),
            SuccessDamageEvent(secondaryBronze.id, 1, secondaryBronze.startingHealth - 1, secondaryBronze.priority),
            MoveResolveEvent(secondaryBronze.priority)
        );
        Assert.AreEqual(primaryExpected, primaryBronze.position);
        Assert.AreEqual(secondaryExpected, secondaryBronze.position);
        Assert.AreEqual(platinumExpected, primaryPlatinum.position);
        Assert.AreEqual(primaryBronze.startingHealth - 1, primaryBronze.health);
        Assert.AreEqual(secondaryBronze.startingHealth - 1, secondaryBronze.health);
        Assert.AreEqual(expectedPrimaryScore, testgame.primary.battery);
        Assert.AreEqual(expectedSecondaryScore, testgame.secondary.battery);
    }

    [Test]
    public void TestAttackSimple()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 1));
        setup.Add(secondaryBronze.id, new Vector2Int(1, 2));
        BeforeEachTest(setup);

        float expected = secondaryBronze.startingHealth - primaryBronze.attack;
        int expectedScore = testgame.primary.battery - GameConstants.DEFAULT_ATTACK_POWER;
        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.UP, primaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            SuccessAttackEvent(secondaryBronze.position, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_ATTACK_POWER, 0),
            SuccessDamageEvent(secondaryBronze.id, primaryBronze.attack, secondaryBronze.startingHealth - primaryBronze.attack, primaryBronze.priority),
            AttackResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(expected, secondaryBronze.health);
        Assert.AreEqual(expectedScore, testgame.primary.battery);
    }

    [Test]
    public void TestAttackMiss()
    {
        Dictionary<short, Vector2Int> setup = new Dictionary<short, Vector2Int>(2);
        setup.Add(primaryBronze.id, new Vector2Int(1, 1));
        setup.Add(secondaryBronze.id, new Vector2Int(1,2));
        BeforeEachTest(setup);

        float expected = secondaryBronze.startingHealth;
        Vector2Int expectedPosition = primaryBronze.position + Vector2Int.left;
        int expectedScore = testgame.primary.battery - GameConstants.DEFAULT_ATTACK_POWER;
        List<GameEvent> events = SimulateCommands(
            AttackCommand(Command.LEFT, primaryBronze.id)
        );

        AssertExpectedGameEvents(events,
            SuccessAttackEvent(expectedPosition, primaryBronze.id, primaryBronze.priority, GameConstants.DEFAULT_ATTACK_POWER, 0),
            SuccessMissEvent(expectedPosition, primaryBronze.id, primaryBronze.priority),
            AttackResolveEvent(primaryBronze.priority)
        );
        Assert.AreEqual(expected, secondaryBronze.health);
        Assert.AreEqual(expectedScore, testgame.primary.battery);
    }
    
    [Test]
    public void TestAttackBattery()
    {
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
    /*
    [Test]
    public void TestAttackDeath()
    {
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
        Assert.IsInstanceOf<DamageEvent>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Death>(events.Get(2));
        Assert.AreEqual(primaryBronze.startingHealth, primaryBronze.health);
        Assert.AreEqual(Map.NULL_VEC, primaryBronze.position);
    }

    [Test]
    public void TestAttackDeathFailedMove()
    {
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
        Assert.IsInstanceOf<DamageEvent>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Death>(events.Get(2));
        Assert.AreEqual(primaryBronze.startingHealth, primaryBronze.health);
        Assert.AreEqual(Map.NULL_VEC, primaryBronze.position);
    }

    [Test]
    public void TestAttackMultipleSamePriority()
    {
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
        Assert.IsInstanceOf<DamageEvent>(events.Get(1));
        Assert.IsInstanceOf<GameEvent.Attack>(events.Get(2));
        Assert.IsInstanceOf<DamageEvent>(events.Get(3));
        Assert.AreEqual(expected, secondarySilver.health);
    }
    */
}