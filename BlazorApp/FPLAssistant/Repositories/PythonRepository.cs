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
    public interface IPythonRepository
    {
        Task<string> SendMessage();
        Task<bool> GenerateCsv(List<History> fixtureHistory);
        Task<bool> TrainModel();
    }

    public class PythonRepository : IPythonRepository
    {
        private readonly HttpClient _httpClient;
        private string flaskUrl = "http://127.0.0.1:5000/";
        public PythonRepository(HttpClient httpClient) 
        { 
            _httpClient = httpClient;
        }

        public async Task<string> SendMessage()
        {
            try
            {
                var response = await _httpClient.GetAsync(flaskUrl);

                var responseContent = await response.Content.ReadAsStringAsync();

                return responseContent;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<bool> GenerateCsv(List<History> fixtureHistory)
        {
            var url = flaskUrl + "create_csv";

            var jsonContent = new StringContent(JsonSerializer.Serialize(fixtureHistory), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(result);

                return true;
            }
            else
            {
                Console.WriteLine("Error: " + response.StatusCode);
                return false;
            }
        }

        public async Task<bool> TrainModel()
        {
            var url = flaskUrl + "train_model";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode) 
            { 
                var result = response.Content.ReadAsStringAsync();
                Console.WriteLine(result);

                return true;
            }
            else
            {
                Console.WriteLine("Error: " + response.StatusCode);
                return false;
            }
        }

        public async Task<int?> PredictPlayerScore(History upcomingFixtureData)
        {
            var url = flaskUrl + "predict_player_score";

            //var jsonContent = new StringContent(JsonSerializer.Serialize(upcomingFixtureData), Encoding.UTF8, "application/json");

            var serializedData = JsonSerializer.Serialize(upcomingFixtureData);
            Console.WriteLine(serializedData);  // Check the structure of the JSON you're sending
            var jsonContent = new StringContent(serializedData, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(result);

                var predictionList = JsonSerializer.Deserialize<List<float>>(result);

                if (predictionList != null && predictionList.Count > 0)
                {
                    int predictedScore = (int)Math.Round(predictionList[0]);

                    return predictedScore;
                }

                return null;
            }
            else
            {
                Console.WriteLine("Error: " + response.StatusCode);
                return null;
            }
        }
    }

    public class PredictionResponse
    {
        public List<float> Prediction { get; set; }
    }
}
