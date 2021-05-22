package com.dynatrace.banestes_lab.pix_sensibiliza;


import com.dynatrace.banestes_lab.pix_sensibiliza.model.MyQueueMessageRepository;
import com.dynatrace.banestes_lab.pix_sensibiliza.model.MyQueueMessage;
import com.dynatrace.oneagent.sdk.OneAgentSDKFactory;
import com.dynatrace.oneagent.sdk.api.IncomingMessageProcessTracer;
import com.dynatrace.oneagent.sdk.api.IncomingMessageReceiveTracer;
import com.dynatrace.oneagent.sdk.api.OneAgentSDK;
import com.dynatrace.oneagent.sdk.api.OutgoingMessageTracer;
import com.dynatrace.oneagent.sdk.api.enums.ChannelType;
import com.dynatrace.oneagent.sdk.api.enums.MessageDestinationType;
import com.dynatrace.oneagent.sdk.api.infos.MessagingSystemInfo;
import java.io.BufferedWriter;
import java.io.FileWriter;
import java.io.IOException;
import java.util.Collections;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class PixSensibilizaApplication implements CommandLineRunner{
        @Autowired
        MyQueueMessageRepository myQueueMessageRepository;
        
        private static Logger LOG = LoggerFactory.getLogger(PixSensibilizaApplication.class);
	public static void main(String[] args) {
		//SpringApplication.run(PixSensibilizaApplication.class, args);
                SpringApplication app = new SpringApplication(PixSensibilizaApplication.class);
                app.setDefaultProperties(Collections
                    .singletonMap("server.port", "8084"));
                app.run(args);
	}
        
        @Override
        public void run(String... args) throws IOException, InterruptedException {
            OneAgentSDK oneAgentSdk = OneAgentSDKFactory.createInstance();
            MessagingSystemInfo messagingSystemInfo = oneAgentSdk.createMessagingSystemInfo("DBMessaging", "table_queue", MessageDestinationType.QUEUE, ChannelType.OTHER, null);
            IncomingMessageProcessTracer incomingMessageProcessTracer = oneAgentSdk.traceIncomingMessageProcess(messagingSystemInfo);
            
            while (true){
                String messagePacket = "";
                for (int i=0; i<10; i++){
                    try{
                        incomingMessageProcessTracer.start();
                        MyQueueMessage myQueueMessage = myQueueMessageRepository.findTopByOrderByIdAsc();
                        incomingMessageProcessTracer.setDynatraceStringTag(myQueueMessage.getDt_header());
                        incomingMessageProcessTracer.setVendorMessageId(String.valueOf(myQueueMessage.getId()));
                        myQueueMessageRepository.delete(myQueueMessage);
                        messagePacket += myQueueMessage.toJson();
                        LOG.info("PROCESSING MESSAGE : " + myQueueMessage.toString());
                        incomingMessageProcessTracer.end();
                    }
                    catch (Exception e){
                        System.out.println("Deu erro: " + e.getMessage());
                        e.printStackTrace();
                        Thread.sleep(5000);
                    }
                }
                
            BufferedWriter writer = new BufferedWriter(new FileWriter("myFile.txt"));
            writer.write(messagePacket);
            writer.close();
            }
    }
}
