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
        Task<List<History>> GetPlayerFixtureData(int playerId);
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
                    List<History> playerFixtureData = await GetPlayerFixtureData(player.Id);

                    fullFixtureHistory.AddRange(playerFixtureData);
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

        public async Task<List<History>> GetPlayerFixtureData(int playerId)
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

                return playerFixtureData.History;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}
