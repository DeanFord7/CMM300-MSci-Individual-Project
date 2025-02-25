using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FPLAssistant.Models
{
    public class PlayerData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }
        [JsonPropertyName("second_name")]
        public string LastName { get; set; }
        [JsonPropertyName("web_name")]
        public string WebName { get; set; }
        [JsonPropertyName("total_points")]
        public int TotalPoints { get; set; }
        [JsonPropertyName("goals_scored")]
        public int Goals { get; set; }
        [JsonPropertyName("assists")]
        public int Assists { get; set; }
        [JsonPropertyName("now_cost")]
        public int Cost {  get; set; }
        [JsonPropertyName("element_type")]
        public int Position { get; set; }
        public int Index { get; set; }
    }
}
