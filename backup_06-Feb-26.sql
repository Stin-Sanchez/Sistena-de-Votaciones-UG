CREATE DATABASE  IF NOT EXISTS `sivug` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `sivug`;
-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
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
-- Table structure for table `albumes`
--

DROP TABLE IF EXISTS `albumes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `albumes` (
  `id_album` int NOT NULL AUTO_INCREMENT,
  `titulo` varchar(150) NOT NULL,
  `descripcion` text,
  `id_candidata` int NOT NULL,
  `fecha_creacion` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_album`),
  KEY `fk_album_candidata` (`id_candidata`),
  CONSTRAINT `fk_album_candidata` FOREIGN KEY (`id_candidata`) REFERENCES `candidatas` (`id_candidata`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `candidata_detalles`
--

DROP TABLE IF EXISTS `candidata_detalles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `candidata_detalles` (
  `Id_Detalle` int NOT NULL AUTO_INCREMENT,
  `Id_Candidata` int DEFAULT NULL,
  `Id_Catalogo` int DEFAULT NULL,
  PRIMARY KEY (`Id_Detalle`),
  KEY `fk_candidata_detalle` (`Id_Candidata`),
  KEY `fk_candidata_catalogo` (`Id_Catalogo`),
  CONSTRAINT `fk_candidata_catalogo` FOREIGN KEY (`Id_Catalogo`) REFERENCES `catalogos` (`IdCatalogo`),
  CONSTRAINT `fk_candidata_detalle` FOREIGN KEY (`Id_Candidata`) REFERENCES `candidatas` (`id_candidata`)
) ENGINE=InnoDB AUTO_INCREMENT=49 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

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
  UNIQUE KEY `nombre` (`nombre`),
  KEY `id_facultad` (`id_facultad`),
  CONSTRAINT `carreras_ibfk_1` FOREIGN KEY (`id_facultad`) REFERENCES `facultades` (`id_facultad`)
) ENGINE=InnoDB AUTO_INCREMENT=34 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `catalogos`
--

DROP TABLE IF EXISTS `catalogos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `catalogos` (
  `IdCatalogo` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(100) NOT NULL,
  `Tipo` varchar(20) NOT NULL,
  PRIMARY KEY (`IdCatalogo`),
  UNIQUE KEY `UQ_Nombre_Tipo` (`Nombre`,`Tipo`)
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `comentarios`
--

DROP TABLE IF EXISTS `comentarios`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `comentarios` (
  `id_comentario` bigint NOT NULL AUTO_INCREMENT,
  `contenido` text NOT NULL,
  `fecha_comentario` datetime NOT NULL,
  `id_estudiante` int NOT NULL,
  `id_foto` int NOT NULL,
  PRIMARY KEY (`id_comentario`),
  KEY `fk_comentario_estudiante` (`id_estudiante`),
  KEY `fk_comentario_foto` (`id_foto`),
  CONSTRAINT `fk_comentario_estudiante` FOREIGN KEY (`id_estudiante`) REFERENCES `estudiantes` (`id_estudiante`) ON DELETE CASCADE,
  CONSTRAINT `fk_comentario_foto` FOREIGN KEY (`id_foto`) REFERENCES `fotos` (`id_foto`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `estudiantes`
--

DROP TABLE IF EXISTS `estudiantes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `estudiantes` (
  `id_estudiante` int NOT NULL,
  `matricula` varchar(20) NOT NULL,
  `semestre` tinyint DEFAULT NULL,
  `id_carrera` int DEFAULT NULL,
  `ruta_foto_perfil` varchar(60) DEFAULT NULL,
  PRIMARY KEY (`id_estudiante`),
  UNIQUE KEY `matricula` (`matricula`),
  KEY `fk_estudiante_carrera` (`id_carrera`),
  CONSTRAINT `fk_estudiante_carrera` FOREIGN KEY (`id_carrera`) REFERENCES `carreras` (`id_carrera`),
  CONSTRAINT `fk_persona_estudiante` FOREIGN KEY (`id_estudiante`) REFERENCES `personas` (`id_persona`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `facultades`
--

DROP TABLE IF EXISTS `facultades`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `facultades` (
  `id_facultad` int NOT NULL AUTO_INCREMENT,
  `nombre` varchar(100) NOT NULL,
  PRIMARY KEY (`id_facultad`),
  UNIQUE KEY `nombre` (`nombre`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `fotos`
--

DROP TABLE IF EXISTS `fotos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `fotos` (
  `id_foto` int NOT NULL AUTO_INCREMENT,
  `ruta_archivo` varchar(500) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL,
  `id_album` int NOT NULL,
  `fecha_subida` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_foto`),
  KEY `fk_foto_album` (`id_album`),
  CONSTRAINT `fk_foto_album` FOREIGN KEY (`id_album`) REFERENCES `albumes` (`id_album`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

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
  `id_usuario` int DEFAULT NULL,
  PRIMARY KEY (`id_persona`),
  UNIQUE KEY `dni` (`dni`),
  UNIQUE KEY `id_usuario` (`id_usuario`),
  CONSTRAINT `FK_Personas_Cuenta_Usuarios` FOREIGN KEY (`id_usuario`) REFERENCES `usuarios` (`id_usuario`)
) ENGINE=InnoDB AUTO_INCREMENT=36 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles` (
  `id_rol` int NOT NULL AUTO_INCREMENT,
  `nombre` varchar(50) NOT NULL,
  PRIMARY KEY (`id_rol`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `usuarios`
--

DROP TABLE IF EXISTS `usuarios`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `usuarios` (
  `id_usuario` int NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `id_rol` int NOT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT '1',
  `requiere_cambio_contrasena` tinyint(1) DEFAULT '1',
  `fecha_registro` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_usuario`),
  UNIQUE KEY `username` (`username`),
  KEY `FK_Usuarios_Roles` (`id_rol`),
  CONSTRAINT `FK_Usuarios_Roles` FOREIGN KEY (`id_rol`) REFERENCES `roles` (`id_rol`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

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
) ENGINE=InnoDB AUTO_INCREMENT=38 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping routines for database 'sivug'
--
/*!50003 DROP PROCEDURE IF EXISTS `sp_obtener_foto_comentarios_estudiante` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_obtener_foto_comentarios_estudiante`(IN p_id_foto BIGINT)
BEGIN
    SELECT 
        c.id_comentario,
        c.contenido,
        c.fecha_comentario,
        e.id_estudiante,
        e.matricula,
        e.ruta_foto_perfil,
        p.nombres,
        p.apellidos
    FROM comentarios c
    INNER JOIN estudiantes e ON c.id_estudiante = e.id_estudiante
    INNER JOIN personas p ON e.id_estudiante = p.id_persona
    WHERE c.id_foto = p_id_foto
    ORDER BY c.fecha_comentario ASC;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_roles_obtener_todos` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_roles_obtener_todos`()
BEGIN
    SELECT id_rol, nombre
    FROM roles
    ORDER BY nombre ASC;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;


INSERT IGNORE INTO roles (nombre)
VALUES 
    ('Administrador'),
    ('Estudiante');
      
      -- ============================================================================
-- 5. INSERTAR USUARIO ADMIN
-- ============================================================================
INSERT IGNORE INTO usuarios (username, password_hash, id_rol, activo)
SELECT 
    'admin',
    '03AC674216F3E15C761EE1A5E255F067953623C8B388B4459E13F978D7C846F4',
    id_rol,
    1
FROM roles
WHERE nombre = 'Administrador';

-- Insertar persona admin
INSERT IGNORE INTO personas (dni, nombres, apellidos, edad, id_usuario)
SELECT 
    '0000-00',
    'Administrador',
    'Sistema',
    25,
    id_usuario
FROM usuarios
WHERE username = 'admin';

-- 1. Desactivar revisión de claves foráneas temporalmente para limpiar
SET FOREIGN_KEY_CHECKS = 0;

-- 2. Limpiar tablas (TRUNCATE resetea los contadores a 0)
TRUNCATE TABLE carreras;
TRUNCATE TABLE facultades;

SET FOREIGN_KEY_CHECKS = 1;

-- ---------------------------------------------------------
-- 3. INSERTAR FACULTADES (Con IDs explícitos)
-- ---------------------------------------------------------
INSERT INTO Facultades (id_facultad, nombre) VALUES
(1, 'Facultad de Ciencias Matemáticas y Físicas'),
(2, 'Facultad de Ciencias Administrativas'),
(3, 'Facultad de Ciencias Económicas'),
(4, 'Facultad de Ingeniería Industrial'),
(5, 'Facultad de Ciencias Químicas'),
(6, 'Facultad de Ciencias Médicas'),
(7, 'Facultad de Jurisprudencia y Ciencias Sociales y Políticas'),
(8, 'Facultad de Filosofía, Letras y Ciencias de la Educación'),
(9, 'Facultad de Arquitectura y Urbanismo'),
(10, 'Facultad de Psicología'),
(11, 'Facultad de Comunicación Social (FACSO)'),
(12, 'Facultad de Ingeniería Química'),
(13, 'Facultad de Ciencias Agrarias'),
(14, 'Facultad de Medicina Veterinaria y Zootecnia'),
(15, 'Facultad de Educación Física, Deportes y Recreación'),
(16, 'Facultad Piloto de Odontología'),
(17, 'Facultad de Ciencias Naturales');

-- ---------------------------------------------------------
-- 4. INSERTAR CARRERAS (Vinculadas por el ID de Facultad)
-- ---------------------------------------------------------

-- ID 1: Matemáticas y Físicas (Aquí suele estar Software)
INSERT INTO Carreras (id_facultad, nombre) VALUES
(1, 'Ingeniería de Software'),
(1, 'Ingeniería en Tecnologías de la Información'),
(1, 'Ingeniería Civil'),
(1, 'Ingeniería en Sistemas Computacionales'); -- (Plan antiguo/cierre)

-- ID 2: Administrativas
INSERT INTO Carreras (id_facultad, nombre) VALUES
(2, 'Licenciatura en Administración de Empresas'),
(2, 'Contabilidad y Auditoría (CPA)'),
(2, 'Licenciatura en Finanzas'),
(2, 'Gestión de la Información Gerencial'),
(2, 'Comercio Exterior'),
(2, 'Negocios Internacionales'),
(2, 'Marketing');

-- ID 3: Económicas
INSERT INTO Carreras (id_facultad, nombre) VALUES
(3, 'Economía'),
(3, 'Economía Internacional');

-- ID 4: Ingeniería Industrial
INSERT INTO Carreras (id_facultad, nombre) VALUES
(4, 'Ingeniería Industrial'),
(4, 'Ingeniería en Telemática');

-- ID 5: Químicas
INSERT INTO Carreras (id_facultad, nombre) VALUES
(5, 'Bioquímica y Farmacia');

-- ID 6: Médicas
INSERT INTO Carreras (id_facultad, nombre) VALUES
(6, 'Medicina'),
(6, 'Enfermería'),
(6, 'Obstetricia'),
(6, 'Terapia Ocupacional'),
(6, 'Terapia Respiratoria'),
(6, 'Fonoaudiología'),
(6, 'Dietética y Nutrición');

-- ID 7: Jurisprudencia
INSERT INTO Carreras (id_facultad, nombre) VALUES
(7, 'Derecho'),
(7, 'Sociología');

-- ID 8: Filosofía (Educación)
INSERT INTO Carreras (id_facultad, nombre) VALUES
(8, 'Pedagogía de las Ciencias Experimentales (Informática)'),
(8, 'Pedagogía de los Idiomas Nacionales y Extranjeros'),
(8, 'Pedagogía de la Lengua y Literatura'),
(8, 'Pedagogía de la Historia y las Ciencias Sociales'),
(8, 'Educación Básica'),
(8, 'Educación Inicial'),
(8, 'Psicopedagogía');

-- ID 9: Arquitectura
INSERT INTO Carreras (id_facultad, nombre) VALUES
(9, 'Arquitectura'),
(9, 'Diseño de Interiores');

-- ID 10: Psicología
INSERT INTO Carreras (id_facultad, nombre) VALUES
(10, 'Psicología');

-- ID 11: Comunicación Social
INSERT INTO Carreras (id_facultad, nombre) VALUES
(11, 'Comunicación'),
(11, 'Diseño Gráfico'),
(11, 'Publicidad'),
(11, 'Turismo');

-- ID 12: Ingeniería Química
INSERT INTO Carreras (id_facultad, nombre) VALUES
(12, 'Ingeniería Química'),
(12, 'Ingeniería en Alimentos'),
(12, 'Gastronomía');

-- ID 13: Agrarias
INSERT INTO Carreras (id_facultad, nombre) VALUES
(13, 'Agronomía');

-- ID 14: Veterinaria
INSERT INTO Carreras (id_facultad, nombre) VALUES
(14, 'Medicina Veterinaria y Zootecnia');

-- ID 15: Educación Física
INSERT INTO Carreras (id_facultad, nombre) VALUES
(15, 'Pedagogía de la Actividad Física y Deporte');

-- ID 16: Odontología
INSERT INTO Carreras (id_facultad, nombre) VALUES
(16, 'Odontología');

-- ID 17: Ciencias Naturales
INSERT INTO Carreras (id_facultad, nombre) VALUES
(17, 'Biología'),
(17, 'Ingeniería Ambiental'),
(17, 'Geología');

-- Dump completed on 2026-02-06 11:46:52
