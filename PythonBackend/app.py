from flask import Flask, jsonify, request
import csv
import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import root_mean_squared_error

app = Flask(__name__)

model = None

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
        writer.writerow(["element", "fixture", "opponent_team", "total_points", "was_home", "team_h_score", "team_a_score", 
          "minutes", "goals_scored", "assists", "clean_sheets", "goals_conceded", "own_goals", 
          "penalties_saved", "penalties_missed", "yellow_cards", "red_cards", "saves", "bonus", "bps", 
          "influence", "creativity", "threat", "ict_index", "starts", "expected_goals", "expected_assists", 
          "expected_goal_involvements", "expected_goals_conceded", "position"])

        # Write each history entry (ensure the keys match the property names in the data)
        for history in data:
            print(history)
            writer.writerow([history['element'], history['fixture'], history['opponent_team'], history['total_points'], 
                                history['was_home'], history['team_h_score'], history['team_a_score'], 
                                history['minutes'], history['goals_scored'], history['assists'], history['clean_sheets'], 
                                history['goals_conceded'], history['own_goals'], history['penalties_saved'], history['penalties_missed'], 
                                history['yellow_cards'], history['red_cards'], history['saves'], history['bonus'], 
                                history['bps'], history['influence'], history['creativity'], history['threat'], 
                                history['ict_index'], history['starts'], history['expected_goals'], history['expected_assists'], 
                                history['expected_goal_involvements'], history['expected_goals_conceded'], history['position']])

    # Return a response back to Blazor indicating success
    return jsonify({"message": "CSV created successfully", "filePath": csv_file_path}), 200

@app.route("/train_model", methods=["GET"])
def train_model():
    global model
    df = pd.read_csv("FixtureData.csv")

    x = df.drop(columns=["total_points"])
    y = df["total_points"]

    x_train, x_test, y_train, y_test = train_test_split(x, y, test_size=0.2, random_state=42)

    model = RandomForestRegressor(n_estimators=100, random_state=42)
    model.fit(x_train, y_train)

    y_pred = model.predict(x_test)

    rmse = root_mean_squared_error(y_test, y_pred)
    print(f"RMSE: {rmse:.4f}")

    return jsonify({"status": "Model trained"})

@app.route('/predict_player_score', methods=['POST'])
def predict_player_score():
    global model
    print("Endpoint reached")
    if model is None:
        print("No Model")
        return jsonify({"error": "Model not trained yet"}), 400
    
    input_data = request.get_json()
    print(input_data)
    df_input = pd.DataFrame([input_data])

    df_input = df_input.drop(columns=["total_points"])

    prediction = model.predict(df_input)

    return jsonify(prediction.tolist())

if __name__ == '__main__':
    app.run(debug=True)
