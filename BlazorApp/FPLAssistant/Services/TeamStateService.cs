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
        public string TeamName;
        public double RemainingBudget;
        public TeamStateService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
            LoadTeam();
            LoadTeamName();
            LoadBudget();
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

        private async Task LoadTeamName()
        {
            TeamName = await _localStorage.GetItemAsync<string>("team_name");
        }

        private async Task SaveTeamName()
        {
            await _localStorage.SetItemAsync("team_name", TeamName);
        }

        public async Task SetTeamName(string teamName)
        {
            TeamName = teamName;
            await SaveTeamName();
        }

        public async Task<string> RetrieveTeamName()
        {
            await LoadTeamName();
            return TeamName;
        }

        private async Task LoadBudget()
        {
            RemainingBudget = await _localStorage.GetItemAsync<double>("budget");
        }

        private async Task SaveBudget()
        {
            await _localStorage.SetItemAsync("budget", RemainingBudget);
        }

        public async Task SetBudget(double budget)
        {
            RemainingBudget = budget;
            await SaveBudget();
        }

        public async Task<double> RetrieveBudget()
        {
            await LoadBudget();
            return RemainingBudget;
        }
    }
}
