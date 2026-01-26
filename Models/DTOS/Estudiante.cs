using SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models
{
    public enum Rol
    {
        ADMINISTRADOR=0,
        ESTUDIANTE=1,
        CANDIDATA=2
    }



    // Representa un estudiante de la universidad que puede votar en el concurso
    public class Estudiante : Persona
    {


        //Matrícula única del estudiante
        public int Id { get; set; }
        public string Matricula { get; set; }
        // --- CAMBIO: Relación Normalizada ---
        // ID para la base de datos
        public int IdCarrera { get; set; }
        // Objeto completo para mostrar nombre en la vista
        public Carrera Carrera { get; set; }

        public string FotoPerfilRuta {  get; set; }

        //Semestre actual (1-10 típicamente)
        public byte Semestre { get; set; }

        //Rol en el sistema
        public Rol RolEstudiante { get; set; }
       
        /*Indica si el estudiante ya emitió su voto para Reina
            NOTA: Se actualiza en la capa de servicio después de persistir el voto*/
        public bool HavotadoReina { get; set; }

        /*Indica si el estudiante ya emitió su voto para Fotogenia
             NOTA: Se actualiza en la capa de servicio después de persistir el voto*/
        public bool HavotadoFotogenia { get; set; }


        //Metodo votar funciona como una fabrica de votaciones crea un nuevo voto y lo retorna

        /// <summary>Constructor por defecto</summary>
        public Estudiante() { }

        /*
         Crea un nuevo estudiante con sus datos académicos
         <param name="havotadoReina">Por defecto false - no ha votado
         <param name="havotadoFotogenia">Por defecto false - no ha votado
       
         */
        // Constructor actualizado
        public Estudiante(string matricula, Carrera carrera, byte semestre,
                          bool havotadoReina = false, bool havotadoFotogenia = false)
        {
            this.Matricula = matricula;
            // Asignamos tanto el objeto como el ID para mantener consistencia
            this.Carrera = carrera;
            if (carrera != null) this.IdCarrera = carrera.Id;

            this.Semestre = semestre;
            this.HavotadoReina = havotadoReina;
            this.HavotadoFotogenia = havotadoFotogenia;
        }

        /*
         *Factory method que crea un nuevo Voto asociado a este estudiante.
          NO valida si ya votó ni persiste datos - eso es responsabilidad de la capa de servicio.
         
         <param name="candidata">Candidata que recibe el voto</param>
          <param name="tipo">Tipo de votación (Reina/Fotogenia)</param>
          <returns>Nuevo objeto Voto sin persistir</returns>

          NOTA IMPORTANTE:
          La capa de servicio debe:
          1. Validar si ya votó (HavotadoReina/HavotadoFotogenia)
          2. Persistir el voto en BD
          3. Actualizar las propiedades HavotadoReina/HavotadoFotogenia
         * 
         */


        public Voto Votar(Candidata candidata, TipoVoto tipo)
        {
            return new Voto
            {
                FechaVotacion = DateTime.Now,
                Candidata = candidata,
                Tipo = tipo,
                Sufragador = this  // Asocia automáticamente este estudiante
            };
        }

        /*
         *  Factory method que crea un nuevo comentario asociado a este estudiante para una foto específica.
         NO valida ni persiste datos - eso es responsabilidad de la capa de servicio.
          
         <param name="contenido">Texto del comentario a publicar</param>
          <param name="foto">Foto que será comentada</param>
          <returns>Nuevo objeto Comentario sin persistir</returns>
         
          NOTA IMPORTANTE:
          La capa de servicio debe:
          1. Validar el contenido (no vacío, longitud máxima, etc.)
          2. Verificar que la foto existe y está activa
          3. Persistir el comentario en BD
         */
       
        public Comentario ComentarFoto(string contenido, Foto foto)
        {
            return new Comentario(contenido, this,foto);
         
        }


    }

        
        



}
