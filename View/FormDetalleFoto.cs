using SIVUG.Models;
using SIVUG.Models.DAO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIVUG.View
{
    public partial class FormDetalleFoto : Form
    {

        private List<Foto> _listaFotos;       // La cadena completa de fotos
        private int _indiceActual;   // El puntero a la foto que estamos viendo

        // Datos
        private Foto _fotoActual;
        private Candidata _candidataActual;
        private Estudiante _estudianteLogueado;
        // DAO para persistencia
        private ComentarioDao _comentarioDao;

        // Controles de UI
        private PictureBox pbFotoGrande;
        private FlowLayoutPanel panelListaComentarios;
        private TextBox txtNuevoComentario;
        private Button btnEnviar;
        private Label lblTituloFoto;
        private Label lblSubtitulo; 
        private Button btnCerrar;   
        private Button btnAnterior;
        private Button btnSiguiente;

        public FormDetalleFoto(Foto foto,List<Foto> listaAlbum, Estudiante estudianteLogueado)
        {
            InitializeComponent();

           
            _listaFotos = listaAlbum;
            _estudianteLogueado = estudianteLogueado;
            _comentarioDao= new ComentarioDao();

            _indiceActual = _listaFotos.FindIndex(f => f.Id == foto.Id);

            // Si por alguna razón no la encuentra (-1), empezamos en la 0
            if (_indiceActual == -1) _indiceActual = 0;

            _fotoActual = foto;
          

            // 1. Configuración de la Ventana (Sin bordes estándar para hacerlo moderno)
            this.Text = "Detalle";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None; // Quitamos el borde de Windows
            this.BackColor = Color.FromArgb(20, 20, 20);
            InicializarComponentes();

            // Cargar la primera vista
            CargarVistaActual();
        }

        private void FormDetalleFoto_Load(object sender, EventArgs e)
        {

        }

        private void InicializarComponentes()
        {
            TableLayoutPanel layoutPrincipal = new TableLayoutPanel();
            layoutPrincipal.Dock = DockStyle.Fill;
            layoutPrincipal.ColumnCount = 2;
            layoutPrincipal.RowCount = 1;
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350F));
            this.Controls.Add(layoutPrincipal);

            // --- LADO IZQUIERDO (FOTO) ---
            Panel panelIzquierdo = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };

            // 1. Foto
            pbFotoGrande = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            panelIzquierdo.Controls.Add(pbFotoGrande);

            // 2. Panel de botones flotantes
            Panel panelControles = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(100, 0, 0, 0) };

            btnAnterior = CrearBotonNavegacion("< Anterior");
            btnAnterior.Dock = DockStyle.Left;
            // ASIGNACIÓN DE EVENTO DIRECTA
            btnAnterior.Click += (s, e) => CambiarFoto(-1); // Ir atrás

            btnSiguiente = CrearBotonNavegacion("Siguiente >");
            btnSiguiente.Dock = DockStyle.Right;
            // ASIGNACIÓN DE EVENTO DIRECTA
            btnSiguiente.Click += (s, e) => CambiarFoto(1); // Ir adelante

            panelControles.Controls.Add(btnSiguiente);
            panelControles.Controls.Add(btnAnterior);

            panelIzquierdo.Controls.Add(panelControles);

            // ¡CRUCIAL PARA QUE LOS CLICS FUNCIONEN!
            panelControles.BringToFront();

            layoutPrincipal.Controls.Add(panelIzquierdo, 0, 0);

            // --- LADO DERECHO (CHAT) ---
            Panel panelDerecho = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            // Header
            Panel headerChat = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.WhiteSmoke, Padding = new Padding(10) };
            btnCerrar = new Button { Text = "✕", Size = new Size(40, 40), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            lblTituloFoto = new Label { Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true, Location = new Point(10, 15), MaximumSize = new Size(260, 0) };
            lblSubtitulo = new Label { Font = new Font("Segoe UI", 8), ForeColor = Color.Gray, Location = new Point(10, 50), AutoSize = true };

            headerChat.Controls.Add(btnCerrar);
            headerChat.Controls.Add(lblTituloFoto);
            headerChat.Controls.Add(lblSubtitulo);

            // Lista Comentarios
            panelListaComentarios = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.White, Padding = new Padding(0, 10, 0, 0) };

            // Footer
            Panel footerChat = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.WhiteSmoke, Padding = new Padding(10) };
            Button btnEnviar = new Button { Text = "Enviar", Width = 70, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.RoyalBlue, ForeColor = Color.White };
            btnEnviar.Click += BtnEnviar_Click;
            txtNuevoComentario = new TextBox { Multiline = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };

            footerChat.Controls.Add(txtNuevoComentario);
            footerChat.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 10 });
            footerChat.Controls.Add(btnEnviar);

            panelDerecho.Controls.Add(panelListaComentarios);
            panelDerecho.Controls.Add(headerChat);
            panelDerecho.Controls.Add(footerChat);

            layoutPrincipal.Controls.Add(panelDerecho, 1, 0);
        }

        private Button CrearBotonNavegacion(string texto)
        {
            return new Button
            {
                Text = texto,
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(50, 255, 255, 255) }
            };
        }

        private void CambiarFoto(int direccion)
        {
            // direccion será -1 (atrás) o +1 (adelante)
            int nuevoIndice = _indiceActual + direccion;

            // Validación de límites
            if (nuevoIndice >= 0 && nuevoIndice < _listaFotos.Count)
            {
                _indiceActual = nuevoIndice;
                CargarVistaActual();
            }
        }

        // ---------------- LOGICA DE NAVEGACIÓN Y CARGA ----------------

        private void CargarVistaActual()
        {
            if (_listaFotos == null || _listaFotos.Count == 0) return;

            _fotoActual = _listaFotos[_indiceActual]; // <-- Actualizamos la referencia

            // 1. Cargar Imagen y Textos (Igual que antes)
            if (File.Exists(_fotoActual.RutaArchivo))
                pbFotoGrande.Image = Image.FromFile(_fotoActual.RutaArchivo);
            else
                pbFotoGrande.Image = null;

    

            // 2. Actualizar estado de botones (Igual que antes)
            btnAnterior.Enabled = (_indiceActual > 0);
            btnSiguiente.Enabled = (_indiceActual < _listaFotos.Count - 1);
            // ... (colores de botones) ...

            // 3. CARGAR COMENTARIOS REALES DE LA BD
            CargarComentariosDB();
        }

        // ---------------- LOGICA DE COMENTARIOS ----------------

        private void CargarComentariosDB()
        {
            panelListaComentarios.Controls.Clear();

            try
            {
                // Usamos el DAO para traer la lista de la BD
                List<Comentario> comentariosBD = _comentarioDao.ObtenerPorFotoId(_fotoActual.Id);

                // 2. Actualizar Título con Conteo (LINQ)
                int totalComentarios = comentariosBD.Count(); // Usamos Count() de LINQ o la propiedad de lista
                lblTituloFoto.Text = $"Comentarios ({totalComentarios})";

                // El subtítulo ahora llevará la descripción del álbum o la foto
                lblSubtitulo.Text = _fotoActual.Album != null ? _fotoActual.Album.Titulo : "Detalle de Foto";

                // 3. Validar si está vacío (Empty State)
                if (totalComentarios == 0)
                {
                    Label lblVacio = new Label();
                    lblVacio.Text = "Sin comentarios.\n¡Sé el primero en agregar uno!";
                    lblVacio.AutoSize = false;
                    // Hacemos que ocupe el ancho del panel y una altura decente para centrar
                    lblVacio.Size = new Size(panelListaComentarios.Width - 25, 200);
                    lblVacio.TextAlign = ContentAlignment.MiddleCenter;
                    lblVacio.ForeColor = Color.Gray;
                    lblVacio.Font = new Font("Segoe UI", 10, FontStyle.Italic);

                    panelListaComentarios.Controls.Add(lblVacio);
                    return; // Salimos, no hay burbujas que pintar
                }

                foreach (Comentario c in comentariosBD)
                {
                    // Preparar datos para la burbuja
                    string nombreAutor = $"{c.Estudiante.Nombres} {c.Estudiante.Apellidos}";

                    Image avatar = null;
                    string rutaFoto = c.Estudiante.FotoPerfilRuta;

                    // Validar si existe ruta de perfil y cargar imagen
                    // Lógica robusta para la imagen
                    if (!string.IsNullOrEmpty(rutaFoto) && File.Exists(rutaFoto))
                    {
                        try
                        {
                            // 1. Leemos los bytes al instante (sin bloquear el archivo)
                            byte[] bytes = File.ReadAllBytes(rutaFoto);
                            // 2. Usamos streams temporales
                            using (MemoryStream ms = new MemoryStream(bytes))
                            {
                                // 3. Cargamos la imagen original temporalmente
                                using (Image imagenOriginal = Image.FromStream(ms))
                                {
                                    // 4. ¡EL TRUCO! Creamos un Bitmap nuevo copiando los píxeles.
                                    // Esto rompe el enlace con el MemoryStream, permitiendo que se cierre sin errores.
                                    avatar = new Bitmap(imagenOriginal);
                                }
                            }
                        }
                        catch
                        {
                            avatar = null; // Si falla, se va null (el control pondrá una por defecto)
                        }
                    }
                    // NOTA: CommentBubble debe manejar internamente el texto de "hace X tiempo" usando la fecha.
                    AgregarBurbujaComentario(nombreAutor, c.Contenido, c.FechaComentario, avatar);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar comentarios: " + ex.Message);
            }
        }

        private void AgregarBurbujaComentario(string nombre, string texto, DateTime fecha, Image avatar)
        {
            CommentBubble burbuja = new CommentBubble();
            burbuja.Width = 320;
            burbuja.SetData(nombre, texto, fecha, avatar);
            panelListaComentarios.Controls.Add(burbuja);
            panelListaComentarios.ScrollControlIntoView(burbuja);
        }

        private void BtnEnviar_Click(object sender, EventArgs e)
        {
            string texto = txtNuevoComentario.Text.Trim();
            if (string.IsNullOrEmpty(texto)) return;

            try
            {
                Comentario nuevoComentario = new Comentario(texto, _estudianteLogueado, _fotoActual);
                _comentarioDao.Guardar(nuevoComentario);

                // Limpiar textbox
                txtNuevoComentario.Text = "";

                // RECARGAR TODO DESDE LA BD
                CargarComentariosDB();

                // Scroll al fondo para ver tu nuevo mensaje
                panelListaComentarios.ScrollControlIntoView(panelListaComentarios.Controls[panelListaComentarios.Controls.Count - 1]);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo enviar el comentario: " + ex.Message);
            }
        }

       
        }
    }


