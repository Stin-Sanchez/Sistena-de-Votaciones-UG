using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models
{

    
    // Define los tipos de votación disponibles en el sistema
    public enum TipoVoto
    {
        Reina,
        Fotogenia
    }
    //Representa un voto emitido por un estudiante hacia una candidata
    public class Voto
    {

        //Identificador unico del voto en la BD
        public long Id { get; set; }

        //Fecha y hora en que se registró el voto
        public DateTime FechaVotacion { get; set; }

        /*
         * Relacion Many to one con candidata
         * Muchos votos le pertenecen a una sola candidata
         */
        public Candidata Candidata { get; set; }
        /*
         * Relacion One to One con estudiante
         * Un voto le pertenece a un solo estudiante sufragador
         * NOTA: Un estudiante puede tener maximo 2 votos uno de cada tipo
         */
        public Estudiante Sufragador { get; set; }

        //Tipo de votacion Reina o Fotogenia
        public TipoVoto Tipo { get; set; }


        //Constructor por defecto
        public Voto()
        {
        }

        //Constructor parametrizado
        public Voto(Candidata candidata, Estudiante sufragador, TipoVoto tipo)
        {
            Candidata = candidata;
            Sufragador = sufragador;
            Tipo = tipo;
            FechaVotacion = DateTime.Now;
        }


    }
}
