using FPLAssistant.Models;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;
using FPLAssistant.Services;
using FPLAssistant.Algorithms;

namespace FPLAssistant.Repositories
{
    public interface IFPLRepository
    {
        Task<BootStrapAPIResponse> GetBootStrapAPIResponse();
        Task<PlayerData> GetPlayerData(int playerId);
        Task<bool> GeneratePlayerDataCSV();
        Task<FixtureData> GetPlayerFixtureData(int playerId);
        Task<int?> GetPredictionForPlayer(int playerId);
        Task<History> GetPlayerAverages(FixtureData playerFixtureData);
        Task<List<PlayerData>> GetAllPlayers();
        Task<List<PlayerData>> GetLatestPlayerData(List<PlayerData> playerData);
        Task<List<PlayerData>> PredictTeamScores(List<PlayerData> players);
        Task<List<PlayerData>> PredictAllPlayers();
        Task<List<RecommendedTransfer>> RecommendTransfers(int numberOfTransfers);
    }

    public class FPLRepository : IFPLRepository
    {
        private readonly HttpClient _httpClient;
        private readonly PythonRepository _pythonRepository;
        private readonly TeamStateService _teamStateService;
        public FPLRepository(HttpClient httpClient, PythonRepository pythonRepository, TeamStateService teamStateService) 
        { 
            _httpClient = httpClient;
            _pythonRepository = pythonRepository;
            _teamStateService = teamStateService;
        }

        public async Task<BootStrapAPIResponse> GetBootStrapAPIResponse()
        {
            string generalInfoUrl = "https://fantasy.premierleague.com/api/bootstrap-static";

            try
            {
                var generalResponse = await _httpClient.GetAsync(generalInfoUrl);

                var responseContent = await generalResponse.Content.ReadAsStringAsync();

                BootStrapAPIResponse bootStrapAPIResponse = JsonSerializer.Deserialize<BootStrapAPIResponse>(responseContent);

                return bootStrapAPIResponse;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<PlayerData> GetPlayerData(int playerId)
        {
            PlayerData playerData = new PlayerData();

            string generalInfoUrl = "https://fantasy.premierleague.com/api/bootstrap-static";

            BootStrapAPIResponse bootStrapAPIResponse = await GetBootStrapAPIResponse();

            playerData = bootStrapAPIResponse.Elements.Where(i => i.Id == playerId).FirstOrDefault();

            return playerData;
        }

        public async Task<bool> GeneratePlayerDataCSV()
        {
            try
            {
                BootStrapAPIResponse bootStrapAPIResponse = await GetBootStrapAPIResponse();

                List<History> fullFixtureHistory = new List<History>();

                foreach (var player in bootStrapAPIResponse.Elements)
                {
                    FixtureData playerFixtureData = await GetPlayerFixtureData(player.Id);

                    List<History> playerHistory = playerFixtureData.History;

                    foreach (var history in playerHistory)
                    {
                        history.Position = player.Position;
                    }

                    fullFixtureHistory.AddRange(playerHistory);
                }

                bool result = await _pythonRepository.GenerateCsv(fullFixtureHistory);
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        public async Task<FixtureData> GetPlayerFixtureData(int playerId)
        {
            try
            {
                string playerUrl = $"https://fantasy.premierleague.com/api/element-summary/{playerId}/";

                var response = await _httpClient.GetStringAsync(playerUrl);

                FixtureData playerFixtureData = null;

                try
                {
                    playerFixtureData = JsonSerializer.Deserialize<FixtureData>(response);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Deserialization error: {ex.Message}");
                }

                return playerFixtureData;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public async Task<int?> GetPredictionForPlayer(int playerId)
        {
            FixtureData playerFixtureData = await GetPlayerFixtureData(playerId);

            if (playerFixtureData != null)
            {
                History averagedHistory = await GetPlayerAverages(playerFixtureData);

                if (averagedHistory != null)
                {
                    BootStrapAPIResponse bootStrapAPIResponse = await GetBootStrapAPIResponse();

                    PlayerData playerData = bootStrapAPIResponse.Elements.Where(i => i.Id == playerId).FirstOrDefault();

                    averagedHistory.Position = playerData.Position;

                    int? predictedScore = await _pythonRepository.PredictPlayerScore(averagedHistory);

                    return predictedScore;
                }
            }
            return null;
        }

        /// <summary>
        /// Use past fixture history to get averages for the player to be used in the prediction
        /// </summary>
        /// <param name="playerFixtureData"></param>
        /// <returns></returns>
        public async Task<History> GetPlayerAverages(FixtureData playerFixtureData)
        {
            var history = playerFixtureData.History;
            int position = history.Select(i => i.Position).FirstOrDefault();

            if (history == null || history.Count == 0)
            {
                return new History
                {
                    PlayerId = 0,
                    Fixture = playerFixtureData.Fixtures.FirstOrDefault()?.Id ?? 0,
                    OpponentTeam = playerFixtureData.Fixtures.FirstOrDefault()?.TeamAway ?? 0,
                    WasHome = playerFixtureData.Fixtures.FirstOrDefault()?.IsHome ?? true,
                    Minutes = 0,
                    GoalsScored = 0,
                    Assists = 0,
                    CleanSheets = 0,
                    GoalsConceded = 0,
                    YellowCards = 0,
                    RedCards = 0,
                    Saves = 0,
                    ExpectedGoals = "0.0",
                    ExpectedAssists = "0.0",
                    ExpectedGoalInvolvements = "0.0",
                    ExpectedGoalsConceded = "0.0"
                };
            }

            int count = history.Count;
            History predictedMatchHistory = new History
            {
                PlayerId = history.First().PlayerId,
                Fixture = playerFixtureData.Fixtures.FirstOrDefault()?.Id ?? 0,
                OpponentTeam = playerFixtureData.Fixtures.FirstOrDefault()?.TeamAway ?? 0,
                WasHome = playerFixtureData.Fixtures.FirstOrDefault()?.IsHome ?? true,
            };

            double totalMinutes = 0, totalGoals = 0, totalAssists = 0, totalGoalsConceded = 0;
            double totalOwnGoals = 0, totalPenaltiesSaved = 0, totalPenaltiesMissed = 0;
            double totalYellowCards = 0, totalRedCards = 0, totalSaves = 0, totalBonus = 0, totalBonusPoints = 0;
            double totalInfluence = 0, totalCreativity = 0, totalThreat = 0, totalICT = 0;
            double totalXG = 0, totalXA = 0, totalXGI = 0, totalXGC = 0;

            foreach (var item in history)
            {
                int index = history.IndexOf(item);
                double.TryParse(item.Influence, out double influence);
                double.TryParse(item.Creativity, out double creativity);
                double.TryParse(item.Threat, out double threat);
                double.TryParse(item.IctIndex, out double ictIndex);
                double.TryParse(item.ExpectedGoals, out double expectedGoals);
                double.TryParse(item.ExpectedAssists, out double expectedAssists);
                double.TryParse(item.ExpectedGoalInvolvements, out double expectedGoalInvolvements);
                double.TryParse(item.ExpectedGoalsConceded, out double expectedGoalsConceded);

                // Base weighting: double last 5 matches
                double matchWeight = index >= history.Count - 5 ? 2.0 : 1.0;

                // Apply positional weighting
                double goalsMultiplier = 1.0, assistsMultiplier = 1.0, cleanSheetsMultiplier = 1.0;
                double savesMultiplier = 1.0, goalsConcededMultiplier = 1.0, ownGoalsMultiplier = 1.0;
                double penaltiesSavedMultiplier = 1.0, penaltiesMissedMultiplier = 1.0;
                double xgMultiplier = 1.0, xaMultiplier = 1.0, xgiMultiplier = 1.0, xgcMultiplier = 1.0;

                if (position == 1) // Goalkeeper
                {
                    cleanSheetsMultiplier = 2.0;
                    savesMultiplier = 2.0;
                    goalsConcededMultiplier = 2.0;
                    penaltiesSavedMultiplier = 2.0;
                    xgcMultiplier = 2.0;
                }
                else if (position == 2) // Defender
                {
                    cleanSheetsMultiplier = 2.0;
                    goalsConcededMultiplier = 2.0;
                    ownGoalsMultiplier = 2.0;
                    xgcMultiplier = 2.0;
                }
                else if (position == 3) // Midfielder
                {
                    goalsMultiplier = 2.0;
                    assistsMultiplier = 2.0;
                    xgMultiplier = 2.0;
                    xaMultiplier = 2.0;
                    xgiMultiplier = 2.0;
                }
                else if (position == 4) // Forward
                {
                    goalsMultiplier = 2.0;
                    penaltiesMissedMultiplier = 2.0;
                    xgMultiplier = 2.0;
                }

                totalMinutes += (double)item.Minutes * matchWeight;
                totalGoals += (double)item.GoalsScored * matchWeight * goalsMultiplier;
                totalAssists += (double)item.Assists * matchWeight * assistsMultiplier;
                totalGoalsConceded += (double)item.GoalsConceded * matchWeight * goalsConcededMultiplier;
                totalOwnGoals += (double)item.OwnGoals * matchWeight * ownGoalsMultiplier;
                totalPenaltiesSaved += (double)item.PenaltiesSaved * matchWeight * penaltiesSavedMultiplier;
                totalPenaltiesMissed += (double)item.PenaltiesMissed * matchWeight * penaltiesMissedMultiplier;
                totalYellowCards += (double)item.YellowCards * matchWeight;
                totalRedCards += (double)item.RedCards * matchWeight;
                totalSaves += (double)item.Saves * matchWeight * savesMultiplier;
                totalBonus += (double)item.Bonus * matchWeight;
                totalBonusPoints += (double)item.BonusPoints * matchWeight;
                totalInfluence += influence * matchWeight;
                totalCreativity += creativity * matchWeight;
                totalThreat += threat * matchWeight;
                totalICT += ictIndex * matchWeight;
                totalXG += expectedGoals * matchWeight * xgMultiplier;
                totalXA += expectedAssists * matchWeight * xaMultiplier;
                totalXGI += expectedGoalInvolvements * matchWeight * xgiMultiplier;
                totalXGC += expectedGoalsConceded * matchWeight * xgcMultiplier;
            }

            int totalMatches = history.Count;

            predictedMatchHistory.Minutes = (int)(totalMinutes / totalMatches);
            predictedMatchHistory.GoalsScored = totalGoals / totalMatches;
            predictedMatchHistory.Assists = totalAssists / totalMatches;
            predictedMatchHistory.GoalsConceded = totalGoalsConceded / totalMatches;
            predictedMatchHistory.OwnGoals = totalOwnGoals / totalMatches;
            predictedMatchHistory.PenaltiesSaved = totalPenaltiesSaved / totalMatches;
            predictedMatchHistory.PenaltiesMissed = totalPenaltiesMissed / totalMatches;
            predictedMatchHistory.YellowCards = totalYellowCards / totalMatches;
            predictedMatchHistory.RedCards = totalRedCards / totalMatches;
            predictedMatchHistory.Saves = totalSaves / totalMatches;
            predictedMatchHistory.Bonus = totalBonus / totalMatches;
            predictedMatchHistory.BonusPoints = totalBonusPoints / totalMatches;
            predictedMatchHistory.Influence = (totalInfluence / totalMatches).ToString("0.0");
            predictedMatchHistory.Creativity = (totalCreativity / totalMatches).ToString("0.0");
            predictedMatchHistory.Threat = (totalThreat / totalMatches).ToString("0.0");
            predictedMatchHistory.IctIndex = (totalICT / totalMatches).ToString("0.0");
            predictedMatchHistory.ExpectedGoals = (totalXG / totalMatches).ToString("0.0");
            predictedMatchHistory.ExpectedAssists = (totalXA / totalMatches).ToString("0.0");
            predictedMatchHistory.ExpectedGoalInvolvements = (totalXGI / totalMatches).ToString("0.0");
            predictedMatchHistory.ExpectedGoalsConceded = (totalXGC / totalMatches).ToString("0.0");

            return predictedMatchHistory;
        }


        public async Task<List<PlayerData>> GetAllPlayers()
        {
            BootStrapAPIResponse bootStrapAPIResponse = await GetBootStrapAPIResponse();

            return bootStrapAPIResponse.Elements.OrderBy(i => i.FirstName).ThenBy(i => i.LastName).ToList();
        }

        public async Task<List<PlayerData>> GetLatestPlayerData(List<PlayerData> playerData)
        {

            List<PlayerData> updatedPlayerData = new List<PlayerData>();

            foreach (PlayerData player in playerData)
            {
                PlayerData updated = await GetPlayerData(player.Id);
                updated.Index = player.Index;

                updatedPlayerData.Add(updated);
            }

            return updatedPlayerData;
        }

        public async Task<List<PlayerData>> PredictTeamScores(List<PlayerData> players)
        {
            foreach (PlayerData player in players)
            {
                player.PredictedScore = await GetPredictionForPlayer(player.Id);
                if (player.ChanceOfPlaying == 0)
                {
                    player.PredictedScore = 0;
                }
            }

            return players;
        }

        public async Task<List<PlayerData>> PredictAllPlayers()
        {
            List<PlayerData> allPlayers = await GetAllPlayers();
            List<History> allPlayerPrecitionData = new List<History>();

            foreach (PlayerData player in allPlayers)
            {
                FixtureData playerFixtureData = await GetPlayerFixtureData(player.Id);

                if (playerFixtureData != null)
                {
                    History averagedHistory = await GetPlayerAverages(playerFixtureData);

                    allPlayerPrecitionData.Add(averagedHistory);
                }
            }

            allPlayers = await _pythonRepository.PredictAllPlayerScores(allPlayerPrecitionData, allPlayers);

            foreach (PlayerData player in allPlayers)
            {
                if (player.ChanceOfPlaying == 0)
                {
                    player.PredictedScore = 0;
                }
            }

            return allPlayers;
        }

        public async Task<List<RecommendedTransfer>> RecommendTransfers(int numberOftransfers)
        {
            List<RecommendedTransfer> recommendedTransfers = new List<RecommendedTransfer>();
            List<PlayerData> currentTeam = await _teamStateService.RetrieveTeam();
            double remainingBudget = await _teamStateService.RetrieveBudget();
            List<PlayerData> playersWithPredictions = await PredictAllPlayers();

            foreach (PlayerData player in currentTeam)
            {
                player.PredictedScore = playersWithPredictions
                    .Where(p => p.Id == player.Id)
                    .Select(p => p.PredictedScore)
                    .FirstOrDefault();
            }

            var transferAlgorithm = new TransferAlgorithm(currentTeam, playersWithPredictions, remainingBudget, numberOftransfers);

            recommendedTransfers = transferAlgorithm.Run();

            return recommendedTransfers;
        }
    }
}
