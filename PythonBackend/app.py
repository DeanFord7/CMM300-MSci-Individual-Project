from flask import Flask, jsonify, request
import csv
import numpy as np
import pandas as pd
import joblib 
import os 
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import root_mean_squared_error

app = Flask(__name__)

model = None
MODEL_PATH = "trained_model.pkl" 


if os.path.exists(MODEL_PATH):
    model = joblib.load(MODEL_PATH)
    print("✅ Model loaded successfully from file.")
else:
    print("⚠️ No pre-trained model found. Train the model first.")

import pandas as pd

def apply_position_weightings(df: pd.DataFrame) -> pd.DataFrame:
    # Features to double for each position
    features_to_weight = {
        1: ["clean_sheets", "saves", "goals_conceded", "penalties_saved", "expected_goals_conceded"],  # Goalkeepers
        2: ["goals_conceded", "own_goals", "clean_sheets", "expected_goals_conceded"],                 # Defenders
        3: ["goals_scored", "assists", "expected_goals", "expected_assists", "expected_goal_involvements"],  # Midfielders
        4: ["goals_scored", "penalties_missed", "expected_goals"]                                      # Attackers
    }

    # Make a copy to avoid modifying original
    df_weighted = df.copy()

    # Apply x2 weightings for relevant features
    for position, feature_list in features_to_weight.items():
        position_mask = df_weighted['position'] == position

        for feature in feature_list:
            if feature in df_weighted.columns:
                df_weighted.loc[position_mask, feature] *= 2

    return df_weighted

@app.route('/create_csv', methods=['POST'])

def create_csv():
    data = request.get_json()

    csv_file_path = './FixtureData.csv'

    with open(csv_file_path, mode='w', newline='') as file:
        writer = csv.writer(file)
        
        # Write the header
        writer.writerow(["element", "fixture", "opponent_team", "total_points", "was_home", "team_h_score", "team_a_score", 
          "minutes", "goals_scored", "assists", "clean_sheets", "goals_conceded", "own_goals", 
          "penalties_saved", "penalties_missed", "yellow_cards", "red_cards", "saves", "bonus", "bps", 
          "influence", "creativity", "threat", "ict_index", "starts", "expected_goals", "expected_assists", 
          "expected_goal_involvements", "expected_goals_conceded", "position"])

        # Write each history entry
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

    return jsonify({"message": "CSV created successfully", "filePath": csv_file_path}), 200

@app.route("/train_model", methods=["GET"])
def train_model():
    global model
    df = pd.read_csv("FixtureData.csv")

    # Apply the weightings based on a players position
    df_weighted = apply_position_weightings(df)

    x = df_weighted.drop(columns=["total_points"])
    y = df_weighted["total_points"]

    # Split into train test split
    x_train, x_test, y_train, y_test = train_test_split(x, y, test_size=0.2, random_state=42)

    # Initailise and fit the random forest model
    model = RandomForestRegressor(n_estimators=100, random_state=42)
    model.fit(x_train, y_train)

    # Make predictions on the testing data
    y_pred = model.predict(x_test)

    # Calculate the root mean squared error of the predictions
    rmse = root_mean_squared_error(y_test, y_pred)
    print(f"RMSE: {rmse:.4f}")

    # Save the model
    joblib.dump(model, MODEL_PATH)
    print(f"✅ Model saved to {MODEL_PATH}")

    return jsonify({"status": "Model trained"})

@app.route('/predict_player_score', methods=['POST'])
def predict_player_score():
    global model
    # Load the existing model if it exists
    if model is None:
        if os.path.exists(MODEL_PATH):
            model = joblib.load(MODEL_PATH)
            print("✅ Model loaded from file.")
        else:
            print("❌ No model available.")
            return jsonify({"error": "Model not trained yet"}), 400
    
    input_data = request.get_json()
    df_input = pd.DataFrame([input_data])

    df_input = df_input.drop(columns=["total_points"])

    # Make a prediction on the input data
    prediction = model.predict(df_input)

    return jsonify(prediction.tolist())

@app.route('/predict_all_player_scores', methods=['POST'])
def predict_all_players():
    global model
    print("Predicting all players")
    # Get the model if it exists
    if model is None:
        if os.path.exists(MODEL_PATH):
            model = joblib.load(MODEL_PATH)
            print("✅ Model loaded from file.")
        else:
            print("❌ No model available.")
            return jsonify({"error": "Model not trained yet"}), 400
    
    input_data = request.get_json()
    print("Received data:", input_data)

    df_input = pd.DataFrame(input_data)

    if "total_points" in df_input.columns:
        df_input = df_input.drop(columns=["total_points"])

    # Get predictions for all players
    predictions = model.predict(df_input)

    # Format the data to return to the front end
    response_data = [
        {"id": player["element"], "predicted_score": float(score)}
        for player, score in zip(input_data, predictions)
    ]

    return jsonify(response_data)

if __name__ == '__main__':
    app.run(debug=True)
