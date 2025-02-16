using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FPLAssistant.Models
{
    public class FixtureData
    {
        [JsonPropertyName("fixtures")]
        public List<Fixture> Fixtures { get; set; }
        [JsonPropertyName("history")]
        public List<History> History { get; set; }
        [JsonPropertyName("history_past")]
        public List<PastHistory> PastHistory { get; set; }
        
    }

    public class Fixture
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("team_h")]
        public int TeamHome { get; set; }

        [JsonPropertyName("team_h_score")]
        public int? TeamHomeScore { get; set; }

        [JsonPropertyName("team_a")]
        public int TeamAway { get; set; }

        [JsonPropertyName("team_a_score")]
        public int? TeamAwayScore { get; set; }

        [JsonPropertyName("event")]
        public int Event { get; set; }

        [JsonPropertyName("finished")]
        public bool Finished { get; set; }

        [JsonPropertyName("minutes")]
        public int Minutes { get; set; }

        [JsonPropertyName("provisional_start_time")]
        public bool ProvisionalStartTime { get; set; }

        [JsonPropertyName("kickoff_time")]
        public string KickoffTime { get; set; }

        [JsonPropertyName("event_name")]
        public string EventName { get; set; }

        [JsonPropertyName("is_home")]
        public bool IsHome { get; set; }

        [JsonPropertyName("difficulty")]
        public int Difficulty { get; set; }
    }


    public class History
    {
        [JsonPropertyName("element")]
        public int PlayerId { get; set; }

        [JsonPropertyName("fixture")]
        public int Fixture { get; set; }

        [JsonPropertyName("opponent_team")]
        public int OpponentTeam { get; set; }

        [JsonPropertyName("total_points")]
        public int? TotalPoints { get; set; }

        [JsonPropertyName("was_home")]
        public bool WasHome { get; set; }

        [JsonPropertyName("team_h_score")]
        public int? TeamHomeScore { get; set; }

        [JsonPropertyName("team_a_score")]
        public int? TeamAwayScore { get; set; }

        [JsonPropertyName("minutes")]
        public int? Minutes { get; set; }

        [JsonPropertyName("goals_scored")]
        public int? GoalsScored { get; set; }

        [JsonPropertyName("assists")]
        public int? Assists { get; set; }

        [JsonPropertyName("clean_sheets")]
        public int? CleanSheets { get; set; }

        [JsonPropertyName("goals_conceded")]
        public int? GoalsConceded { get; set; }

        [JsonPropertyName("own_goals")]
        public int? OwnGoals { get; set; }

        [JsonPropertyName("penalties_saved")]
        public int? PenaltiesSaved { get; set; }

        [JsonPropertyName("penalties_missed")]
        public int? PenaltiesMissed { get; set; }

        [JsonPropertyName("yellow_cards")]
        public int? YellowCards { get; set; }

        [JsonPropertyName("red_cards")]
        public int? RedCards { get; set; }

        [JsonPropertyName("saves")]
        public int? Saves { get; set; }

        [JsonPropertyName("bonus")]
        public int? Bonus { get; set; }

        [JsonPropertyName("bps")]
        public int? BonusPoints { get; set; }

        [JsonPropertyName("influence")]
        public string Influence { get; set; }

        [JsonPropertyName("creativity")]
        public string Creativity { get; set; }

        [JsonPropertyName("threat")]
        public string Threat { get; set; }

        [JsonPropertyName("ict_index")]
        public string IctIndex { get; set; }

        [JsonPropertyName("starts")]
        public int? Starts { get; set; }

        [JsonPropertyName("expected_goals")]
        public string ExpectedGoals { get; set; }

        [JsonPropertyName("expected_assists")]
        public string ExpectedAssists { get; set; }

        [JsonPropertyName("expected_goal_involvements")]
        public string ExpectedGoalInvolvements { get; set; }

        [JsonPropertyName("expected_goals_conceded")]
        public string ExpectedGoalsConceded { get; set; }
        [JsonPropertyName("position")]
        public int Position { get; set; }
    }

    public class PastHistory
    {

    }
}
