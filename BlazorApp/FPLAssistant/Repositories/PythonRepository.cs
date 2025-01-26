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
    }

    public class PythonRepository : IPythonRepository
    {
        private readonly HttpClient _httpClient;
        public PythonRepository(HttpClient httpClient) 
        { 
            _httpClient = httpClient;
        }

        public async Task<string> SendMessage()
        {
            string flaskUrl = "http://127.0.0.1:5000/";

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
    }
}
