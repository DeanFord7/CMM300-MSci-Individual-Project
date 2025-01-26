using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FPLAssistant.Models
{
    public class BootStrapAPIResponse
    {
        [JsonPropertyName("elements")]
        public List<PlayerData> Elements { get; set; } = new List<PlayerData>();
    }
}
