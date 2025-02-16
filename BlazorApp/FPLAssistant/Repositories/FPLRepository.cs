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
        Task<PlayerData> GetPlayerData();
        Task<bool> GeneratePlayerDataCSV();
        Task<FixtureData> GetPlayerFixtureData(int playerId);
        Task<int?> GetPredictionForPlayer(int playerId);
        Task<History> GetPlayerAverages(FixtureData playerFixtureData);
        Task<List<PlayerData>> GetAllPlayers();
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

        public async Task<PlayerData> GetPlayerData()
        {
            PlayerData playerData = new PlayerData();
            Random random = new Random();

            int randomId = random.Next(1, 701);

            string generalInfoUrl = "https://fantasy.premierleague.com/api/bootstrap-static";

            BootStrapAPIResponse bootStrapAPIResponse = await GetBootStrapAPIResponse();

            playerData = bootStrapAPIResponse.Elements.Where(i => i.Id == randomId).FirstOrDefault();

            string playerUrl = $"https://fantasy.premierleague.com/api/element-summary/{randomId}/";

            try
            {
                var response = await _httpClient.GetStringAsync(playerUrl);
            }
            catch (HttpRequestException e) 
            { 
                Console.WriteLine(e.Message);
                return null;
            }

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

            return new History
            {
                PlayerId = history.First().PlayerId,  // Keep the same player ID
                Fixture = playerFixtureData.Fixtures.FirstOrDefault()?.Id ?? 0,
                OpponentTeam = playerFixtureData.Fixtures.FirstOrDefault()?.TeamAway ?? 0,
                WasHome = playerFixtureData.Fixtures.FirstOrDefault()?.IsHome ?? true,

                // Compute averages
                Minutes = (int)history.Average(h => h.Minutes),
                GoalsScored = (double)history.Average(h => h.GoalsScored),
                Assists = (double)history.Average(h => h.Assists),
                CleanSheets = (double)history.Average(h => h.CleanSheets),
                GoalsConceded = (double)history.Average(h => h.GoalsConceded),
                OwnGoals = (double)history.Average(h => h.OwnGoals),
                PenaltiesSaved = (double)history.Average(h => h.PenaltiesSaved),
                PenaltiesMissed = (double)history.Average(h => h.PenaltiesMissed),
                YellowCards = (double)history.Average(h => h.YellowCards),
                RedCards = (double)history.Average(h => h.RedCards),
                Saves = (double)history.Average(h => h.Saves),
                Bonus = (double)history.Average(h => h.Bonus),
                BonusPoints = (double)history.Average(h => h.BonusPoints),

                // Advanced stats
                Influence = history.Average(h => double.TryParse(h.Influence, out var x) ? x : 0).ToString("0.00"),
                Creativity = history.Average(h => double.TryParse(h.Creativity, out var x) ? x : 0).ToString("0.00"),
                Threat = history.Average(h => double.TryParse(h.Threat, out var x) ? x : 0).ToString("0.00"),
                IctIndex = history.Average(h => double.TryParse(h.IctIndex, out var x) ? x : 0).ToString("0.00"),
                ExpectedGoals = history.Average(h => double.TryParse(h.ExpectedGoals, out var x) ? x : 0).ToString("0.00"),
                ExpectedAssists = history.Average(h => double.TryParse(h.ExpectedAssists, out var x) ? x : 0).ToString("0.00"),
                ExpectedGoalInvolvements = history.Average(h => double.TryParse(h.ExpectedGoalInvolvements, out var x) ? x : 0).ToString("0.00"),
                ExpectedGoalsConceded = history.Average(h => double.TryParse(h.ExpectedGoalsConceded, out var x) ? x : 0).ToString("0.00"),
            };
        }

        public async Task<List<PlayerData>> GetAllPlayers()
        {
            BootStrapAPIResponse bootStrapAPIResponse = await GetBootStrapAPIResponse();

            return bootStrapAPIResponse.Elements;
        }
    }
}
