/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.dynatrace.sdk_lab.pix_processor.model;

/**
 *
 * @author igor.simoes
 */

import java.sql.Date;

import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.GenerationType;
import javax.persistence.Id;
import com.google.gson.*;

@Entity
public class MyQueueMessage {
    @Id @GeneratedValue(strategy = GenerationType.IDENTITY)
    private int id;
    private int pix_ammount;
    private String queue_name;
    private String dt_header;//  NOT NULL,
    
    protected MyQueueMessage() {}

    public MyQueueMessage(int id, int pix_ammount, String queue_name, String dt_header) throws Exception{
        this.id=id;
        this.pix_ammount=pix_ammount;
        this.queue_name=queue_name;
        this.dt_header=dt_header;
    }
    
    public MyQueueMessage(int pix_ammount, String queue_name, String dt_header) throws Exception{
        this.pix_ammount=pix_ammount;
        this.queue_name=queue_name;
        this.dt_header=dt_header;
    }
    
    @Override
    public String toString(){
        return String.format("Message[id='%s', pix_ammount='%s', queue_name='%s', dt_header='%s']", id, pix_ammount, queue_name, dt_header);
    }

    public String toJson(){
        return new Gson().toJson(this);
    }
    
    private void setId(int id){
        this.id = id;
    }
    
    public int getId(){
        return this.id;
    }
    
    private void setPix_ammount(int pix_ammount){
        this.id = id;
    }
    
    public int getPix_ammount(){
        return this.pix_ammount;
    }
    
    private void setQueue_name(String queue_name){
        this.queue_name = queue_name;
    }
    
    public String getQueue_name(){
        return this.queue_name;
    }
    
    private void setDt_header(String dt_header){
        this.dt_header = dt_header;
    }
    
    public String getDt_header(){
        return this.dt_header;
    }
}
