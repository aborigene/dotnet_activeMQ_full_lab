using System;
using Apache.NMS;
using System.Threading;
using Apache.NMS.Util;
using Dynatrace.OneAgent.Sdk.Api;
using Dynatrace.OneAgent.Sdk.Api.Enums;
using Dynatrace.OneAgent.Sdk.Api.Infos;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace bacen_processor
{
    class Program
    {
        private static readonly Lazy<IOneAgentSdk> _oneAgentSdk = new Lazy<IOneAgentSdk>(() =>
        {
            IOneAgentSdk instance = OneAgentSdkFactory.CreateInstance();
            instance.SetLoggingCallback(new StdErrLoggingCallback());
            return instance;
        });

        class StdErrLoggingCallback : ILoggingCallback
        {
            public void Error(string message) => Console.Error.WriteLine("[OneAgent SDK] Error: " + message);

            public void Warn(string message) => Console.Error.WriteLine("[OneAgent SDK] Warning: " + message);
        }

        public static IOneAgentSdk OneAgentSdk => _oneAgentSdk.Value;
        public class BacenMessage
        {
            public string message_id { get; set; }
            public int operation_count { get; set; }
            public Operation[] operations { get; set; }
        }

        public class Operation
        {
            public string operation_id { get; set; }
            public int operation_type { get; set; }
            public int pix_ammount { get; set; }
        }

        public static void ReceiveMessage(IMessageConsumer consumer, ISession session, IMessageProducer producer, IMessagingSystemInfo messagingSystemInfo)
        {
            IIncomingMessageReceiveTracer receiveTracer = OneAgentSdk.TraceIncomingMessageReceive(messagingSystemInfo);
            
            Console.WriteLine("Waiting for new message...");
            ITextMessage message = consumer.Receive() as ITextMessage;
            receiveTracer.Start();

            IIncomingMessageProcessTracer processTracer = OneAgentSdk.TraceIncomingMessageProcess(messagingSystemInfo);

            string property = message.Properties[OneAgentSdkConstants.DYNATRACE_MESSAGE_PROPERTYNAME].ToString();
            Console.WriteLine(property);


            //if (message.Properties..Contains(OneAgentSdkConstants.DYNATRACE_MESSAGE_PROPERTYNAME))
            if (message.Properties.Contains(OneAgentSdkConstants.DYNATRACE_MESSAGE_PROPERTYNAME) && !message.Properties.GetString(OneAgentSdkConstants.DYNATRACE_MESSAGE_PROPERTYNAME).Equals(""))
            {
                Console.WriteLine("Iniciando OneAgent...");
                string properties = message.Properties.GetString(OneAgentSdkConstants.DYNATRACE_MESSAGE_PROPERTYNAME);
                Console.WriteLine("Correlation header: "+properties);
                processTracer.SetDynatraceStringTag(properties);
            }
            // start processing:
            processTracer.Start();
            //processTracer.SetCorrelationId(message.Properties.);           // optional
            //processTracer.SetVendorMessageId(receiveResult.VendorMessageId); // optional
            ProcessMessage(message, session, producer, messagingSystemInfo);
            processTracer.End();
            receiveTracer.End();
        }

        public static void ProcessMessage(ITextMessage message, ISession session, IMessageProducer producer, IMessagingSystemInfo messagingSystemInfo)
        {
            if (message == null)
            {
                Console.WriteLine("No message received!");
            }
            else
            {
                BacenMessage bacenMessage = JsonConvert.DeserializeObject<BacenMessage>(message.Text);
                for (int i = 0; i < bacenMessage.operation_count; i++)
                {
                    IOutgoingMessageTracer outgoingMessageTracer = OneAgentSdk.TraceOutgoingMessage(messagingSystemInfo);
                    outgoingMessageTracer.Start();

                    string operationMessage = JsonConvert.SerializeObject(bacenMessage.operations[i]);
                    ITextMessage request = session.CreateTextMessage(operationMessage);

                    string outgoing_tag = outgoingMessageTracer.GetDynatraceStringTag();
                    request.Properties[OneAgentSdkConstants.DYNATRACE_MESSAGE_PROPERTYNAME] = outgoing_tag;
                    
                    Console.WriteLine("Sending message to Java:" + request.Text);
                    producer.Send(request);
                    
                    outgoingMessageTracer.SetCorrelationId(request.NMSCorrelationID);    // optional
                    outgoingMessageTracer.SetVendorMessageId(request.NMSMessageId); // optional
                    outgoingMessageTracer.End();

                }
                //Console.WriteLine("Received message with ID:   " + message.NMSMessageId);
                //Console.WriteLine("Received message with text: " + message.Text);
            }
        }
        static void Main(string[] args)
        {

            string source_queue = "queue://treepix.queue.bacen.response";
            string destination_queue = "queue://banestes.queue.pix";
            //string active_mq_ip = Environment.GetEnvironmentVariable("ACTIVE_MQ_IP");
            string connection_url = "activemq:tcp://127.0.0.1:61616";
            Uri connecturi = new Uri(connection_url);

            Console.WriteLine("About to connect to " + connecturi);

            // NOTE: ensure the nmsprovider-activemq.config file exists in the executable folder.
            IConnectionFactory factory = new NMSConnectionFactory(connecturi);

            //IOneAgentSdk oneAgentSdk = OneAgentSdkFactory.CreateInstance();

            IMessagingSystemInfo messagingSystemInfo = OneAgentSdk.CreateMessagingSystemInfo(MessageSystemVendor.ACTIVE_MQ, source_queue, MessageDestinationType.QUEUE, ChannelType.TCP_IP, connection_url);

            using (IConnection connection = factory.CreateConnection("guest", "guest"))
            using (ISession session = connection.CreateSession())
            {
                //connection.
                IDestination source = SessionUtil.GetDestination(session, source_queue);
                IDestination destination = SessionUtil.GetDestination(session, destination_queue);
                Console.WriteLine("Using source: " + source);

                // Create a consumer and producer
                using (IMessageConsumer consumer = session.CreateConsumer(source))
                using (IMessageProducer producer = session.CreateProducer(destination))
                {
                    // Start the connection so that messages will be processed.
                    connection.Start();
                    producer.DeliveryMode = MsgDeliveryMode.Persistent;

                    // Consume a message
                    while (true)
                    {
                        ReceiveMessage(consumer, session, producer, messagingSystemInfo);
                        //Thread.Sleep(2000);
                    }
                }
            }
        }
    }
}
