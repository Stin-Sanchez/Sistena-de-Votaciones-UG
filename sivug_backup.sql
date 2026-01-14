-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: localhost    Database: sivug
-- ------------------------------------------------------
-- Server version	8.4.6

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `candidatas`
--

DROP TABLE IF EXISTS `candidatas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `candidatas` (
  `id_candidata` int NOT NULL,
  `url_foto` varchar(500) DEFAULT NULL,
  `activa` tinyint(1) DEFAULT '1',
  `tipo_candidatura` int NOT NULL DEFAULT '1',
  PRIMARY KEY (`id_candidata`),
  CONSTRAINT `fk_candidata_estudiante` FOREIGN KEY (`id_candidata`) REFERENCES `estudiantes` (`id_estudiante`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `candidatas`
--

LOCK TABLES `candidatas` WRITE;
/*!40000 ALTER TABLE `candidatas` DISABLE KEYS */;
INSERT INTO `candidatas` VALUES (2,'D:\\Proyectos\\Sistema de Gestion de Votaciones UG\\SIVUG\\SIVUG\\bin\\Debug\\ImagenesCandidatas\\candidata_0967150465_20260114010305.jpg',1,2),(13,'D:\\Proyectos\\Sistema de Gestion de Votaciones UG\\SIVUG\\SIVUG\\bin\\Debug\\ImagenesCandidatas\\candidata_0956152044_20260114004335.jpg',1,2),(16,'D:\\Proyectos\\Sistema de Gestion de Votaciones UG\\SIVUG\\SIVUG\\bin\\Debug\\ImagenesCandidatas\\candidata_0944485414_20260114004747.jpg!d',1,1),(17,'D:\\Proyectos\\Sistema de Gestion de Votaciones UG\\SIVUG\\SIVUG\\bin\\Debug\\ImagenesCandidatas\\candidata_0945154602_20260114135231.jpg',1,1),(18,'D:\\Proyectos\\Sistema de Gestion de Votaciones UG\\SIVUG\\SIVUG\\bin\\Debug\\ImagenesCandidatas\\candidata_09562612545_20260114152257.jpg',1,1);
/*!40000 ALTER TABLE `candidatas` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `carreras`
--

DROP TABLE IF EXISTS `carreras`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `carreras` (
  `id_carrera` int NOT NULL AUTO_INCREMENT,
  `nombre` varchar(100) NOT NULL,
  `id_facultad` int NOT NULL,
  PRIMARY KEY (`id_carrera`),
  KEY `id_facultad` (`id_facultad`),
  CONSTRAINT `carreras_ibfk_1` FOREIGN KEY (`id_facultad`) REFERENCES `facultades` (`id_facultad`)
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `carreras`
--

LOCK TABLES `carreras` WRITE;
/*!40000 ALTER TABLE `carreras` DISABLE KEYS */;
INSERT INTO `carreras` VALUES (1,'Ingeniería de Software',1),(2,'Ingeniería en Sistemas Computacionales',1),(3,'Ingeniería en Networking y Telecomunicaciones',1),(4,'Ingeniería Civil',1),(5,'Licenciatura en Contabilidad y Auditoría',2),(6,'Ingeniería en Marketing y Negociación Comercial',2),(7,'Licenciatura en Administración de Empresas',2),(8,'Comercio Exterior',2),(9,'Ingeniería Industrial',3),(10,'Ingeniería en Teleinformática',3),(11,'Medicina',4),(12,'Enfermería',4),(13,'Obstetricia',4),(14,'Odontología',4),(15,'Derecho',5),(16,'Sociología',5),(17,'Lic. en Actividad fisica y recreacion',6);
/*!40000 ALTER TABLE `carreras` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `estudiantes`
--

DROP TABLE IF EXISTS `estudiantes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `estudiantes` (
  `id_estudiante` int NOT NULL,
  `matricula` varchar(20) DEFAULT NULL,
  `semestre` tinyint DEFAULT NULL,
  `id_carrera` int DEFAULT NULL,
  PRIMARY KEY (`id_estudiante`),
  UNIQUE KEY `matricula` (`matricula`),
  KEY `fk_estudiante_carrera` (`id_carrera`),
  CONSTRAINT `fk_estudiante_carrera` FOREIGN KEY (`id_carrera`) REFERENCES `carreras` (`id_carrera`),
  CONSTRAINT `fk_persona_estudiante` FOREIGN KEY (`id_estudiante`) REFERENCES `personas` (`id_persona`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `estudiantes`
--

LOCK TABLES `estudiantes` WRITE;
/*!40000 ALTER TABLE `estudiantes` DISABLE KEYS */;
INSERT INTO `estudiantes` VALUES (1,'01002010101',5,1),(2,'00101001111',6,1),(3,'021515151555',5,1),(4,'1511165161',4,1),(12,'00251515151',5,1),(13,'44561651515',8,8),(16,'414141141',5,14),(17,'00021515844',8,13),(18,'0616161651165',8,6),(19,'06161419848',8,5),(20,'000106511161441',8,10),(21,'065165161',2,16),(22,'08026321498',5,6),(23,'0201051515151',9,17);
/*!40000 ALTER TABLE `estudiantes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `facultades`
--

DROP TABLE IF EXISTS `facultades`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `facultades` (
  `id_facultad` int NOT NULL AUTO_INCREMENT,
  `nombre` varchar(100) NOT NULL,
  PRIMARY KEY (`id_facultad`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `facultades`
--

LOCK TABLES `facultades` WRITE;
/*!40000 ALTER TABLE `facultades` DISABLE KEYS */;
INSERT INTO `facultades` VALUES (1,'Ciencias Matemáticas y Físicas'),(2,'Ciencias Administrativas'),(3,'Ingeniería Industrial'),(4,'Ciencias Médicas'),(5,'Jurisprudencia'),(6,'Actividad fisica y recreacion');
/*!40000 ALTER TABLE `facultades` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `personas`
--

DROP TABLE IF EXISTS `personas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `personas` (
  `id_persona` int NOT NULL AUTO_INCREMENT,
  `dni` varchar(20) NOT NULL,
  `nombres` varchar(100) DEFAULT NULL,
  `apellidos` varchar(100) DEFAULT NULL,
  `edad` tinyint DEFAULT NULL,
  PRIMARY KEY (`id_persona`),
  UNIQUE KEY `dni` (`dni`)
) ENGINE=InnoDB AUTO_INCREMENT=24 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `personas`
--

LOCK TABLES `personas` WRITE;
/*!40000 ALTER TABLE `personas` DISABLE KEYS */;
INSERT INTO `personas` VALUES (1,'08036939483','Stin Josue','Sanchez Mosquera',21),(2,'0967150465','Ariel','Reyes',22),(3,'00000000','User test','Testing',18),(4,'015151515','purbea01','pruab 3',22),(12,'0803639483','Stin Josue','Sanchez Mosquera',22),(13,'0956152044','Ana ','LLanez',25),(16,'0944485414','Luna','Odontotetona',25),(17,'0945154602','Daniela','Mora',22),(18,'09562612545','Angelica','Reyes',25),(19,'0945562246','Luis','Armany',18),(20,'08181481488','Damian','Mendez',19),(21,'094984494','Maria','Angeles',22),(22,'0956123612','Allison','Sevilla',22),(23,'0945623215','Robert','Holguim',54);
/*!40000 ALTER TABLE `personas` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `votos`
--

DROP TABLE IF EXISTS `votos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `votos` (
  `id_voto` bigint NOT NULL AUTO_INCREMENT,
  `fecha_votacion` datetime DEFAULT CURRENT_TIMESTAMP,
  `id_candidata` int NOT NULL,
  `id_estudiante` int NOT NULL,
  `tipo_voto` enum('Reina','Fotogenia') NOT NULL,
  PRIMARY KEY (`id_voto`),
  UNIQUE KEY `uq_un_voto_por_tipo` (`id_estudiante`,`tipo_voto`),
  KEY `fk_voto_candidata` (`id_candidata`),
  CONSTRAINT `fk_voto_candidata` FOREIGN KEY (`id_candidata`) REFERENCES `candidatas` (`id_candidata`) ON DELETE CASCADE,
  CONSTRAINT `fk_voto_estudiante` FOREIGN KEY (`id_estudiante`) REFERENCES `estudiantes` (`id_estudiante`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=29 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `votos`
--

LOCK TABLES `votos` WRITE;
/*!40000 ALTER TABLE `votos` DISABLE KEYS */;
INSERT INTO `votos` VALUES (1,'2026-01-14 01:07:38',2,12,'Reina'),(2,'2026-01-14 01:08:24',2,1,'Reina'),(3,'2026-01-14 01:09:04',13,13,'Reina'),(4,'2026-01-14 01:09:36',16,3,'Reina'),(5,'2026-01-14 01:10:11',2,4,'Reina'),(6,'2026-01-14 01:10:41',16,16,'Reina'),(7,'2026-01-14 01:13:36',2,16,'Fotogenia'),(8,'2026-01-14 13:53:21',17,17,'Fotogenia'),(9,'2026-01-14 13:54:00',17,1,'Fotogenia'),(10,'2026-01-14 13:55:41',17,2,'Fotogenia'),(11,'2026-01-14 14:22:24',17,17,'Reina'),(12,'2026-01-14 14:29:27',13,2,'Reina'),(13,'2026-01-14 14:30:31',13,3,'Fotogenia'),(14,'2026-01-14 14:31:32',13,4,'Fotogenia'),(15,'2026-01-14 14:32:00',13,12,'Fotogenia'),(16,'2026-01-14 14:32:24',17,13,'Fotogenia'),(17,'2026-01-14 14:35:02',17,18,'Fotogenia'),(18,'2026-01-14 14:35:12',2,18,'Reina'),(19,'2026-01-14 14:46:11',13,19,'Reina'),(20,'2026-01-14 14:51:57',2,19,'Fotogenia'),(21,'2026-01-14 15:24:22',18,20,'Fotogenia'),(22,'2026-01-14 16:00:47',2,21,'Fotogenia'),(23,'2026-01-14 16:00:58',18,21,'Reina'),(24,'2026-01-14 16:30:32',18,20,'Reina'),(25,'2026-01-14 16:41:44',18,22,'Reina'),(26,'2026-01-14 16:42:14',13,22,'Fotogenia'),(27,'2026-01-14 17:43:22',13,23,'Fotogenia'),(28,'2026-01-14 17:44:51',16,23,'Reina');
/*!40000 ALTER TABLE `votos` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-01-14 17:50:35
