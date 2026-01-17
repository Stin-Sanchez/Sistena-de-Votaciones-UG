using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace SIVUG.View
{
    public partial class CommentBubble : UserControl
    {

        // 1. Declaramos los componentes visuales como variables privadas
        private PictureBox pbAvatar;
        private Label lblNombre;
        private Label lblComentario;
        private Label lblFecha;
        private Panel panelSeparador; // Linea decorativa opcional

        public CommentBubble()
        {
            // Configuración básica del contenedor (la burbuja en sí)
            this.BackColor = Color.White;
            this.Padding = new Padding(10);
            this.DoubleBuffered = true; // Para evitar parpadeos al redibujar
            this.Width = 400; // Ancho inicial por defecto (se ajustará luego)

            InitializeComponent();
            InitComponentes();
          
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void FormComentarios_Load(object sender, EventArgs e)
        {

        }

        private void InitComponentes()
        {
            // --- A. Avatar (PictureBox) ---
            pbAvatar = new PictureBox();
            pbAvatar.Size = new Size(45, 45); // Tamaño fijo 45x45
            pbAvatar.Location = new Point(10, 10); // Margen superior e izquierdo
            pbAvatar.SizeMode = PictureBoxSizeMode.Zoom;

            // --- B. Nombre (Label) ---
            lblNombre = new Label();
            lblNombre.AutoSize = true;
            lblNombre.Location = new Point(65, 10); // A la derecha del avatar
            lblNombre.Font = new Font("Segoe UI", 10, FontStyle.Bold); // Fuente negrita
            lblNombre.ForeColor = Color.FromArgb(40, 40, 40); // Gris oscuro

            // --- C. Comentario (Label) ---
            lblComentario = new Label();
            lblComentario.AutoSize = true; // Importante para que crezca
            lblComentario.Location = new Point(65, 30); // Debajo del nombre
            lblComentario.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblComentario.ForeColor = Color.Black;
            // MaximumSize es CLAVE: obliga al texto a hacer salto de línea si es muy largo
            lblComentario.MaximumSize = new Size(this.Width - 80, 0);

            // --- D. Fecha (Label) ---
            lblFecha = new Label();
            lblFecha.AutoSize = true;
            lblFecha.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            lblFecha.ForeColor = Color.Gray;
            // La posición Y se calculará dinámicamente luego, por ahora 0
            lblFecha.Location = new Point(65, 0);

            // --- E. Agregar todo al control ---
            this.Controls.Add(pbAvatar);
            this.Controls.Add(lblNombre);
            this.Controls.Add(lblComentario);
            this.Controls.Add(lblFecha);
        }

        // 3. Método público para llenar los datos y recalcular el diseño
        public void SetData(string nombre, string comentario, DateTime fecha, Image avatar)
        {
            // Asignar datos
            lblNombre.Text = nombre;
            lblComentario.Text = comentario;
            lblFecha.Text = CalcularTiempo(fecha);

            // Imagen Circular
            if (avatar != null)
                pbAvatar.Image = RecortarImagenCircular(avatar);
            else
                pbAvatar.BackColor = Color.LightGray; // Color por defecto si no hay foto

            // --- RECALCULO DE DISEÑO (Responsive) ---

            // 1. Ajustar el ancho máximo del texto según el ancho actual del control
            lblComentario.MaximumSize = new Size(this.Width - 80, 0);

            // 2. Calcular la posición de la fecha (debe ir debajo del comentario)
            int yPosFecha = lblComentario.Location.Y + lblComentario.Height + 5;
            lblFecha.Location = new Point(65, yPosFecha);

            // 3. Calcular la altura total de este UserControl
            int alturaTotal = lblFecha.Location.Y + lblFecha.Height + 15; // +15 de margen inferior

            // Asegurarnos de que no sea más pequeño que el avatar (45px + margen)
            if (alturaTotal < 70) alturaTotal = 70;

            this.Height = alturaTotal;
        }

        // Auxiliar: Formato de tiempo "Hace X minutos"
        private string CalcularTiempo(DateTime fecha)
        {
            TimeSpan diferencia = DateTime.Now - fecha;
            if (diferencia.TotalMinutes < 1) return "Justo ahora";
            if (diferencia.TotalMinutes < 60) return $"Hace {Math.Floor(diferencia.TotalMinutes)} min";
            if (diferencia.TotalHours < 24) return $"Hace {Math.Floor(diferencia.TotalHours)} h";
            return fecha.ToString("dd/MM/yyyy");
        }

        // Auxiliar: Recortar imagen en círculo
        private Image RecortarImagenCircular(Image img)
        {
            Bitmap bmp = new Bitmap(img.Width, img.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(0, 0, img.Width, img.Height);
                    using (TextureBrush tb = new TextureBrush(img))
                    {
                        tb.WrapMode = WrapMode.Clamp;
                        g.FillPath(tb, gp);
                    }
                }
            }
            return bmp;
        }

        // Evento opcional: Si redimensionan el control manualmente, reajustar texto
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (lblComentario != null)
            {
                lblComentario.MaximumSize = new Size(this.Width - 80, 0);
            }
        }
    }
}

