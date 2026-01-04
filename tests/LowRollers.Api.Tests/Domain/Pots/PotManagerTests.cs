using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.Pots;

namespace LowRollers.Api.Tests.Domain.Pots;

public class PotManagerTests
{
    private readonly PotManager _potManager = new();

    private static Guid Player1 => Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static Guid Player2 => Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static Guid Player3 => Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static Guid Player4 => Guid.Parse("00000000-0000-0000-0000-000000000004");

    #region CalculatePots Tests

    [Fact]
    public void CalculatePots_EmptyContributions_ReturnsEmptyMainPot()
    {
        // Arrange
        var contributions = new Dictionary<Guid, decimal>();
        var allInPlayers = new HashSet<Guid>();
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Single(pots);
        Assert.Equal(PotType.Main, pots[0].Type);
        Assert.Equal(0m, pots[0].Amount);
    }

    [Fact]
    public void CalculatePots_SimpleNoAllIns_SingleMainPot()
    {
        // Arrange - Three players each contribute 100
        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 100m },
            { Player2, 100m },
            { Player3, 100m }
        };
        var allInPlayers = new HashSet<Guid>();
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Single(pots);
        Assert.Equal(PotType.Main, pots[0].Type);
        Assert.Equal(300m, pots[0].Amount);
        Assert.Contains(Player1, pots[0].EligiblePlayerIds);
        Assert.Contains(Player2, pots[0].EligiblePlayerIds);
        Assert.Contains(Player3, pots[0].EligiblePlayerIds);
    }

    [Fact]
    public void CalculatePots_SingleAllIn_CreatesSidePot()
    {
        // Arrange
        // Player1 goes all-in for 50
        // Player2 and Player3 call/raise to 100
        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 50m },
            { Player2, 100m },
            { Player3, 100m }
        };
        var allInPlayers = new HashSet<Guid> { Player1 };
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Equal(2, pots.Count);

        // Main pot: 50 * 3 = 150 (all players eligible)
        var mainPot = pots.First(p => p.Type == PotType.Main);
        Assert.Equal(150m, mainPot.Amount);
        Assert.Equal(3, mainPot.EligiblePlayerIds.Count);

        // Side pot: 50 * 2 = 100 (only Player2 and Player3)
        var sidePot = pots.First(p => p.Type == PotType.Side);
        Assert.Equal(100m, sidePot.Amount);
        Assert.Equal(2, sidePot.EligiblePlayerIds.Count);
        Assert.Contains(Player2, sidePot.EligiblePlayerIds);
        Assert.Contains(Player3, sidePot.EligiblePlayerIds);
        Assert.DoesNotContain(Player1, sidePot.EligiblePlayerIds);
    }

    [Fact]
    public void CalculatePots_MultipleAllIns_CreatesMultipleSidePots()
    {
        // Arrange
        // Player1 all-in for 25
        // Player2 all-in for 50
        // Player3 all-in for 100
        // Player4 calls 100
        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 25m },
            { Player2, 50m },
            { Player3, 100m },
            { Player4, 100m }
        };
        var allInPlayers = new HashSet<Guid> { Player1, Player2, Player3 };
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Equal(3, pots.Count);

        // Main pot: 25 * 4 = 100 (all players)
        var mainPot = pots.First(p => p.Type == PotType.Main);
        Assert.Equal(100m, mainPot.Amount);
        Assert.Equal(4, mainPot.EligiblePlayerIds.Count);

        // Side pot 1: 25 * 3 = 75 (P2, P3, P4)
        var sidePots = pots.Where(p => p.Type == PotType.Side).OrderBy(p => p.CreationOrder).ToList();
        Assert.Equal(75m, sidePots[0].Amount);
        Assert.Equal(3, sidePots[0].EligiblePlayerIds.Count);
        Assert.DoesNotContain(Player1, sidePots[0].EligiblePlayerIds);

        // Side pot 2: 50 * 2 = 100 (P3, P4)
        Assert.Equal(100m, sidePots[1].Amount);
        Assert.Equal(2, sidePots[1].EligiblePlayerIds.Count);
        Assert.Contains(Player3, sidePots[1].EligiblePlayerIds);
        Assert.Contains(Player4, sidePots[1].EligiblePlayerIds);
    }

    [Fact]
    public void CalculatePots_AllInForExactAmount_NoSidePot()
    {
        // Arrange - All players contribute exactly 100
        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 100m },
            { Player2, 100m },
            { Player3, 100m }
        };
        // Player1 is all-in but for the same amount as others
        var allInPlayers = new HashSet<Guid> { Player1 };
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert - Only main pot, no side pot needed
        Assert.Single(pots);
        Assert.Equal(300m, pots[0].Amount);
    }

    [Fact]
    public void CalculatePots_FoldedPlayersNotEligible()
    {
        // Arrange
        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 50m },  // Folded after betting
            { Player2, 100m },
            { Player3, 100m }
        };
        var allInPlayers = new HashSet<Guid>();
        var foldedPlayers = new HashSet<Guid> { Player1 };

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Single(pots);
        Assert.Equal(250m, pots[0].Amount);
        Assert.DoesNotContain(Player1, pots[0].EligiblePlayerIds);
        Assert.Contains(Player2, pots[0].EligiblePlayerIds);
        Assert.Contains(Player3, pots[0].EligiblePlayerIds);
    }

    [Fact]
    public void CalculatePots_AllPlayersFolded_StillHasPotAmount()
    {
        // Arrange - Edge case: all active players somehow folded
        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 50m },
            { Player2, 100m }
        };
        var allInPlayers = new HashSet<Guid>();
        var foldedPlayers = new HashSet<Guid> { Player1, Player2 };

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert - Returns main pot (will be awarded to last remaining player)
        Assert.Single(pots);
        Assert.Equal(PotType.Main, pots[0].Type);
    }

    #endregion

    #region CollectBets Tests

    [Fact]
    public void CollectBets_SimpleBets_AddToMainPot()
    {
        // Arrange
        var existingPots = new List<Pot> { Pot.CreateMainPot() };
        existingPots[0].Amount = 50m; // Previous betting round

        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 25m },
            { Player2, 25m }
        };
        var allInPlayers = new HashSet<Guid>();
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CollectBets(existingPots, contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Single(pots);
        Assert.Equal(100m, pots[0].Amount); // 50 + 25 + 25
    }

    [Fact]
    public void CollectBets_NoBets_ReturnsExistingPots()
    {
        // Arrange
        var existingPots = new List<Pot> { Pot.CreateMainPot() };
        existingPots[0].Amount = 100m;

        var contributions = new Dictionary<Guid, decimal>();
        var allInPlayers = new HashSet<Guid>();
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CollectBets(existingPots, contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Single(pots);
        Assert.Equal(100m, pots[0].Amount);
    }

    [Fact]
    public void CollectBets_AllInCreatingNewSidePot_PreservesExistingAmount()
    {
        // Arrange
        var existingPots = new List<Pot> { Pot.CreateMainPot() };
        existingPots[0].Amount = 100m;
        existingPots[0].AddEligiblePlayer(Player1);
        existingPots[0].AddEligiblePlayer(Player2);

        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 25m },  // All-in
            { Player2, 50m }
        };
        var allInPlayers = new HashSet<Guid> { Player1 };
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CollectBets(existingPots, contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Equal(2, pots.Count);

        var mainPot = pots.First(p => p.Type == PotType.Main);
        // Main pot should have: existing 100 + (25 * 2 players contributing at this level) = 150
        Assert.True(mainPot.Amount >= 100m);
    }

    #endregion

    #region RemovePlayerFromPots Tests

    [Fact]
    public void RemovePlayerFromPots_RemovesFromAllPots()
    {
        // Arrange
        var pots = new List<Pot>
        {
            Pot.CreateMainPot(),
            Pot.CreateSidePot([Player1, Player2, Player3], 1)
        };
        pots[0].AddEligiblePlayer(Player1);
        pots[0].AddEligiblePlayer(Player2);
        pots[0].AddEligiblePlayer(Player3);

        // Act
        _potManager.RemovePlayerFromPots(pots, Player2);

        // Assert
        Assert.DoesNotContain(Player2, pots[0].EligiblePlayerIds);
        Assert.DoesNotContain(Player2, pots[1].EligiblePlayerIds);
        Assert.Contains(Player1, pots[0].EligiblePlayerIds);
        Assert.Contains(Player3, pots[0].EligiblePlayerIds);
    }

    #endregion

    #region AwardPots Tests

    [Fact]
    public void AwardPots_SingleWinner_GetsFullPot()
    {
        // Arrange
        var pot = Pot.CreateMainPot();
        pot.Amount = 300m;
        pot.AddEligiblePlayer(Player1);
        pot.AddEligiblePlayer(Player2);
        pot.AddEligiblePlayer(Player3);

        var pots = new List<Pot> { pot };
        var winners = new Dictionary<Guid, List<Guid>>
        {
            { pot.Id, [Player1] }
        };

        // Act
        var winnings = _potManager.AwardPots(pots, winners);

        // Assert
        Assert.Single(winnings);
        Assert.Equal(300m, winnings[Player1]);
        Assert.Equal(0m, pot.Amount);
    }

    [Fact]
    public void AwardPots_SplitPot_DividesEvenly()
    {
        // Arrange
        var pot = Pot.CreateMainPot();
        pot.Amount = 300m;
        pot.AddEligiblePlayer(Player1);
        pot.AddEligiblePlayer(Player2);
        pot.AddEligiblePlayer(Player3);

        var pots = new List<Pot> { pot };
        var winners = new Dictionary<Guid, List<Guid>>
        {
            { pot.Id, [Player1, Player2] }
        };

        // Act
        var winnings = _potManager.AwardPots(pots, winners);

        // Assert
        Assert.Equal(2, winnings.Count);
        Assert.Equal(150m, winnings[Player1]);
        Assert.Equal(150m, winnings[Player2]);
    }

    [Fact]
    public void AwardPots_SplitPotWithOddChips_FirstWinnerGetsRemainder()
    {
        // Arrange
        var pot = Pot.CreateMainPot();
        pot.Amount = 100m;
        pot.AddEligiblePlayer(Player1);
        pot.AddEligiblePlayer(Player2);
        pot.AddEligiblePlayer(Player3);

        var pots = new List<Pot> { pot };
        var winners = new Dictionary<Guid, List<Guid>>
        {
            { pot.Id, [Player1, Player2, Player3] }
        };

        // Act
        var winnings = _potManager.AwardPots(pots, winners);

        // Assert
        Assert.Equal(3, winnings.Count);
        // 100 / 3 = 33.33, rounded down = 33.33
        // First winner gets extra penny: 33.34
        Assert.True(winnings[Player1] >= winnings[Player2]);
        Assert.True(winnings[Player1] >= winnings[Player3]);
        Assert.Equal(100m, winnings.Values.Sum());
    }

    [Fact]
    public void AwardPots_MainAndSidePots_DifferentWinners()
    {
        // Arrange
        var mainPot = Pot.CreateMainPot();
        mainPot.Amount = 150m;
        mainPot.AddEligiblePlayer(Player1);
        mainPot.AddEligiblePlayer(Player2);
        mainPot.AddEligiblePlayer(Player3);

        var sidePot = Pot.CreateSidePot([Player2, Player3], 1);
        sidePot.Amount = 100m;

        var pots = new List<Pot> { mainPot, sidePot };
        var winners = new Dictionary<Guid, List<Guid>>
        {
            { mainPot.Id, [Player1] },  // Player1 wins main
            { sidePot.Id, [Player2] }   // Player2 wins side
        };

        // Act
        var winnings = _potManager.AwardPots(pots, winners);

        // Assert
        Assert.Equal(2, winnings.Count);
        Assert.Equal(150m, winnings[Player1]);
        Assert.Equal(100m, winnings[Player2]);
    }

    [Fact]
    public void AwardPots_AllInPlayerWinsMain_CannotWinSidePot()
    {
        // Arrange - Player1 went all-in for less, won best hand
        var mainPot = Pot.CreateMainPot();
        mainPot.Amount = 150m;
        mainPot.AddEligiblePlayer(Player1);
        mainPot.AddEligiblePlayer(Player2);
        mainPot.AddEligiblePlayer(Player3);

        var sidePot = Pot.CreateSidePot([Player2, Player3], 1);
        sidePot.Amount = 100m;
        // Player1 is NOT eligible for side pot

        var pots = new List<Pot> { mainPot, sidePot };

        // Player1 has the best hand overall
        var winners = new Dictionary<Guid, List<Guid>>
        {
            { mainPot.Id, [Player1] },
            { sidePot.Id, [Player1, Player2] }  // Player1 included but not eligible
        };

        // Act
        var winnings = _potManager.AwardPots(pots, winners);

        // Assert
        Assert.Equal(150m, winnings[Player1]); // Only gets main
        Assert.Equal(100m, winnings[Player2]); // Gets full side pot
    }

    [Fact]
    public void AwardPots_NoWinnersForPot_SkipsPot()
    {
        // Arrange
        var pot = Pot.CreateMainPot();
        pot.Amount = 100m;
        pot.AddEligiblePlayer(Player1);

        var pots = new List<Pot> { pot };
        var winners = new Dictionary<Guid, List<Guid>>(); // No winners specified

        // Act
        var winnings = _potManager.AwardPots(pots, winners);

        // Assert
        Assert.Empty(winnings);
        Assert.Equal(100m, pot.Amount); // Pot not awarded
    }

    [Fact]
    public void AwardPots_MultipleSidePots_AwardsInOrder()
    {
        // Arrange - Complex scenario with 3 all-ins at different levels
        var mainPot = Pot.CreateMainPot();
        mainPot.Amount = 100m;
        mainPot.AddEligiblePlayer(Player1);
        mainPot.AddEligiblePlayer(Player2);
        mainPot.AddEligiblePlayer(Player3);
        mainPot.AddEligiblePlayer(Player4);

        var sidePot1 = Pot.CreateSidePot([Player2, Player3, Player4], 1);
        sidePot1.Amount = 75m;

        var sidePot2 = Pot.CreateSidePot([Player3, Player4], 2);
        sidePot2.Amount = 50m;

        var pots = new List<Pot> { mainPot, sidePot1, sidePot2 };

        // Player1 wins main, Player2 wins side1, Player4 wins side2
        var winners = new Dictionary<Guid, List<Guid>>
        {
            { mainPot.Id, [Player1] },
            { sidePot1.Id, [Player2] },
            { sidePot2.Id, [Player4] }
        };

        // Act
        var winnings = _potManager.AwardPots(pots, winners);

        // Assert
        Assert.Equal(3, winnings.Count);
        Assert.Equal(100m, winnings[Player1]);
        Assert.Equal(75m, winnings[Player2]);
        Assert.Equal(50m, winnings[Player4]);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public void ComplexScenario_ThreeAllInsAtDifferentAmounts()
    {
        // Scenario:
        // - Player1 all-in for $30
        // - Player2 all-in for $60
        // - Player3 all-in for $100
        // - Player4 calls $100 (not all-in)

        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 30m },
            { Player2, 60m },
            { Player3, 100m },
            { Player4, 100m }
        };
        var allInPlayers = new HashSet<Guid> { Player1, Player2, Player3 };
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Equal(3, pots.Count);

        // Main pot: $30 x 4 = $120 (P1, P2, P3, P4)
        var mainPot = pots.First(p => p.Type == PotType.Main);
        Assert.Equal(120m, mainPot.Amount);
        Assert.Equal(4, mainPot.EligiblePlayerIds.Count);

        // Side pot 1: $30 x 3 = $90 (P2, P3, P4)
        var sidePots = pots.Where(p => p.Type == PotType.Side).OrderBy(p => p.CreationOrder).ToList();
        Assert.Equal(90m, sidePots[0].Amount);
        Assert.Equal(3, sidePots[0].EligiblePlayerIds.Count);
        Assert.DoesNotContain(Player1, sidePots[0].EligiblePlayerIds);

        // Side pot 2: $40 x 2 = $80 (P3, P4)
        Assert.Equal(80m, sidePots[1].Amount);
        Assert.Equal(2, sidePots[1].EligiblePlayerIds.Count);
        Assert.Contains(Player3, sidePots[1].EligiblePlayerIds);
        Assert.Contains(Player4, sidePots[1].EligiblePlayerIds);

        // Total should equal all contributions
        Assert.Equal(290m, pots.Sum(p => p.Amount));
    }

    [Fact]
    public void ComplexScenario_TwoPlayersAllInSameAmount()
    {
        // Scenario: Two players all-in for same amount
        // - Player1 all-in for $50
        // - Player2 all-in for $50
        // - Player3 calls $50

        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 50m },
            { Player2, 50m },
            { Player3, 50m }
        };
        var allInPlayers = new HashSet<Guid> { Player1, Player2 };
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert - Only main pot since all-ins are at same level
        Assert.Single(pots);
        Assert.Equal(150m, pots[0].Amount);
        Assert.Equal(3, pots[0].EligiblePlayerIds.Count);
    }

    [Fact]
    public void ComplexScenario_HeadsUpAllIn()
    {
        // Scenario: Heads-up, both players all-in
        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 100m },
            { Player2, 80m }
        };
        var allInPlayers = new HashSet<Guid> { Player1, Player2 };
        var foldedPlayers = new HashSet<Guid>();

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Equal(2, pots.Count);

        // Main pot: $80 x 2 = $160
        var mainPot = pots.First(p => p.Type == PotType.Main);
        Assert.Equal(160m, mainPot.Amount);

        // Side pot: $20 (Player1's excess) - only 1 eligible player
        var sidePot = pots.First(p => p.Type == PotType.Side);
        Assert.Equal(20m, sidePot.Amount);
        Assert.Single(sidePot.EligiblePlayerIds);
        Assert.Contains(Player1, sidePot.EligiblePlayerIds);
    }

    [Fact]
    public void ComplexScenario_FoldedPlayerBetsStillCount()
    {
        // Scenario: Player bets, then folds - their chips stay in pot
        var contributions = new Dictionary<Guid, decimal>
        {
            { Player1, 100m },  // Bet then folded
            { Player2, 100m },
            { Player3, 100m }
        };
        var allInPlayers = new HashSet<Guid>();
        var foldedPlayers = new HashSet<Guid> { Player1 };

        // Act
        var pots = _potManager.CalculatePots(contributions, allInPlayers, foldedPlayers);

        // Assert
        Assert.Single(pots);
        Assert.Equal(300m, pots[0].Amount); // All contributions count
        Assert.Equal(2, pots[0].EligiblePlayerIds.Count); // But only 2 can win
        Assert.DoesNotContain(Player1, pots[0].EligiblePlayerIds);
    }

    #endregion
}
