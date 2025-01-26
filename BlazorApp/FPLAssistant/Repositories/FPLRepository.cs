using FPLAssistant.Models;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace FPLAssistant.Repositories
{
    public interface IFPLRepository
    {
        Task<PlayerData> GetPlayerData();
    }

    public class FPLRepository : IFPLRepository
    {
        private readonly HttpClient _httpClient;
        public FPLRepository(HttpClient httpClient) 
        { 
            _httpClient = httpClient;
        }

        public async Task<PlayerData> GetPlayerData()
        {
            PlayerData playerData = new PlayerData();
            Random random = new Random();

            int randomId = random.Next(1, 701);

            string generalInfoUrl = "https://fantasy.premierleague.com/api/bootstrap-static";

            try
            {
                var generalResponse = await _httpClient.GetAsync(generalInfoUrl);

                var responseContent = await generalResponse.Content.ReadAsStringAsync();

                BootStrapAPIResponse bootStrapAPIResponse = JsonSerializer.Deserialize<BootStrapAPIResponse>(responseContent);

                playerData = bootStrapAPIResponse.Elements.Where(i => i.Id == randomId).FirstOrDefault();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

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
    }
}
