using System;
using Apache.NMS;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS.Util;
using Dynatrace.OneAgent.Sdk.Api;
using Dynatrace.OneAgent.Sdk.Api.Enums;
using Dynatrace.OneAgent.Sdk.Api.Infos;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using CommandLine;

namespace bacen_receiver
{
    class Program
    {
        public class BacenMessage
        {
            public string message_id { get; set; }
            public int operation_count { get; set; }
            public Operation[] operations { get; set; }

            public override string ToString()
            {
                string returnString = "{\"message_id\":\"" + this.message_id + "\", \"operation_count\":\"" + this.operation_count + "\", \"operations\": [";
                int i = 1;
                foreach (Operation operation in operations) {
                    returnString += operation.ToString();
                    if (i != operations.Length) returnString += ",";
                    i++;
                }
                returnString += "]}";
                return returnString;
            }
        }

        public class Operation
        {
            public string operation_id { get; set; }
            public int operation_type { get; set; }
            public int pix_ammount { get; set; }

            public override string ToString()
            {
                return "{\"operation_id\":\"" + this.operation_id + "\",\"operation_type\":\"" + this.operation_type + "\",\"pix_ammount\":\"" + this.pix_ammount + "\"}";
            }
        }

        static async Task ProcessBacenNewMessage(ISession session, IConnection connection, IMessageProducer producer, IMessagingSystemInfo messagingSystemInfo)
        {
            BacenMessage newBacenMessage = new BacenMessage();
            string pixURL = "http://127.0.0.1:5000/new_pix";
            IOutgoingWebRequestTracer tracer = OneAgentSdk.TraceOutgoingWebRequest(pixURL, "GET");

            /*await tracer.Trace(async () =>
            {
                try
                {*/
                    newBacenMessage = await RequestBacen(pixURL);
                /*}
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }*/
                
            //});
            
            //String message = i + " - Hello World!";
            SendMessage(newBacenMessage, session, connection, producer, messagingSystemInfo);

        }
        static async Task<BacenMessage> RequestBacen(string pixURL)
        {
            Uri pix_message_uri = new Uri(pixURL);
            Console.WriteLine("Retrieving PIX info from "+pixURL);
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                //using (HttpResponseMessage response = await client.GetAsync(pix_message_uri).ConfigureAwait(false))
                //using (HttpContent content = response.Content) 
                //using (string )
                try
                {
                    HttpResponseMessage response = await client.GetAsync(pix_message_uri);//.ConfigureAwait(false);
                    HttpContent content = response.Content;
                    string msg = await content.ReadAsStringAsync();
                    Console.WriteLine("Received message: " + msg);
                    BacenMessage bacenMessage = JsonConvert.DeserializeObject<BacenMessage>(msg);
                    return bacenMessage;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");	
                    Console.WriteLine("Message :{0} ",e.Message);
                    return null;
                }
            }
        }

        static void SendMessage(BacenMessage newBacenMessage, ISession session, IConnection connection, IMessageProducer producer, IMessagingSystemInfo messagingSystemInfo)
        {
            IOutgoingMessageTracer outgoingMessageTracer = OneAgentSdk.TraceOutgoingMessage(messagingSystemInfo);
            outgoingMessageTracer.Start();
            Console.WriteLine("Sending message: " + newBacenMessage.message_id);
            ITextMessage request = session.CreateTextMessage(newBacenMessage.ToString());
            request.NMSCorrelationID = newBacenMessage.message_id;

            string outgoing_tag = outgoingMessageTracer.GetDynatraceStringTag();
            Console.WriteLine("Correlation ID: "+outgoing_tag);
            request.Properties[OneAgentSdkConstants.DYNATRACE_MESSAGE_PROPERTYNAME] = outgoing_tag; //GetDynatraceByteTag();

            producer.Send(request);

            outgoingMessageTracer.SetCorrelationId(request.NMSCorrelationID);    // optional
            outgoingMessageTracer.SetVendorMessageId(request.NMSMessageId); // optional
            outgoingMessageTracer.End();
        }

        class StdErrLoggingCallback : ILoggingCallback
        {
            public void Error(string message) => Console.Error.WriteLine("[OneAgent SDK] Error: " + message);

            public void Warn(string message) => Console.Error.WriteLine("[OneAgent SDK] Warning: " + message);
        }

        private static readonly Lazy<IOneAgentSdk> _oneAgentSdk = new Lazy<IOneAgentSdk>(() =>
        {
            IOneAgentSdk instance = OneAgentSdkFactory.CreateInstance();
            instance.SetLoggingCallback(new StdErrLoggingCallback());
            return instance;
        });

        public static IOneAgentSdk OneAgentSdk => _oneAgentSdk.Value;

        public class Options
        { 
            [Option('d', "daemon", Required = false, HelpText = "If set, application will be executed as a service that checks the bacen fake HTTP endpoint periodically. If not set, command will get one message and send to the queue for processing.")]
            public bool DaemonMode { get; set; }

            [Option('i', "interval", Required = false, HelpText = "Interval in milis which the service will check the Bacen fake HTTP service.")]
            public int Interval { get; set; }
        }

        public static async Task DoEverything(string[] args)
        {
            string connection_url = "activemq:tcp://127.0.0.1:61616";
            Uri connecturi = new Uri(connection_url);
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(async (o) =>
                   {
                       bool daemon_mode = o.DaemonMode;
                       int interval = o.Interval;
                       string destination_queue = "queue://treepix.queue.bacen.response";

                       Console.WriteLine("About to connect to " + connecturi);
                       // NOTE: ensure the nmsprovider-activemq.config file exists in the executable folder.
                       IConnectionFactory factory = new NMSConnectionFactory(connecturi);
                       IMessagingSystemInfo messagingSystemInfo = OneAgentSdk.CreateMessagingSystemInfo(MessageSystemVendor.ACTIVE_MQ, destination_queue, MessageDestinationType.QUEUE, ChannelType.TCP_IP, connection_url);

                       //IInProcessLink inProcessLink = OneAgentSdk.CreateInProcessLink();
                       using (IConnection connection = factory.CreateConnection("guest", "guest"))
                       using (ISession session = connection.CreateSession())
                       {
                           IDestination destination = SessionUtil.GetDestination(session, destination_queue);
                           Console.WriteLine("Using destination: " + destination);
                           // Create a consumer and producer
                           using (IMessageConsumer consumer = session.CreateConsumer(destination))
                           using (IMessageProducer producer = session.CreateProducer(destination))
                           {
                               // Start the connection so that messages will be processed.
                               connection.Start();
                               producer.DeliveryMode = MsgDeliveryMode.Persistent;

                               if (daemon_mode)
                               {
                                   while (true)
                                   {
                                       ProcessBacenNewMessage(session, connection, producer, messagingSystemInfo).Wait();
                                       Thread.Sleep(interval);
                                   }
                               }
                               else
                               {
                                   ProcessBacenNewMessage(session, connection, producer, messagingSystemInfo).Wait();
                                   Thread.Sleep(100000);
                               }

                               
                           }
                       }

                   });
            
        }

        static void Main(string[] args)
        {
            DoEverything(args).Wait();
        }
    }
}
