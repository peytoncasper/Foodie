from flask import Flask
from cassandra_helper import create_cassandra_session
from routes.web import web


app = Flask(__name__)
app.config.from_pyfile('application.cfg')

app.register_blueprint(web)

def start():



    create_cassandra_session(app.config["DSE_CLUSTER"].split(','), app.config["DSE_KEYSPACE"])
    app.run(host='0.0.0.0',
            port=5000,
            use_reloader=True,
            threaded=True)
