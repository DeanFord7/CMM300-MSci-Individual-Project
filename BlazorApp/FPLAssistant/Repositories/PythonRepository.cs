using FPLAssistant.Models;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FPLAssistant.Repositories
{
    public interface IPythonRepository
    {
        Task<bool> GenerateCsv(List<History> fixtureHistory);
        Task<bool> TrainModel();
        Task<int?> PredictPlayerScore(History upcomingFixtureData);
        Task<List<PlayerData>> PredictAllPlayerScores(List<History> playerData, List<PlayerData> bootstrapResponse);
    }

    public class PythonRepository : IPythonRepository
    {
        private readonly HttpClient _httpClient;
        private string flaskUrl = "http://127.0.0.1:5000/";
        public PythonRepository(HttpClient httpClient) 
        { 
            _httpClient = httpClient;
        }

        /// <summary>
        /// Generate a CSV on the latest data
        /// </summary>
        /// <param name="fixtureHistory"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Re-train the machine learning model
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Get predictions for an individual player
        /// </summary>
        /// <param name="upcomingFixtureData"></param>
        /// <returns></returns>
        public async Task<int?> PredictPlayerScore(History upcomingFixtureData)
        {
            var url = flaskUrl + "predict_player_score";

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

        /// <summary>
        /// Get predictions for all players
        /// </summary>
        /// <param name="playerData"></param>
        /// <param name="bootstrapResponse"></param>
        /// <returns></returns>
        public async Task<List<PlayerData>> PredictAllPlayerScores(List<History> playerData, List<PlayerData> bootstrapResponse)
        {
            var url = flaskUrl + "predict_all_player_scores";

            var serializedData = JsonSerializer.Serialize(playerData);
            Console.WriteLine("Sending: " + serializedData);
            var jsonContent = new StringContent(serializedData, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response from Flask: " + result);

                var predictions = JsonSerializer.Deserialize<List<PlayerPredictionResponse>>(result);

                if (predictions != null)
                {
                    List<PlayerData> allPlayers = bootstrapResponse;
                    
                    foreach (PlayerData player in allPlayers)
                    {
                        player.PredictedScore = (int)Math.Round(predictions
                            .Where(i => i.Id == player.Id)
                            .Select(i => i.PredictedScore)
                            .FirstOrDefault());
                    }

                    return allPlayers;
                }
            }
            else
            {
                Console.WriteLine("Error: " + response.StatusCode);
            }

            return new List<PlayerData>();
        }

    }

    public class PredictionResponse
    {
        public List<float> Prediction { get; set; }
    }

    public class PlayerPredictionResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("predicted_score")]
        public float PredictedScore { get; set; }
    }
}
