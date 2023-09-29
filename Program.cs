using Confluent.Kafka;
using MyTelemetryClasses;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;

namespace KafkaProducerToFabric
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            //EventStream item in Fabric: TrailorEventhub
            const string connectionstring_primary_key = "Endpoint=sb://esehwestea2jlgmz920oo5ck.servicebus.windows.net/;SharedAccessKeyName=key_e7ebadba-3da0-7691-ac07-97b89c21e5a9;SharedAccessKey=5jQm1GCuSX7eKeUb5MgDRdJHb36DQWP4V+AEhFBsmms=;EntityPath=es_34e34399-9dd4-492b-a9f6-dd35f6e0f042";

            string[] parts = connectionstring_primary_key.Split(';');
            string eventHubNameSpace = parts[0].Split('=')[1].Split('.')[0].Replace("sb://",""); //known as name of the endpoint from the connection string (esehwestea2jlgmz920oo5ck)
            string topic = parts[3].Split('=')[1]; //known as EntityPath from the connection string (es_34e34399-9dd4-492b-a9f6-dd35f6e0f042)

            Console.WriteLine("EventHub Namespace: " + eventHubNameSpace);
            Console.WriteLine("EventHub Topic: " + topic);
            //demo settings
            int numberOfEventsSent = 10;
            bool dumpJsonOutput = true;
            bool useJsonAsEventFormat = true;

            var config = new ProducerConfig
            {
                /*
                   //Kafka Config Template
                   BootstrapServers = "localhost:9092",
                   SaslMechanism = SaslMechanism.Plain,
                   SecurityProtocol = SecurityProtocol.SaslSsl,
                   SaslUsername = "xxxxxxx",
                   SaslPassword = "xxxx+"

                    //EventHub Config Template
                    bootstrap.servers=NAMESPACENAME.servicebus.windows.net:9093
                    security.protocol=SASL_SSL
                    sasl.mechanism=PLAIN
                    sasl.jaas.config=org.apache.kafka.common.security.plain.PlainLoginModule required username="$ConnectionString" password="{YOUR.EVENTHUBS.CONNECTION.STRING}";
                */

                BootstrapServers = eventHubNameSpace + ".servicebus.windows.net:9093",
                SaslMechanism = SaslMechanism.Plain,
                ApiVersionRequest = true,
                BrokerVersionFallback = "2.0.0",
                CompressionType = CompressionType.Gzip,

                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslUsername = "$ConnectionString",
                SaslPassword = connectionstring_primary_key,

                //Optional Kafka settings below - to maintain message ordering and prevent duplication
                EnableIdempotence = true,
                Acks = Acks.All

            };
            //Console.WriteLine(config.ApiVersionRequest);

            using (var p = new ProducerBuilder<long, string>(config).Build())
            {
                string brokerList = config.BootstrapServers.ToString();

                try
                {
                    Console.WriteLine("Sending " + numberOfEventsSent + " message/s to topic: " + topic + ", broker(s): " + brokerList);

                    string msg;

                    for (int i = 1; i < numberOfEventsSent + 1; i++)
                    {
                        if (useJsonAsEventFormat)
                        {
                            msg = DeliverEventMsgAsJson(dumpJsonOutput);

                        }
                        else
                        {
                            msg = DeliverEventMsgAsString(i);
                        }

                        //Send the message to the kafka cluster

                        var deliveryReport = await p.ProduceAsync(topic, new Message<long, string> { Key = DateTime.UtcNow.Ticks, Value = msg });

                        //output the sended message in console
                        Console.WriteLine(string.Format("Message {0} sent (value: '{1}') to TopicPartition and its Offset: {2}", i, msg, deliveryReport.TopicPartitionOffset));
                    }

                }
                catch (ProduceException<long, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                }
            }

            Console.ReadLine();
            /*
             * https://xyzcoder.github.io/kafka/kafka_csharp/confluent_kafka/kafka_on_windows/2019/02/26/getting-started-with-kafka-and-c.html
             * 
             */
        }
        
        //option 1: Delivering message as json
        private static string DeliverEventMsgAsJson(bool dumpJsonOutput)
        {
            string msg;
            
            Router router = new Router();
            RouterTelemetry routerTel = new RouterTelemetry();

            //Generate Random Router telemtry Data
            router = routerTel.GenerateRouterData();

            //wrap the telemetry data in json format
            string JSONresult = JsonConvert.SerializeObject(router);

            //Dump the json output on local machine for review
            if (dumpJsonOutput.Equals(true))
            {
                Utilities.DumpJsonOutput(JSONresult);
            }

            msg = JSONresult;
            return msg;
        }
        //option 2: Delivering message as a | string
        private static string DeliverEventMsgAsString(int i)
        {
            string msg;

            WifiTelemetryEvent tevent = getWifiTeleEvent();
            msg = string.Format("msgID: {0} | timestamp: {1} | ssid: {2} | signal: {3} | connectedDeviceID: {4} | downStreamKbps: {5} | upStreamKbps: {6} | routerTemp: {7}"
                , i
                , DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss.ff")
                , tevent.ssid
                , tevent.signal
                , tevent.connectedDeviceID
                , tevent.downStreamKbps
                , tevent.upStreamKbps
                , tevent.routerTemp
                );
            return msg;
        }

        public static WifiTelemetryEvent getWifiTeleEvent()
        {
            /*
                Generate Random wifi telemetry Data
             */
            WifiTelemetryEvent evt = new WifiTelemetryEvent();

            evt.ssid = "myWifiName123";
            evt.connectedDeviceID = new Random().Next(1, 3); // Up to 3 connected devices
            evt.signal = new Random().Next(-70, -30); //-70 dBm Weak | -60 dBm to -70 dBm Fair | -50 dBm to -60 dBm Good | > -50 dBm Excellent
            evt.downStreamKbps = new Random().Next(0, 150000); //max 150 mbit
            evt.upStreamKbps = new Random().Next(0, 150000); //max 150 mbit
            evt.routerTemp = new Random().Next(15, 45);
            return evt;
        }
    }
}
