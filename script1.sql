
create database sivug;
use sivug;

-- 1. Tabla Padre
CREATE TABLE personas (
    id_persona INT AUTO_INCREMENT PRIMARY KEY, -- Nuevo ID
    dni VARCHAR(20) NOT NULL UNIQUE,
    nombres VARCHAR(100),
    apellidos VARCHAR(100),
    edad TINYINT
);

-- 2. Tabla Hija (Relación 1 a 1 por Herencia)
CREATE TABLE estudiantes (
    id_estudiante INT PRIMARY KEY, -- Es PK y FK a la vez (apunta a personas.id_persona)
    matricula VARCHAR(20) UNIQUE,
    carrera VARCHAR(100),
    semestre TINYINT,
    facultad VARCHAR(100),
    ha_votado_reina BOOLEAN DEFAULT 0,
    ha_votado_fotogenia BOOLEAN DEFAULT 0,
    
    CONSTRAINT fk_persona_estudiante 
    FOREIGN KEY (id_estudiante) REFERENCES personas(id_persona)
);

select *from personas;
select *from estudiantes;