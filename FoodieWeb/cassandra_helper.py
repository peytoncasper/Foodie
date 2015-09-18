from cassandra.cluster import Cluster
from cassandra.query import ordered_dict_factory

session = None

def create_cassandra_session(ip_address, keyspace):
    global session
    cluster = Cluster(ip_address)
    session = cluster.connect(keyspace)
    session.row_factory = ordered_dict_factory