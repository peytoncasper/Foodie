package weight.sparkjobs

import java.sql.DriverManager
import com.datastax.spark.connector.SomeColumns
import com.datastax.spark.connector.streaming._
import org.apache.spark.SparkConf
import org.apache.spark.streaming.eventhubs.EventHubsUtils
import org.apache.spark.streaming.{Seconds, StreamingContext}
import org.json4s._
import org.json4s.native.JsonMethods._
import com.microsoft.sqlserver.jdbc._
case class weight_reading(Sensor_Id: String, Weight: String, Reading_Time: String)
object WeightSensorSparkStreamingJob {


	implicit val formats = DefaultFormats
	def main(args: Array[String]) {
		Class.forName("com.microsoft.sqlserver.jdbc.SQLServerDriver");
		if (args.length < 7) {
			System.err.println("Usage: EventCount <policyname> <policykey>"
				+ "<namespace> <name> <partitionCount> <cassandra IP> <sqlConnectionString>")
			System.err.println(args.length)
			System.exit(1)
		}
		val Array(policy, key, namespace, name,partitionCount, cassandraIp, sqlConnectionString) = args
		val ehParams = Map[String, String](
			"eventhubs.policyname" -> policy,
			"eventhubs.policykey" -> key,
			"eventhubs.namespace" -> namespace,
			"eventhubs.name" -> name,
			"eventhubs.partition.count" -> partitionCount,
			"eventhubs.consumergroup" -> "$Default",
			"eventhubs.checkpoint.dir" -> "checkpoint",
			"eventhubs.checkpoint.interval" -> "10"
		)


		val ptCount = partitionCount.toInt


		val sparkConf = new SparkConf().setAppName("EventCount").set("spark.cores.max",(ptCount*2).toString).setMaster( "local[9]").set("spark.cassandra.connection.host", cassandraIp)
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

						val insertWeightReading = sqlString + "('" + weight_readings.Sensor_Id + "'," + weight_readings.Weight + ",'" + weight_readings.Reading_Time + "')"

						statement.executeUpdate(insertWeightReading)
					}
				})
			})

		})

		ssc.start()
		ssc.awaitTermination()
1
	}
}
