using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models
{
    // Clase base que representa una persona en el sistema
    class Persona
    {


        //Atributos de la clase Madre con sus respectivos set y get
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string DNI { get; set; }
        public byte Edad { get; set; }

        //Constructor sin parametros
        public Persona()
        {
        }

        //Constructor parametrizado
        public Persona(string nombres, string apellidos, string dNI, byte edad)
        {
            this.Nombres = nombres;
            this.Apellidos = apellidos;
            DNI = dNI;
            this.Edad = edad;
        }

        public string GetNombreCompleto()
        {
            return Nombres + Apellidos;
        }
       
    }
}
