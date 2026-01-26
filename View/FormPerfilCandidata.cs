using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
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
    public partial class FormPerfilCandidata : Form
    {
        private Candidata _candidata;
        private AlbumDAO _albumDAO;

        // Colores
        private Color colorPrimario = Color.FromArgb(255, 105, 180);
        private Color colorFondo = Color.FromArgb(245, 246, 250);
        private Color colorTarjeta = Color.White;
        private Color colorTexto = Color.FromArgb(45, 52, 54);

        // Controles
        private Panel panelContenido;
        private Button btnTabBio;
        private Button btnTabFotos;

        public FormPerfilCandidata(Candidata candidata)
        {

            InitializeComponent();
            _candidata = candidata;
            _albumDAO = new AlbumDAO();

            // Configuración Ventana
            this.Size = new Size(450, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = colorFondo;
            this.Text = $"Perfil de {_candidata.Nombres}";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            InicializarUI();

            // Carga inicial
            MostrarBio();
            ActualizarEstiloTabs(btnTabBio);
        }

        private void FormPerfilCandidata_Load(object sender, EventArgs e)
        {

        }

        private void InicializarUI()
        {
            // 1. HEADER CON DEGRADADO
            Panel panelHeader = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = colorTarjeta };
            panelHeader.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, this.Width, 140),
                    Color.FromArgb(108, 92, 231), Color.FromArgb(162, 155, 254), 45F))
                {
                    e.Graphics.FillRectangle(brush, brush.Rectangle);
                }
            };
            this.Controls.Add(panelHeader);

            // 2. FOTO DE PERFIL
            PictureBox picAvatar = new CircularPictureBox
            {
                Size = new Size(130, 130),
                Location = new Point(20, 70), // Superpuesto
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.WhiteSmoke,
                BorderColor = Color.White,
                BorderSize = 4
            };
            if (!string.IsNullOrEmpty(_candidata.ImagenPrincipal) && File.Exists(_candidata.ImagenPrincipal))
                picAvatar.Image = Image.FromFile(_candidata.ImagenPrincipal);
            panelHeader.Controls.Add(picAvatar);

            // 3. DATOS (Muestra Nombres y Apellidos COMPLETOS)

            // Usamos el operador ?? para decir: "Si es null, usa cadena vacía"
            string nombresCompletos = _candidata.Nombres ?? "";
            string apellidosCompletos = _candidata.Apellidos ?? "";

            Label lblNombre = new Label
            {
                Text = $"{nombresCompletos} {apellidosCompletos}".Trim(),
                Location = new Point(160, 150),
                AutoSize = true,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = colorTexto,
                MaximumSize = new Size(260, 0) // Importante: Limita el ancho para que si es muy largo baje de línea
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

            // Etiqueta Tipo
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

            // 4. SISTEMA DE TABS (TableLayout para que no fallen los clics)
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

            // Eventos Click explícitos
            btnTabBio.Click += (s, e) => { MostrarBio(); ActualizarEstiloTabs(btnTabBio); };
            btnTabFotos.Click += (s, e) => { MostrarAlbumes(); ActualizarEstiloTabs(btnTabFotos); };

            tableTabs.Controls.Add(btnTabBio, 0, 0);
            tableTabs.Controls.Add(btnTabFotos, 1, 0);

            // 5. CONTENIDO
            panelContenido = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            this.Controls.Add(panelContenido);
            panelContenido.BringToFront(); // Asegurar que se vea
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

        private void ActualizarEstiloTabs(Button activo)
        {
            // Reset visual
            btnTabBio.ForeColor = Color.Gray;
            btnTabFotos.ForeColor = Color.Gray;

            // Activar seleccionado
            activo.ForeColor = colorPrimario;
        }

        // ---------------- LOGICA DE DATOS ------------------

        private void MostrarBio()
        {
            panelContenido.Controls.Clear();
            int yPos = 20;

            // 1. CARGA DE DATOS USANDO  DAO EXISTENTE
            CatalogoDAO catalogoDAO = new CatalogoDAO();

            // Llamamos al método que devuelve List<CatalogoDTO>
            List<CatalogoDTO> listaCompleta = catalogoDAO.ObtenerDeCandidata(_candidata.CandidataId);

          
            // Filtramos la lista única para llenar las propiedades de la candidata

            _candidata.Habilidades = listaCompleta
                .Where(x => x.Tipo == "HABILIDAD") // Filtramos por el campo 'Tipo'
                .Select(x => x.Nombre)             // Solo tomamos el texto
                .ToList();

            _candidata.Pasatiempos = listaCompleta
                .Where(x => x.Tipo == "PASATIEMPO")
                .Select(x => x.Nombre)
                .ToList();

            _candidata.Aspiraciones = listaCompleta
                .Where(x => x.Tipo == "ASPIRACION")
                .Select(x => x.Nombre)
                .ToList();

            // 3. RENDERIZADO VISUAL
            if (_candidata.Habilidades.Count > 0)
                yPos = AgregarBloqueTags("✨ Habilidades", _candidata.Habilidades, yPos, Color.FromArgb(223, 249, 251), Color.FromArgb(19, 15, 64));

            if (_candidata.Pasatiempos.Count > 0)
                yPos = AgregarBloqueTags("🎨 Pasatiempos", _candidata.Pasatiempos, yPos, Color.FromArgb(224, 236, 255), Color.FromArgb(65, 105, 225));

            if (_candidata.Aspiraciones.Count > 0)
                yPos = AgregarBloqueTags("🎯 Aspiraciones", _candidata.Aspiraciones, yPos, Color.FromArgb(250, 227, 217), Color.FromArgb(189, 87, 87));

            // Mensaje si no hay nada
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

        private void MostrarAlbumes()
        {
            panelContenido.Controls.Clear();

            // VARIABLE CLAVE: Controla la posición vertical
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

                    Panel tarjetaAlbum = CrearTarjetaAlbum(album, fotosDelAlbum);

                    // --- CORRECCIÓN: ASIGNAR POSICIÓN MANUALMENTE ---
                    tarjetaAlbum.Location = new Point(20, yPos);

                    panelContenido.Controls.Add(tarjetaAlbum);

                    // Aumentamos yPos para que el siguiente álbum se dibuje más abajo
                    // Altura de tarjeta (240) + Espacio separación (20)
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
            // A. Contenedor del Álbum
            Panel panelAlbum = new Panel
            {
                Size = new Size(panelContenido.Width - 40, 240), // Alto fijo
                Margin = new Padding(0, 0, 0, 30), // Espacio abajo entre álbumes
                BackColor = Color.White
            };
            // Borde suave
            panelAlbum.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, panelAlbum.ClientRectangle,
                                          Color.LightGray, ButtonBorderStyle.Solid);

            // B. Título y Descripción
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

            // C. Contenedor de Fotos (Horizontal Scroll)
            FlowLayoutPanel scrollFotos = new FlowLayoutPanel
            {
                Location = new Point(10, 60),
                Size = new Size(panelAlbum.Width - 20, 160),
                AutoScroll = true,       // Permite scroll si hay muchas fotos
                WrapContents = false,    // IMPORTANTE: Hace que sea una fila horizontal infinita
                BackColor = Color.WhiteSmoke
            };

            if (fotos != null && fotos.Count > 0)
            {
                foreach (var foto in fotos)
                {
                    PictureBox pic = new PictureBox
                    {
                        Size = new Size(140, 130), // Tamaño de la miniatura
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.Black,
                        Margin = new Padding(0, 5, 15, 5), // Separación entre fotos
                        Cursor = Cursors.Hand
                    };

                    // Carga segura
                    if (File.Exists(foto.RutaArchivo))
                    {
                        try { pic.Image = Image.FromFile(foto.RutaArchivo); } catch { }
                    }

                    pic.Click += (s, e) => {

                        // Pasamos:
                        // 1. La foto específica a la que se dio click (foto)
                        // 2. La lista COMPLETA de fotos de ese álbum (fotos) -> 
                        // 3. El estudiante que esta en la sesion

                        if (Sesion.UsuarioLogueado == null)
                        {
                            MessageBox.Show("Debes iniciar sesión para ver detalles y comentar.");
                            return; // O abrir el FormLogin
                        }
                        FormDetalleFoto frmDetalle = new FormDetalleFoto(foto, fotos, Sesion.UsuarioLogueado);

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

        // Clase auxiliar PictureBox Redondo
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
                    this.Region = new Region(path);
                    base.OnPaint(pe);
                    if (BorderSize > 0) using (Pen pen = new Pen(BorderColor, BorderSize))
                        {
                            pen.Alignment = PenAlignment.Inset; pe.Graphics.DrawEllipse(pen, rect);
                        }
                }
            }


        }
    }
}


    

