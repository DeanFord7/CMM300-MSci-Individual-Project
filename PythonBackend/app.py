from flask import Flask, jsonify

app = Flask(__name__)

@app.route('/')

def basicResponse():
    return 'This is a message from the python backend'

if __name__ == '__main__':
    app.run(debug=True)