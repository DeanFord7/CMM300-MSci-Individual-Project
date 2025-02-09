from flask import Flask, jsonify, request
import csv

app = Flask(__name__)

@app.route('/')

def basicResponse():
    return 'This is a message from the python backend'

@app.route('/create_csv', methods=['POST'])

def create_csv():
    data = request.get_json()

    csv_file_path = './FixtureData.csv'

    with open(csv_file_path, mode='w', newline='') as file:
        writer = csv.writer(file)
        
        # Write the header (you should adjust based on your data fields)
        writer.writerow(["PlayerId", "Fixture", "OpponentTeam", "TotalPoints", "WasHome", "TeamHomeScore", "TeamAwayScore", 
                            "Round", "Minutes", "GoalsScored", "Assists", "CleanSheets", "GoalsConceded", "OwnGoals", 
                            "PenaltiesSaved", "PenaltiesMissed", "YellowCards", "RedCards", "Saves", "Bonus", "BonusPoints", 
                            "Influence", "Creativity", "Threat", "IctIndex", "Starts", "ExpectedGoals", "ExpectedAssists", 
                            "ExpectedGoalInvolvements", "ExpectedGoalsConceded"])

        # Write each history entry (ensure the keys match the property names in the data)
        for history in data:
            print(history)
            writer.writerow([history['element'], history['fixture'], history['opponent_team'], history['total_points'], 
                                history['was_home'], history['team_h_score'], history['team_a_score'], history['round'], 
                                history['minutes'], history['goals_scored'], history['assists'], history['clean_sheets'], 
                                history['goals_conceded'], history['own_goals'], history['penalties_saved'], history['penalties_missed'], 
                                history['yellow_cards'], history['red_cards'], history['saves'], history['bonus'], 
                                history['bps'], history['influence'], history['creativity'], history['threat'], 
                                history['ict_index'], history['starts'], history['expected_goals'], history['expected_assists'], 
                                history['expected_goal_involvements'], history['expected_goals_conceded']])

    # Return a response back to Blazor indicating success
    return jsonify({"message": "CSV created successfully", "filePath": csv_file_path}), 200

if __name__ == '__main__':
    app.run(debug=True)
