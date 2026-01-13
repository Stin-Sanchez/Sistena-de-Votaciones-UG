using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models
{
    // Clase base que representa una persona en el sistema
   public  class Persona
    {

        //Atributos de la clase Madre con sus respectivos set y get
        public int id {  get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string DNI { get; set; }
        public byte Edad { get; set; }

        //Constructor sin parametros
        public Persona()
        {
        }

        public Persona(int id, string nombres, string apellidos, string dNI, byte edad)
        {
            this.id = id;
            Nombres = nombres;
            Apellidos = apellidos;
            DNI = dNI;
            Edad = edad;
        }

        //Constructor parametrizado


        public string GetNombreCompleto()
        {
            return Nombres + Apellidos;
        }
       
    }
}
