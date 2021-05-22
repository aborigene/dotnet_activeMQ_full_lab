package com.dynatrace.banestes_lab.pix_processor.jms;

import com.dynatrace.banestes_lab.pix_processor.model.Operation;
import com.dynatrace.banestes_lab.pix_processor.model.MyQueueMessage;
import com.dynatrace.banestes_lab.pix_processor.model.MyQueueMessageRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.jms.annotation.JmsListener;
import org.springframework.jms.core.JmsTemplate;
import org.springframework.stereotype.Component;

import com.dynatrace.oneagent.sdk.OneAgentSDKFactory;
import com.dynatrace.oneagent.sdk.api.IncomingMessageProcessTracer;
import com.dynatrace.oneagent.sdk.api.IncomingMessageReceiveTracer;
import com.dynatrace.oneagent.sdk.api.OneAgentSDK;
import com.dynatrace.oneagent.sdk.api.OutgoingMessageTracer;
import com.dynatrace.oneagent.sdk.api.enums.ChannelType;
import com.dynatrace.oneagent.sdk.api.enums.MessageDestinationType;
import com.dynatrace.oneagent.sdk.api.infos.MessagingSystemInfo;

import javax.jms.Message;
import javax.jms.MessageListener;
import javax.jms.ObjectMessage;
import com.google.gson.*;
import org.apache.activemq.command.ActiveMQTextMessage;

@Component
@Slf4j
public class JmsConsumer implements MessageListener {
    @Autowired
    MyQueueMessageRepository myQueueMessageRepository;

    OneAgentSDK oneAgentSdk = OneAgentSDKFactory.createInstance();
    
    @Override
    @JmsListener(destination = "banestes.queue.pix")
    public void onMessage(Message message) {
        MessagingSystemInfo messagingSystemInfo = oneAgentSdk.createMessagingSystemInfo("DBMessaging", "table_queue", MessageDestinationType.QUEUE, ChannelType.OTHER, null);
        OutgoingMessageTracer outgoingMessageTracer = oneAgentSdk.traceOutgoingMessage(messagingSystemInfo);
        
        try{
            outgoingMessageTracer.start();
            
            Gson gson = new Gson();
            System.out.println(message.toString());
            
            String message_body = ((ActiveMQTextMessage) message).getText();//getBody(String.class);
            Operation operation = gson.fromJson(message_body, Operation.class);
            //message.
            //ObjectMessage objectMessage = (ObjectMessage)message;
            //Operation operation = (Operation)objectMessage.getObject();
            //do additional processing
            String queue_name;
            int pix_ammount = operation.getPix_ammount();
            String dt_header =  "";
            if (operation.getOperation_type() == 1 )  queue_name = "own_operation";
            else queue_name = "third_party_operation";
            if (pix_ammount>700){
                System.out.println("This is a risky transfer, lets sendo to risk queue");    
                queue_name += ".high_risk";                 
            }
            else{
                System.out.println("This is a NON risky transfer, lets sendo to normal queue");
                queue_name += ".low_risk";
            }
            MyQueueMessage message_to_send = new MyQueueMessage(pix_ammount, queue_name, outgoingMessageTracer.getDynatraceStringTag());
            MyQueueMessage sent_message = myQueueMessageRepository.save(message_to_send);
            outgoingMessageTracer.setVendorMessageId(String.valueOf(sent_message.getId()));
            System.out.println("SENDING: PIX Ammount: "+ operation.getPix_ammount() + ", Operation type: "+ operation.getOperation_type() + " to " + queue_name);
        } catch(Exception e) {
            System.out.println("Received Exception while processing message: "+ e);
            outgoingMessageTracer.error(e);
        } finally {
            outgoingMessageTracer.end();
        }
    }
}