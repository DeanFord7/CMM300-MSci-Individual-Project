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
    }

    public class FPLRepository : IFPLRepository
    {
        private readonly HttpClient _httpClient;
        private readonly PythonRepository _pythonRepository;
        public FPLRepository(HttpClient httpClient, PythonRepository pythonRepository) 
        { 
            _httpClient = httpClient;
            _pythonRepository = pythonRepository;
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

            if (history == null || history.Count == 0)
            {
                // No history available, return default values
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

            double goalsScored = (double)history.Average(h => h.GoalsScored);

            History predictedMatchHistory = new History
            {
                PlayerId = history.First().PlayerId,  // Keep the same player ID
                Fixture = playerFixtureData.Fixtures.FirstOrDefault()?.Id ?? 0,
                OpponentTeam = playerFixtureData.Fixtures.FirstOrDefault()?.TeamAway ?? 0,
                WasHome = playerFixtureData.Fixtures.FirstOrDefault()?.IsHome ?? true,
            };

            double totalMinutes = 0;
            double totalGoals = 0;
            double totalAssists = 0;
            double totalGoalsConceded = 0;
            double totalOwnGoals = 0;
            double totalPenaltiesSaved = 0;
            double totalPenaltiesMissed = 0;
            double totalYellowCards = 0;
            double totalRedCards = 0;
            double totalSaves = 0;
            double totalBonus = 0;
            double totalBonusPoints = 0;
            double totalInfluence = 0;
            double totalCreativity = 0;
            double totalThreat = 0;
            double totalICT = 0;
            double totalXG = 0;
            double totalXA = 0;
            double totalXGI = 0;
            double totalXGC = 0;

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

                if (index >= history.Count - 5)
                {
                    totalMinutes += (double)item.Minutes * 2;
                    totalGoals += (double)item.GoalsScored * 2;
                    totalAssists += (double)item.Assists * 2;
                    totalGoalsConceded += (double)item.GoalsConceded * 2;
                    totalOwnGoals += (double)item.OwnGoals * 2;
                    totalPenaltiesSaved += (double)item.PenaltiesSaved * 2;
                    totalPenaltiesMissed += (double)item.PenaltiesMissed * 2;
                    totalYellowCards += (double)item.YellowCards * 2;
                    totalRedCards += (double)item.RedCards * 2;
                    totalSaves += (double)item.Saves * 2;
                    totalBonus += (double)item.Bonus * 2;
                    totalBonusPoints += (double)item.BonusPoints * 2;
                    totalInfluence += (double)influence * 2;
                    totalCreativity += (double)creativity * 2;
                    totalThreat += (double)threat * 2;
                    totalICT += (double)ictIndex * 2;
                    totalXG += (double)expectedGoals * 2;
                    totalXA += (double)expectedAssists * 2;
                    totalXGI += (double)expectedGoalInvolvements * 2;
                    totalXGC += (double)expectedGoalsConceded * 2;
                }
                else
                {
                    totalMinutes += (double)item.Minutes;
                    totalGoals += (double)item.GoalsScored;
                    totalAssists += (double)item.Assists;
                    totalGoalsConceded += (double)item.GoalsConceded;
                    totalOwnGoals += (double)item.OwnGoals;
                    totalPenaltiesSaved += (double)item.PenaltiesSaved;
                    totalPenaltiesMissed += (double)item.PenaltiesMissed;
                    totalYellowCards += (double)item.YellowCards;
                    totalRedCards += (double)item.RedCards;
                    totalSaves += (double)item.Saves;
                    totalBonus += (double)item.Bonus;
                    totalBonusPoints += (double)item.BonusPoints;
                    totalInfluence += (double)influence;
                    totalCreativity += (double)creativity;
                    totalThreat += (double)threat;
                    totalICT += (double)ictIndex;
                    totalXG += (double)expectedGoals;
                    totalXA += (double)expectedAssists;
                    totalXGI += (double)expectedGoalInvolvements;
                    totalXGC += (double)expectedGoalsConceded;
                }
            }

            int totalMatches = history.Count;
            if (totalMinutes / totalMinutes > 90)
            {
                predictedMatchHistory.Minutes = 90;
            }
            else
            {
                predictedMatchHistory.Minutes = (int)(totalMinutes / totalMatches);
            }
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
            }

            return players;
        }
    }
}
