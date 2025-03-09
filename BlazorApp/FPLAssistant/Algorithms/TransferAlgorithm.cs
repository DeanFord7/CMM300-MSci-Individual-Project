using FPLAssistant.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPLAssistant.Algorithms
{
    public class TransferAlgorithm
    {
        private readonly List<PlayerData> _currentTeam = new List<PlayerData>();
        private readonly List<PlayerData> _playersWithPredictions = new List<PlayerData>();
        private double? _remainingBudget;
        private readonly int _numberOfTransfers;
        private readonly double _mutationRate;
        private readonly Random _random = new Random();

        public TransferAlgorithm(List<PlayerData> currentTeam, List<PlayerData> playersWithPredictions, double? remainingBudget, int numberOfTransfers, double mutationRate = 0.05)
        {
            _currentTeam = currentTeam;
            _playersWithPredictions = playersWithPredictions;
            _remainingBudget = remainingBudget;
            _numberOfTransfers = numberOfTransfers;
            _mutationRate = mutationRate;
        }

        public List<RecommendedTransfer> Run()
        {
            List<List<RecommendedTransfer>> population = GenerateInitialPopulation(100);
            List<RecommendedTransfer> bestSolution = null;
            double? bestFitness = double.MinValue;

            for (int gen = 0; gen < 1000; gen++) 
            {
                List<List<RecommendedTransfer>> nextGen = new List<List<RecommendedTransfer>>();

                foreach (var individual in population) 
                {
                    double? fitness = CalculateFitness(individual);
                    if (fitness > bestFitness) 
                    { 
                        bestFitness = fitness;
                        bestSolution = individual;
                    }
                }

                List<Tuple<List<RecommendedTransfer>, List<RecommendedTransfer>>> parents = SelectParents(population);

                foreach (var parent in parents)
                {
                    List<RecommendedTransfer> offspring = Crossover(parent.Item1, parent.Item2);

                    Mutate(offspring);

                    nextGen.Add(offspring);
                }

                population = nextGen;
            }

            return bestSolution;
        }

        private List<List<RecommendedTransfer>> GenerateInitialPopulation(int populationSize)
        {
            List<List<RecommendedTransfer>> population = new List<List<RecommendedTransfer>>();

            for (int i = 0; i < populationSize; i++)
            {
                var transfers = GenerateRandomTransfers();
                population.Add(transfers);
            }

            return population;
        }

        private List<RecommendedTransfer> GenerateRandomTransfers()
        {
            List<RecommendedTransfer> transfers = new List<RecommendedTransfer>();
            HashSet<int> selectedOutPlayers = new HashSet<int>(); // Track unique PlayerOut
            HashSet<int> selectedInPlayers = new HashSet<int>();  // Track unique PlayerIn
            Dictionary<int, int> teamCounts = new Dictionary<int, int>(); // Track max 3 per team
            double? availableBudget = _remainingBudget; // Remaining budget

            int attempts = 0;
            while (transfers.Count < _numberOfTransfers && attempts < 1000) // Retry limit
            {
                attempts++;

                // Select a unique PlayerOut who hasn't been transferred yet
                var availableOutPlayers = _currentTeam
                    .Where(p => !selectedOutPlayers.Contains(p.Id)) // Ensure unique
                    .ToList();

                if (availableOutPlayers.Count == 0) break; // No more valid players

                var playerOut = availableOutPlayers[_random.Next(availableOutPlayers.Count)];
                selectedOutPlayers.Add(playerOut.Id); // Mark as used

                // Find a valid PlayerIn who hasn't been used
                var possiblePlayerIns = _playersWithPredictions
                    .Where(p => p.Position == playerOut.Position) // Same position
                    .Where(p => !teamCounts.ContainsKey(p.Team) || teamCounts[p.Team] < 3) // Max 3 per team
                    .Where(p => !_currentTeam.Any(i => i.Id == p.Id)) // Not already in team
                    .Where(p => !selectedInPlayers.Contains(p.Id)) // Ensure unique PlayerIn
                    .Where(p => p.Cost <= availableBudget + (playerOut.SellingPrice * 10)) // Budget check
                    .OrderByDescending(p => p.PredictedScore) // Prefer highest score
                    .ToList();

                if (possiblePlayerIns.Count == 0) continue; // Skip if no valid options

                var playerIn = possiblePlayerIns.First(); // Pick the best one
                double? transferCost = playerIn.Cost - (playerOut.SellingPrice * 10);

                if (transferCost <= availableBudget)
                {
                    transfers.Add(new RecommendedTransfer
                    {
                        PlayerOut = playerOut,
                        PlayerIn = playerIn,
                        BudgetImpact = transferCost
                    });

                    availableBudget -= transferCost; // Deduct budget
                    selectedInPlayers.Add(playerIn.Id); // Prevent duplicate PlayerIn

                    // Track team constraints
                    if (!teamCounts.ContainsKey(playerIn.Team))
                    {
                        teamCounts[playerIn.Team] = 0;
                    }
                    teamCounts[playerIn.Team]++;
                }
            }

            // Ensure we return exactly `_numberOfTransfers` or retry
            return transfers.Count == _numberOfTransfers ? transfers : GenerateRandomTransfers();
        }




        private double? CalculateFitness(List<RecommendedTransfer> transfers)
        {
            double? totalScoreDifference = 0;
            double? totalCost = 0;
            Dictionary<int, int> teamCounts = new Dictionary<int, int>();

            foreach (RecommendedTransfer transfer in transfers)
            {
                if (transfer.PlayerOut.Position != transfer.PlayerIn.Position)
                {
                    return double.MinValue;
                }

                if (!teamCounts.ContainsKey(transfer.PlayerIn.Team))
                {
                    teamCounts[transfer.PlayerIn.Team] = 0;
                }

                if (teamCounts[transfer.PlayerIn.Team] > 3)
                {
                    return double.MinValue;
                }

                totalScoreDifference += transfer.PlayerIn.PredictedScore - transfer.PlayerOut.PredictedScore;

                totalCost += transfer.BudgetImpact;

                teamCounts[transfer.PlayerIn.Team]++;
            }

            return totalScoreDifference - (totalCost > _remainingBudget ? (totalCost - _remainingBudget) * 10 : 0);
        }

        private List<Tuple<List<RecommendedTransfer>, List<RecommendedTransfer>>> SelectParents(List<List<RecommendedTransfer>> population)
        {
            List<Tuple<List<RecommendedTransfer>, List<RecommendedTransfer>>> parents = new List<Tuple<List<RecommendedTransfer>, List<RecommendedTransfer>>>();

            for (int i = 0; i < population.Count / 2; i++)
            {
                var parent1 = population[_random.Next(population.Count)];
                var parent2 = population[_random.Next(population.Count)];

                // Add the selected parents as a tuple of lists
                parents.Add(new Tuple<List<RecommendedTransfer>, List<RecommendedTransfer>>(parent1, parent2));
            }

            return parents;
        }

        private List<RecommendedTransfer> Crossover(List<RecommendedTransfer> parent1, List<RecommendedTransfer> parent2)
        {
            HashSet<int> usedPlayerOuts = new HashSet<int>();
            HashSet<int> usedPlayerIns = new HashSet<int>();

            List<RecommendedTransfer> offspring = new List<RecommendedTransfer>();

            foreach (var transfer in parent1)
            {
                if (!usedPlayerOuts.Contains(transfer.PlayerOut.Id) && !usedPlayerIns.Contains(transfer.PlayerIn.Id))
                {
                    offspring.Add(transfer);
                    usedPlayerOuts.Add(transfer.PlayerOut.Id);
                    usedPlayerIns.Add(transfer.PlayerIn.Id);
                }
            }

            foreach (var transfer in parent2)
            {
                if (!usedPlayerOuts.Contains(transfer.PlayerOut.Id) && !usedPlayerIns.Contains(transfer.PlayerIn.Id))
                {
                    offspring.Add(transfer);
                    usedPlayerOuts.Add(transfer.PlayerOut.Id);
                    usedPlayerIns.Add(transfer.PlayerIn.Id);
                }

                if (offspring.Count >= _numberOfTransfers) break; // Stop when we reach the required number
            }

            return offspring;
        }



        private void Mutate(List<RecommendedTransfer> offspring)
        {
            if (_random.NextDouble() < _mutationRate)
            {
                int index1 = _random.Next(offspring.Count);
                int index2 = _random.Next(offspring.Count);

                var temp = offspring[index1];
                offspring[index1] = offspring[index2];
                offspring[index2] = temp;
            }
        }
    }
}
