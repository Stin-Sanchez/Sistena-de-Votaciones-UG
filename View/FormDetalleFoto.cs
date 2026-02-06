using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
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
    /// <summary>
    /// VISTA DE DETALLE: Presenta una foto en alta resolución y su hilo de comentarios.
    /// Funciona como un "Modal Moderno" sin bordes para centrar la atención en el contenido.
    /// </summary>
    public partial class FormDetalleFoto : Form
    {
        // Navegación: Necesito la lista completa para ir "Anterior" y "Siguiente".
        private List<Foto> _listaFotos;       
        private int _indiceActual;   

        // Modelos de datos en contexto.
        private Foto _fotoActual;
        private Candidata _candidataActual;
        
        // Usuario actual para asociar los comentarios nuevos.
        private Estudiante _estudianteLogueado;
        
        // Acceso a datos de comentarios.
        private ComentarioDao _comentarioDao;

        // Referencias a controles UI generados dinámicamente.
        private PictureBox pbFotoGrande;
        private FlowLayoutPanel panelListaComentarios;
        private TextBox txtNuevoComentario;
        private Button btnEnviar;
        private Label lblTituloFoto;
        private Label lblSubtitulo; 
        private Button btnCerrar;   
        private Button btnAnterior;
        private Button btnSiguiente;

        /// <summary>
        /// Constructor: Recibe el contexto necesario para navegar y comentar.
        /// </summary>
        /// <param name="foto">Foto inicial a mostrar.</param>
        /// <param name="listaAlbum">Álbum completo para navegación.</param>
        /// <param name="usuarioLogueado">Quien está viendo/comentando.</param>
        public FormDetalleFoto(Foto foto, List<Foto> listaAlbum, Estudiante usuarioLogueado)
        {
            InitializeComponent();

            _listaFotos = listaAlbum;
            _estudianteLogueado = usuarioLogueado;
            _comentarioDao= new ComentarioDao();

            // Localizo la foto inicial en la lista para saber dónde estoy parado.
            _indiceActual = _listaFotos.FindIndex(f => f.Id == foto.Id);

            // Fallback de seguridad: Si no la encuentro, empiezo desde la primera.
            if (_indiceActual == -1) _indiceActual = 0;

            _fotoActual = foto;
          
            // Configuración visual "Modal Sin Bordes" para inmersión.
            this.Text = "Detalle";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None; 
            this.BackColor = Color.FromArgb(20, 20, 20); // Fondo oscuro tipo "Cine".
            
            InicializarComponentes();

            // Renderizo el contenido inicial.
            CargarVistaActual();
        }

        private void FormDetalleFoto_Load(object sender, EventArgs e)
        {
                            
        }

        /// <summary>
        /// Construcción de UI compleja: Divide la pantalla en Foto (Izquierda) y Chat (Derecha).
        /// </summary>
        private void InicializarComponentes()
        {
            TableLayoutPanel layoutPrincipal = new TableLayoutPanel();
            layoutPrincipal.Dock = DockStyle.Fill;
            layoutPrincipal.ColumnCount = 2;
            layoutPrincipal.RowCount = 1;
            
            // Columna 0 (Foto): 75% del ancho. Columna 1 (Chat): 350px fijos.
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350F));
            this.Controls.Add(layoutPrincipal);

            // --- LADO IZQUIERDO (VISOR DE FOTO) ---
            Panel panelIzquierdo = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };

            // 1. Imagen Central
            pbFotoGrande = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            panelIzquierdo.Controls.Add(pbFotoGrande);

            // 2. Panel de navegación flotante (Overlay)
            Panel panelControles = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(100, 0, 0, 0) };

            btnAnterior = CrearBotonNavegacion("< Anterior");
            btnAnterior.Dock = DockStyle.Left;
            btnAnterior.Click += (s, e) => CambiarFoto(-1); // Navegar atrás

            btnSiguiente = CrearBotonNavegacion("Siguiente >");
            btnSiguiente.Dock = DockStyle.Right;
            btnSiguiente.Click += (s, e) => CambiarFoto(1); // Navegar adelante

            panelControles.Controls.Add(btnSiguiente);
            panelControles.Controls.Add(btnAnterior);

            panelIzquierdo.Controls.Add(panelControles);

            // Z-Index: Aseguro que los botones queden ENCIMA de la foto.
            panelControles.BringToFront();

            layoutPrincipal.Controls.Add(panelIzquierdo, 0, 0);

            // --- LADO DERECHO (PANEL DE INTERACCIÓN) ---
            Panel panelDerecho = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            // Header: Título y botón cerrar.
            Panel headerChat = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.WhiteSmoke, Padding = new Padding(10) };
            
            btnCerrar = new Button { Text = "✕", Size = new Size(40, 40), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            lblTituloFoto = new Label { Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true, Location = new Point(10, 15), MaximumSize = new Size(260, 0) };
            lblSubtitulo = new Label { Font = new Font("Segoe UI", 8), ForeColor = Color.Gray, Location = new Point(10, 50), AutoSize = true };

            headerChat.Controls.Add(btnCerrar);
            headerChat.Controls.Add(lblTituloFoto);
            headerChat.Controls.Add(lblSubtitulo);

            // Lista Comentarios: Scrollable y dinámica.
            panelListaComentarios = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.White, Padding = new Padding(0, 10, 0, 0) };

            // Footer: Caja de texto para escribir.
            Panel footerChat = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.WhiteSmoke, Padding = new Padding(10) };
            
            // Corrección de error NullReference: Asigno directamente al campo de clase, no a una variable local.
            btnEnviar = new Button { Text = "Enviar", Width = 70, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.RoyalBlue, ForeColor = Color.White };
            btnEnviar.Click += BtnEnviar_Click;
            
            txtNuevoComentario = new TextBox { Multiline = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };

            footerChat.Controls.Add(txtNuevoComentario);
            footerChat.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 10 }); // Espaciador
            footerChat.Controls.Add(btnEnviar);

            panelDerecho.Controls.Add(panelListaComentarios);
            panelDerecho.Controls.Add(headerChat);
            panelDerecho.Controls.Add(footerChat);

            // Aplico restricciones de UI según permisos.
            ConfigurarModoComentarios();

            layoutPrincipal.Controls.Add(panelDerecho, 1, 0);
        }

        /// <summary>
        /// Aplica reglas de negocio sobre quién puede comentar.
        /// Si es Admin (usuarioLogueado es null), se bloquea la escritura (Solo Lectura).
        /// </summary>
        private void ConfigurarModoComentarios()
        {
            if (_estudianteLogueado != null)
                return; // Es estudiante, puede comentar.

            // Es Admin o invitado sin permisos de escritura.
            txtNuevoComentario.Enabled = false;
            btnEnviar.Enabled = false; 
            txtNuevoComentario.Text = "Solo lectura (Administrador)";
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

        /// <summary>
        /// Lógica de carrusel de imágenes.
        /// </summary>
        /// <param name="direccion">-1 para anterior, +1 para siguiente.</param>
        private void CambiarFoto(int direccion)
        {
            int nuevoIndice = _indiceActual + direccion;

            // Validación de límites de array para evitar IndexOutOfRangeException.
            if (nuevoIndice >= 0 && nuevoIndice < _listaFotos.Count)
            {
                _indiceActual = nuevoIndice;
                CargarVistaActual(); // Recargo la interfaz con la nueva foto.
            }
        }

        // ---------------- LOGICA DE NAVEGACIÓN Y CARGA ----------------

        private void CargarVistaActual()
        {
            if (_listaFotos == null || _listaFotos.Count == 0) return;

            _fotoActual = _listaFotos[_indiceActual]; // Actualizo el puntero de datos.

            // 1. Cargar Imagen de Archivo (IO)
            if (File.Exists(_fotoActual.RutaArchivo))
                pbFotoGrande.Image = Image.FromFile(_fotoActual.RutaArchivo);
            else
                pbFotoGrande.Image = null; // Placeholder o vacío si falla.

            // 2. Gestionar estado de botones de navegación (Deshabilitar si es fin/inicio).
            btnAnterior.Enabled = (_indiceActual > 0);
            btnSiguiente.Enabled = (_indiceActual < _listaFotos.Count - 1);

            // 3. Traer los comentarios frescos de la BD para esta foto específica.
            CargarComentariosDB();
        }

        // ---------------- LOGICA DE COMENTARIOS ----------------

        private void CargarComentariosDB()
        {
            panelListaComentarios.Controls.Clear();

            try
            {
                List<Comentario> comentariosBD = _comentarioDao.ObtenerPorFotoId(_fotoActual.Id);

                // Actualizo contadores en el header.
                int totalComentarios = comentariosBD.Count();
                lblTituloFoto.Text = $"Comentarios ({totalComentarios})";
                lblSubtitulo.Text = _fotoActual.Album != null ? _fotoActual.Album.Titulo : "Detalle de Foto";

                // Empty State: Feedback si no hay interacción aún.
                if (totalComentarios == 0)
                {
                    Label lblVacio = new Label();
                    lblVacio.Text = "Sin comentarios.\n¡Sé el primero en agregar uno!";
                    lblVacio.AutoSize = false;
                    lblVacio.Size = new Size(panelListaComentarios.Width - 25, 200);
                    lblVacio.TextAlign = ContentAlignment.MiddleCenter;
                    lblVacio.ForeColor = Color.Gray;
                    lblVacio.Font = new Font("Segoe UI", 10, FontStyle.Italic);

                    panelListaComentarios.Controls.Add(lblVacio);
                    return;
                }

                // Renderizo cada comentario como una "Burbuja" de chat.
                foreach (Comentario c in comentariosBD)
                {
                    string nombreAutor = $"{c.Estudiante.Nombres} {c.Estudiante.Apellidos}";
                    Image avatar = null;
                    string rutaFoto = c.Estudiante.FotoPerfilRuta;

                    // TRUCO DE MEMORIA: Carga de imagen sin bloquear el archivo en disco.
                    // Esto permite que el usuario cambie su foto de perfil mientras la app corre.
                    if (!string.IsNullOrEmpty(rutaFoto) && File.Exists(rutaFoto))
                    {
                        try
                        {
                            byte[] bytes = File.ReadAllBytes(rutaFoto);
                            using (MemoryStream ms = new MemoryStream(bytes))
                            using (Image imagenOriginal = Image.FromStream(ms))
                            {
                                avatar = new Bitmap(imagenOriginal); // Copia en memoria profunda
                            }
                        }
                        catch
                        {
                            avatar = null; 
                        }
                    }
                    
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
            // Uso un UserControl personalizado "CommentBubble" para encapsular el diseño.
            CommentBubble burbuja = new CommentBubble();
            burbuja.Width = 320;
            burbuja.SetData(nombre, texto, fecha, avatar);
            
            panelListaComentarios.Controls.Add(burbuja);
            
            // Auto-scroll al último mensaje para mejor UX.
            panelListaComentarios.ScrollControlIntoView(burbuja);
        }

        private void BtnEnviar_Click(object sender, EventArgs e)
        {
            // Doble validación de seguridad.
            if (_estudianteLogueado == null)
            {
                MessageBox.Show("Los administradores no pueden comentar.");
                return;
            }

            string texto = txtNuevoComentario.Text.Trim();
            if (string.IsNullOrEmpty(texto)) return; // No enviar vacíos.

            try
            {
                Comentario nuevoComentario = new Comentario(texto, _estudianteLogueado, _fotoActual);
                _comentarioDao.Guardar(nuevoComentario);

                // UX: Limpio el campo inmediatamente para feedback de "Enviado".
                txtNuevoComentario.Text = "";

                // Recargo la lista completa para asegurarme de que lo que veo es consistente con la BD.
                CargarComentariosDB();

                // Auto-scroll al fondo.
                if (panelListaComentarios.Controls.Count > 0)
                {
                    panelListaComentarios.ScrollControlIntoView(panelListaComentarios.Controls[panelListaComentarios.Controls.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo enviar el comentario: " + ex.Message);
            }
        }
    }
}


