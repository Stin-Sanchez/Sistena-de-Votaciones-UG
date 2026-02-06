using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using SIVUG.Models.SERVICES;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIVUG.View
{
    /// <summary>
    /// VISTA DE PERFIL PÚBLICO.
    /// Funciona como la "Hoja de Vida" visual de la candidata.
    /// 
    /// CARACTERÍSTICAS UX:
    /// - Header visual con degradado.
    /// - Foto de perfil circular (Custom Control).
    /// - Navegación por pestañas (Tabs): Biografía vs Galería.
    /// - Scroll infinito para ver múltiples álbumes.
    /// </summary>
    public partial class FormPerfilCandidata : Form
    {
        // Contexto de datos
        private Candidata _candidata;
        private AlbumDAO _albumDAO;
        private EstudianteService _estudianteService;

        // Paleta de diseño (Tema Pastel/Femenino)
        private Color colorPrimario = Color.FromArgb(255, 105, 180);
        private Color colorFondo = Color.FromArgb(245, 246, 250);
        private Color colorTarjeta = Color.White;
        private Color colorTexto = Color.FromArgb(45, 52, 54);

        // Contenedores dinámicos
        private Panel panelContenido;
        private Button btnTabBio;
        private Button btnTabFotos;

        /// <summary>
        /// Constructor: Recibe la candidata a mostrar.
        /// </summary>
        public FormPerfilCandidata(Candidata candidata)
        {
            InitializeComponent();
            _candidata = candidata;
            _albumDAO = new AlbumDAO();
            _estudianteService = new EstudianteService();

            // Configuración visual: Tamaño fijo tipo "Móvil/App".
            this.Size = new Size(450, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = colorFondo;
            this.Text = $"Perfil de {_candidata.Nombres}";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            InicializarUI();

            // Carga por defecto: Tab de Biografía.
            MostrarBio();
            ActualizarEstiloTabs(btnTabBio);
        }

        private void FormPerfilCandidata_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Construye toda la interfaz gráfica por código para lograr efectos
        /// que el diseñador estándar de WinForms no permite fácilmente (como degradados y shapes).
        /// </summary>
        private void InicializarUI()
        {
            // 1. HEADER CON DEGRADADO (GDI+)
            Panel panelHeader = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = colorTarjeta };
            panelHeader.Paint += (s, e) =>
            {
                // Dibujo un degradado diagonal suave.
                using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, this.Width, 140),
                    Color.FromArgb(108, 92, 231), Color.FromArgb(162, 155, 254), 45F))
                {
                    e.Graphics.FillRectangle(brush, brush.Rectangle);
                }
            };
            this.Controls.Add(panelHeader);

            // 2. FOTO DE PERFIL CIRCULAR
            PictureBox picAvatar = new CircularPictureBox
            {
                Size = new Size(130, 130),
                Location = new Point(20, 70), // Posicionamiento "Overlay" (mitad en header, mitad fuera).
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.WhiteSmoke,
                BorderColor = Color.White,
                BorderSize = 4
            };
            // Carga segura de imagen.
            if (!string.IsNullOrEmpty(_candidata.ImagenPrincipal) && File.Exists(_candidata.ImagenPrincipal))
                picAvatar.Image = Image.FromFile(_candidata.ImagenPrincipal);
            panelHeader.Controls.Add(picAvatar);

            // 3. DATOS PERSONALES
            string nombresCompletos = _candidata.Nombres ?? "";
            string apellidosCompletos = _candidata.Apellidos ?? "";

            Label lblNombre = new Label
            {
                Text = $"{nombresCompletos} {apellidosCompletos}".Trim(),
                Location = new Point(160, 150),
                AutoSize = true,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = colorTexto,
                MaximumSize = new Size(260, 0) // Wrap de texto si el nombre es muy largo.
            };
            panelHeader.Controls.Add(lblNombre);

            Label lblCarrera = new Label
            {
                Text = _candidata.Carrera?.Nombre ?? "Carrera Desconocida",
                Location = new Point(162, 185),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };
            panelHeader.Controls.Add(lblCarrera);

            // Badge de Categoría (Reina/Fotogenia)
            Label lblTipo = new Label
            {
                Text = _candidata.tipoCandidatura.ToString().ToUpper(),
                Location = new Point(340, 158),
                Size = new Size(80, 20),
                BackColor = colorPrimario,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7, FontStyle.Bold)
            };
            panelHeader.Controls.Add(lblTipo);

            // 4. SISTEMA DE PESTAÑAS (Tabs)
            TableLayoutPanel tableTabs = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White
            };
            tableTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.Controls.Add(tableTabs);

            btnTabBio = CrearBotonTab("Sobre Mí");
            btnTabFotos = CrearBotonTab("Galería");

            // Eventos de navegación interna sin recargar formulario.
            btnTabBio.Click += (s, e) => { MostrarBio(); ActualizarEstiloTabs(btnTabBio); };
            btnTabFotos.Click += (s, e) => { MostrarAlbumes(); ActualizarEstiloTabs(btnTabFotos); };

            tableTabs.Controls.Add(btnTabBio, 0, 0);
            tableTabs.Controls.Add(btnTabFotos, 1, 0);

            // 5. ÁREA DE CONTENIDO SCROLLABLE
            panelContenido = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            this.Controls.Add(panelContenido);
            panelContenido.BringToFront(); 
        }

        private Button CrearBotonTab(string texto)
        {
            return new Button
            {
                Text = texto,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        /// <summary>
        /// Feedback visual: Resalta la pestaña activa cambiando el color del texto.
        /// </summary>
        private void ActualizarEstiloTabs(Button activo)
        {
            btnTabBio.ForeColor = Color.Gray;
            btnTabFotos.ForeColor = Color.Gray;
            activo.ForeColor = colorPrimario;
        }

        // ---------------- LOGICA DE PESTAÑA: BIOGRAFÍA ------------------

        private void MostrarBio()
        {
            panelContenido.Controls.Clear();
            int yPos = 20;

            // Recupero los detalles extendidos (Habilidades, Hobbies) desde la BD.
            CatalogoDAO catalogoDAO = new CatalogoDAO();
            List<CatalogoDTO> listaCompleta = catalogoDAO.ObtenerDeCandidata(_candidata.CandidataId);

            // Clasifico los datos usando LINQ para mostrarlos ordenados.
            _candidata.Habilidades = listaCompleta
                .Where(x => x.Tipo == "HABILIDAD")
                .Select(x => x.Nombre).ToList();

            _candidata.Pasatiempos = listaCompleta
                .Where(x => x.Tipo == "PASATIEMPO")
                .Select(x => x.Nombre).ToList();

            _candidata.Aspiraciones = listaCompleta
                .Where(x => x.Tipo == "ASPIRACION")
                .Select(x => x.Nombre).ToList();

            // Renderizado condicional: Solo muestro secciones que tengan datos.
            if (_candidata.Habilidades.Count > 0)
                yPos = AgregarBloqueTags("✨ Habilidades", _candidata.Habilidades, yPos, Color.FromArgb(223, 249, 251), Color.FromArgb(19, 15, 64));

            if (_candidata.Pasatiempos.Count > 0)
                yPos = AgregarBloqueTags("🎨 Pasatiempos", _candidata.Pasatiempos, yPos, Color.FromArgb(224, 236, 255), Color.FromArgb(65, 105, 225));

            if (_candidata.Aspiraciones.Count > 0)
                yPos = AgregarBloqueTags("🎯 Aspiraciones", _candidata.Aspiraciones, yPos, Color.FromArgb(250, 227, 217), Color.FromArgb(189, 87, 87));

            // Empty State
            if (yPos == 20)
            {
                Label lblVacio = new Label
                {
                    Text = "La candidata aún no ha completado su perfil.",
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Location = new Point(20, 20),
                    Font = new Font("Segoe UI", 10, FontStyle.Italic)
                };
                panelContenido.Controls.Add(lblVacio);
            }
        }

        /// <summary>
        /// Helper para dibujar "Tags" o etiquetas de colores (estilo Web 2.0).
        /// </summary>
        private int AgregarBloqueTags(string titulo, List<string> items, int top, Color bg, Color text)
        {
            if (items == null || items.Count == 0) return top;

            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(10, top), AutoSize = true };
            panelContenido.Controls.Add(lbl);
            top += 30;

            FlowLayoutPanel flow = new FlowLayoutPanel { Location = new Point(10, top), Width = panelContenido.Width - 20, AutoSize = true, WrapContents = true };
            foreach (var item in items)
            {
                Label tag = new Label { Text = item, BackColor = bg, ForeColor = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), Padding = new Padding(5), AutoSize = true, Margin = new Padding(3) };
                flow.Controls.Add(tag);
            }
            panelContenido.Controls.Add(flow);
            return top + flow.Height + 20;
        }

        // ---------------- LOGICA DE PESTAÑA: GALERÍA ------------------

        private void MostrarAlbumes()
        {
            panelContenido.Controls.Clear();
            int yPos = 20;

            try
            {
                List<Album> listaAlbumes = _albumDAO.ObtenerPorCandidata(_candidata.CandidataId);

                if (listaAlbumes == null || listaAlbumes.Count == 0)
                {
                    Label lblVacio = new Label
                    {
                        Text = "Esta candidata aún no ha publicado álbumes.",
                        AutoSize = false,
                        Dock = DockStyle.Top,
                        Height = 100,
                        TextAlign = ContentAlignment.MiddleCenter,
                        ForeColor = Color.Gray,
                        Font = new Font("Segoe UI", 10, FontStyle.Italic)
                    };
                    panelContenido.Controls.Add(lblVacio);
                    return;
                }

                foreach (var album in listaAlbumes)
                {
                    List<Foto> fotosDelAlbum = _albumDAO.ObtenerFotosPorAlbum(album.Id);

                    // Componente complejo: Tarjeta de Álbum con scroll horizontal.
                    Panel tarjetaAlbum = CrearTarjetaAlbum(album, fotosDelAlbum);
                    tarjetaAlbum.Location = new Point(20, yPos);

                    panelContenido.Controls.Add(tarjetaAlbum);

                    // Cálculo dinámico de posición Y para el siguiente elemento (Stacking).
                    yPos += tarjetaAlbum.Height + 20;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando galería: " + ex.Message);
            }
        }

        private Panel CrearTarjetaAlbum(Album album, List<Foto> fotos)
        {
            // Contenedor principal del álbum.
            Panel panelAlbum = new Panel
            {
                Size = new Size(panelContenido.Width - 40, 240),
                Margin = new Padding(0, 0, 0, 30),
                BackColor = Color.White
            };
            panelAlbum.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, panelAlbum.ClientRectangle,
                                          Color.LightGray, ButtonBorderStyle.Solid);

            // Metadatos del álbum.
            Label lblTitulo = new Label
            {
                Text = album.Titulo.ToUpper(),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = colorTexto,
                Location = new Point(15, 10),
                AutoSize = true
            };
            panelAlbum.Controls.Add(lblTitulo);

            Label lblDesc = new Label
            {
                Text = album.Descripcion,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                Location = new Point(15, 30),
                AutoSize = true
            };
            panelAlbum.Controls.Add(lblDesc);

            // Carrusel de Fotos Horizontal (Horizontal Scroll).
            FlowLayoutPanel scrollFotos = new FlowLayoutPanel
            {
                Location = new Point(10, 60),
                Size = new Size(panelAlbum.Width - 20, 160),
                AutoScroll = true,
                WrapContents = false, // Clave para que sea horizontal infinito.
                BackColor = Color.WhiteSmoke
            };

            if (fotos != null && fotos.Count > 0)
            {
                foreach (var foto in fotos)
                {
                    PictureBox pic = new PictureBox
                    {
                        Size = new Size(140, 130),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.Black,
                        Margin = new Padding(0, 5, 15, 5),
                        Cursor = Cursors.Hand
                    };

                    if (File.Exists(foto.RutaArchivo))
                    {
                        try { pic.Image = Image.FromFile(foto.RutaArchivo); } catch { }
                    }

                    // Navegación al detalle (Zoom).
                    pic.Click += (s, e) =>
                    {
                        // Seguridad: Solo usuarios autenticados pueden ver detalles/comentar.
                        if (!Sesion.EstaLogueado())
                        {
                            MessageBox.Show("Debes iniciar sesión para ver detalles y comentar.");
                            return;
                        }

                        // Validación: Asegurar que el usuario tenga un perfil válido.
                        Estudiante estudianteLogueado = Sesion.EstudianteLogueado;

                        // Si es la primera vez en la sesión, intento recuperar el perfil de estudiante.
                        if (estudianteLogueado == null && Sesion.UsuarioActual?.Persona != null)
                        {
                            string cedula = Sesion.UsuarioActual.Persona.DNI;
                            if (!string.IsNullOrWhiteSpace(cedula))
                            {
                                estudianteLogueado = _estudianteService.ObtenerPorCedula(cedula);
                            }
                        }

                        bool isAdmin = Sesion.UsuarioActual?.Rol?.Nombre == "Administrador";

                        // Si falla la validación, decido según rol.
                        if (estudianteLogueado == null)
                        {
                            // Admin puede ver pero no comentar (modo solo lectura).
                            if (isAdmin)
                            {
                                FormDetalleFoto frmDetalleAdmin = new FormDetalleFoto(foto, fotos, null);
                                frmDetalleAdmin.ShowDialog();
                                return;
                            }
                            MessageBox.Show("Tu cuenta no tiene perfil de estudiante para acceder a este recurso.");
                            return;
                        }

                        // Estudiante normal: Acceso completo.
                        FormDetalleFoto frmDetalle = new FormDetalleFoto(foto, fotos, estudianteLogueado);
                        frmDetalle.ShowDialog();
                    };

                    scrollFotos.Controls.Add(pic);
                }
            }
            else
            {
                Label lblNoFotos = new Label
                {
                    Text = "Álbum vacío",
                    AutoSize = false,
                    Size = new Size(200, 130),
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.DarkGray
                };
                scrollFotos.Controls.Add(lblNoFotos);
            }

            panelAlbum.Controls.Add(scrollFotos);
            return panelAlbum;
        }

        // --- CUSTOM CONTROL: PictureBox Redondo ---
        // Clase interna para dibujar imágenes circulares mediante GraphicsPath.
        public class CircularPictureBox : PictureBox
        {
            public int BorderSize { get; set; } = 2;
            public Color BorderColor { get; set; } = Color.RoyalBlue;
            
            protected override void OnPaint(PaintEventArgs pe)
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
                
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(rect);
                    this.Region = new Region(path); // Recorte circular
                    
                    base.OnPaint(pe); // Dibujo la imagen recortada
                    
                    // Dibujo el borde encima
                    if (BorderSize > 0) 
                        using (Pen pen = new Pen(BorderColor, BorderSize))
                        {
                            pen.Alignment = PenAlignment.Inset; 
                            pe.Graphics.DrawEllipse(pen, rect);
                        }
                }
            }
        }
    }
}

