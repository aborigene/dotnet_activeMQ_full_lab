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
                string returnString = "{\"message_id\":\"" + this.message_id + "\", \"operation_count\":\"" + this.operation_count + "\",[";
                int i = 1;
                foreach (Operation operation in operations) {
                    returnString += operation.ToString();
                    if (i != operations.Length) returnString += ",";
                }
                returnString = "}";
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
            IOutgoingWebRequestTracer tracer = OneAgentSdk.TraceOutgoingWebRequest("http://127.0.0.1:5000/new_pix", "GET");
            
            await tracer.Trace(async () =>
            {
                newBacenMessage = await RequestBacen();
            });
            
            //String message = i + " - Hello World!";
            SendMessage(newBacenMessage, session, connection, producer, messagingSystemInfo);

        }
        static async Task<BacenMessage> RequestBacen()
        {
            //Int64 i = 0;
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            Console.WriteLine("Retrieving PIX infor from http://127.0.0.1:5000/new_pix");
            Uri pix_message_uri = new Uri("http://127.0.0.1:5000/new_pix");

            Task <String> client_response = client.GetStringAsync(pix_message_uri);
            
            String msg = await client_response;
            Console.WriteLine("Received message: "+ msg);
            BacenMessage bacenMessage = JsonConvert.DeserializeObject<BacenMessage>(msg);
            return bacenMessage;

        }

        static void SendMessage(BacenMessage newBacenMessage, ISession session, IConnection connection, IMessageProducer producer, IMessagingSystemInfo messagingSystemInfo)
        {
            IOutgoingMessageTracer outgoingMessageTracer = OneAgentSdk.TraceOutgoingMessage(messagingSystemInfo);
            outgoingMessageTracer.Start();
            Console.WriteLine("Sending message: " + newBacenMessage.message_id);
            ITextMessage request = session.CreateTextMessage(newBacenMessage.ToString());
            request.NMSCorrelationID = newBacenMessage.message_id;

            string outgoing_tag = outgoingMessageTracer.GetDynatraceStringTag();
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

        static async Task Main(string[] args)
        {
            string connection_url = "activemq:tcp://127.0.0.1:61616";
            Uri connecturi = new Uri(connection_url);
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(async (o) =>
                   {
                       string destination_queue = "queue://treepix.queue.bacen.response";
                       bool daemon_mode = o.DaemonMode;
                       int interval = o.Interval;

                       Console.WriteLine("About to connect to " + connecturi);

                       // NOTE: ensure the nmsprovider-activemq.config file exists in the executable folder.
                       IConnectionFactory factory = new NMSConnectionFactory(connecturi);

                       //IOneAgentSdk oneAgentSdk = oneAgentSdk = OneAgentSdkFactory.CreateInstance();

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
                               //Int64 i = 0;


                               if (daemon_mode)
                               {
                                   while (true)
                                   {
                                       await ProcessBacenNewMessage(session, connection, producer, messagingSystemInfo);
                                       Thread.Sleep(interval);
                                   }
                               }
                               else
                               {
                                   Console.WriteLine("Processing single message...");
                                   await ProcessBacenNewMessage(session, connection, producer, messagingSystemInfo);
                                   while (true) Console.WriteLine(".");
                               }
                               
                           }
                       }
                       //Thread.Sleep(500000);
                   });
        }
    }
}
