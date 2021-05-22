CREATE DATABASE `messaging-db`;

USE `messaging-db`;

DROP TABLE IF EXISTS `pix_db_messaging`;
CREATE TABLE `my_queue_message` (
  `id` int NOT NULL AUTO_INCREMENT,
  `pix_ammount` int NOT NULL,
  `queue_name` varchar(500) not null,
  `dt_header` varchar(1000) NOT NULL,
  PRIMARY KEY (`id`)
)

