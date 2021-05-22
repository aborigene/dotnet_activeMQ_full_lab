/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.dynatrace.banestes_lab.pix_processor.model;
import java.util.List;
import java.util.Optional;
import org.springframework.data.repository.CrudRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
/**
 *
 * @author igor.simoes
 */
public interface MyQueueMessageRepository extends CrudRepository<MyQueueMessage, String>{
    MyQueueMessage findTopByOrderByIdDesc();
}
