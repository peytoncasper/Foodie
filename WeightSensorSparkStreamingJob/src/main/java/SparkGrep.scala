import java.sql.{DriverManager}
import com.datastax.spark.connector.SomeColumns
import org.apache.spark.{SparkConf}
import org.apache.spark.streaming.eventhubs.EventHubsUtils
import org.apache.spark.streaming.{Seconds, StreamingContext}
import com.datastax.spark.connector.streaming._
import org.json4s._
import org.json4s.native.JsonMethods._

case class weight_reading(sensor_id: String, weight: String, reading_time: String)
object SparkGrep {


	implicit val formats = DefaultFormats
	def main(args: Array[String]) {

		if (args.length < 9) {
			System.err.println("Usage: EventCount <checkpointDirectory> <policyname> <policykey>"
				+ "<namespace> <name> <partitionCount> <consumerGroup> <outputDirectory> <sqlConnectionString>")
			System.exit(1)
		}
		val Array(checkpointDir, policy, key, namespace, name,
		partitionCount, consumerGroup, outputDir, sqlConnectionString) = args
		val ehParams = Map[String, String](
			"eventhubs.policyname" -> policy,
			"eventhubs.policykey" -> key,
			"eventhubs.namespace" -> namespace,
			"eventhubs.name" -> name,
			"eventhubs.partition.count" -> partitionCount,
			"eventhubs.consumergroup" -> consumerGroup,
			"eventhubs.checkpoint.dir" -> checkpointDir,
			"eventhubs.checkpoint.interval" -> "10"
		)


		val ptCount = partitionCount.toInt


		val sparkConf = new SparkConf().setAppName("EventCount").set("spark.cores.max",(ptCount*2).toString).setMaster( "local[9]").set("spark.cassandra.connection.host", "127.0.0.1")
		val ssc =  new StreamingContext(sparkConf, Seconds(5))

		val stream = EventHubsUtils.createUnionStream(ssc, ehParams)

		val cqlData = stream.map(record =>parse(new String(record)).extract[weight_reading]).saveToCassandra("foodie", "weight_readings", SomeColumns("sensor_id", "weight", "reading_time"))
		val sqlData = stream.foreachRDD(rdd => {
			rdd.foreachPartition(partitionOfRecords =>{
				val connectionString =sqlConnectionString;
				val connection = DriverManager.getConnection(connectionString)
				val sqlString = "INSERT INTO weight_readings (sensor_id, weight, reading_time) VALUES "
				val statement = connection.createStatement()
				partitionOfRecords.foreach(record => {
					if(record != null) {
						val weight_readings = parse(new String(record)).extract[weight_reading]

						val insertWeightReading = sqlString + "('" + weight_readings.sensor_id + "'," + weight_readings.weight + ",'" + weight_readings.reading_time + "')"

						statement.executeUpdate(insertWeightReading)
					}
				})
			})

		})

		ssc.start()
		ssc.awaitTermination()

	}
}
