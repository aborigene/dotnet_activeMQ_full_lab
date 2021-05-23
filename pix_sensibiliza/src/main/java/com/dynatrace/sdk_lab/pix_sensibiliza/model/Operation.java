package com.dynatrace.sdk_lab.pix_processor.model;


import java.io.Serializable;

public class Operation implements Serializable{
    private static final long serialVersionUID = 300002228479017363L;

    public String operation_id;
    public int operation_type;
    public int pix_ammount;
    
    public Operation(String operation_id, int operation_type, int pix_ammount){
        this.operation_id = operation_id;
        this.operation_type = operation_type;
        this.pix_ammount = pix_ammount;
    }
    
    public String getOperation_id(){
        return this.operation_id;
    }
    
    public int getOperation_type(){
        return this.operation_type;
    }
    
    public int getPix_ammount(){
        return this.pix_ammount;
    }
    
}
