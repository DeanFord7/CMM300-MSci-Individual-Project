using Blazored.LocalStorage;
using FPLAssistant.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPLAssistant.Services
{
    public class TeamStateService
    {
        private readonly ILocalStorageService _localStorage;
        public List<PlayerData> Players { get; private set; } = new List<PlayerData>();
        public TeamStateService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
            LoadTeam();
        }

        private async Task LoadTeam()
        {
            Players = await _localStorage.GetItemAsync<List<PlayerData>>("team");
        }

        private async Task SaveTeam()
        {
            await _localStorage.SetItemAsync("team", Players);
        }

        public async Task SetTeam(List<PlayerData> team)
        {
            Players = team;
            await SaveTeam();
        }

        public async Task<List<PlayerData>> RetrieveTeam()
        {
            await LoadTeam();
            return Players;
        }
    }
}
