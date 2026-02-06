using SIVUG.Models;
using SIVUG.Models.DAO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIVUG.View
{
    /// <summary>
    /// Soy el catálogo visual donde los usuarios exploran las candidatas.
    /// Mi objetivo es ofrecer una experiencia similar a una "tienda online" (e-commerce)
    /// donde el usuario puede filtrar y ver detalles de cada participante.
    /// </summary>
    public partial class FormCatalogoCandidatas : Form
    {
        // DAO para acceder a los datos.
        private CandidataDAO _controller;
        
        // Client-Side Cache:
        // Guardo todas las candidatas en memoria al iniciar.
        // Esto permite cambiar entre pestañas (Reina/Fotogenia) instantáneamente sin recargar la BD.
        private List<Candidata> _listaCompleta; 

        // Componentes de UI generados por código (Layout fluido).
        private Panel panelHeader;
        private Panel panelTabs;
        private FlowLayoutPanel flowPanelTarjetas; // Contenedor responsivo tipo "Masonry".

        // Referencias a los botones de filtro para gestionar su estado visual (Activo/Inactivo).
        private Button btnTabReinas;
        private Button btnTabFotogenia;

        public FormCatalogoCandidatas()
        {
            InitializeComponent();
            
            // Configuración visual base.
            this.Text = "Catálogo de Candidatas SIVUG";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 246, 250); // Fondo gris claro moderno.

            _controller = new CandidataDAO();
            
            // Construcción del Layout.
            InicializarUI();
            
            // Carga de datos inicial.
            CargarDatos();
        }

        private void FormCatalogoCandidatas_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Construye el layout de la pantalla:
        /// 1. Header con título.
        /// 2. Barra de pestañas (Tabs) tipo Material Design.
        /// 3. Área de contenido scrollable para las tarjetas.
        /// </summary>
        private void InicializarUI()
        {
            // 1. HEADER (Identidad)
            panelHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White };
            this.Controls.Add(panelHeader);

            Label lblTitulo = new Label
            {
                Text = "Candidatas a Reina y Miss Fotogenia UG 2026",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(30, 25)
            };
            panelHeader.Controls.Add(lblTitulo);

            // 2. TABS DE NAVEGACIÓN
            panelTabs = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White };
            
            // Custom Painting: Dibujo una línea sutil inferior para separar el header del contenido.
            panelTabs.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, panelTabs.ClientRectangle,
                                         Color.White, 0, ButtonBorderStyle.None,
                                         Color.White, 0, ButtonBorderStyle.None,
                                         Color.White, 0, ButtonBorderStyle.None,
                                         Color.LightGray, 1, ButtonBorderStyle.Solid); 
            this.Controls.Add(panelTabs);

            // Creación dinámica de botones de pestaña.
            // Tab REINAS
            btnTabReinas = CrearBotonTab("👑 REINAS", 0);
            btnTabReinas.Click += (s, e) => FiltrarCandidatas("Reina");
            panelTabs.Controls.Add(btnTabReinas);

            // Tab FOTOGENIA
            btnTabFotogenia = CrearBotonTab("📸 MISS FOTOGENIA", 1);
            btnTabFotogenia.Click += (s, e) => FiltrarCandidatas("Fotogenia");
            panelTabs.Controls.Add(btnTabFotogenia);


            // 3. CONTENEDOR DE TARJETAS (Grid Fluido)
            // FlowLayoutPanel acomoda los controles automáticamente según el ancho de la ventana.
            // Es vital para que la app se vea bien en pantallas grandes y pequeñas.
            flowPanelTarjetas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(30,10,30,10), // Espaciado interno (breathability)
                BackColor = Color.FromArgb(245, 246, 250)
            };
            this.Controls.Add(flowPanelTarjetas);

            flowPanelTarjetas.BringToFront();
        }

        // Factory Method para crear botones de pestaña consistentes.
        private Button CrearBotonTab(string texto, int index)
        {
            int btnWidth = 250;
            int startX = 30;

            Button btn = new Button
            {
                Text = texto,
                Size = new Size(btnWidth, 58), // Ligeramente menor que el panel (60) para el borde.
                Location = new Point(startX + (index * (btnWidth + 10)), 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = Color.White,
                ForeColor = Color.Gray
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240); // Feedback visual suave.
            return btn;
        }

        // --- LÓGICA DE DATOS ---

        private void CargarDatos()
        {
            try
            {
                // Estrategia de carga: Traigo TODAS las activas de una sola vez.
                // Es más eficiente que llamar a la BD cada vez que hago clic en una pestaña.
                _listaCompleta = _controller.ObtenerActivas();

                // Inicio mostrando la categoría principal.
                FiltrarCandidatas("Reina");
                ResaltarTab(btnTabReinas);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando candidatas: " + ex.Message);
            }
        }

        /// <summary>
        /// Filtra la lista en memoria y regenera las tarjetas visuales.
        /// </summary>
        private void FiltrarCandidatas(string tipo)
        {
            // Limpieza del panel anterior.
            flowPanelTarjetas.Controls.Clear();
            
            // Optimización de renderizado: SuspendLayout evita que la pantalla parpadee
            // mientras agrego múltiples controles uno por uno.
            flowPanelTarjetas.SuspendLayout(); 

            // Lógica de filtrado LINQ.
            // Considero el caso especial "Ambas" para candidatas que participan en ambas categorías.
            var listaFiltrada = _listaCompleta
                .Where(c => c.tipoCandidatura.ToString().Contains(tipo) || c.tipoCandidatura.ToString() == "Ambas")
                .ToList();

            foreach (var candidata in listaFiltrada)
            {
                // Por cada dato, instancio un componente visual (Tarjeta).
                Panel tarjeta = CrearTarjetaCandidata(candidata);
                flowPanelTarjetas.Controls.Add(tarjeta);
            }

            // Feedback visual: Actualizo qué pestaña está activa.
            if (tipo == "Reina") ResaltarTab(btnTabReinas);
            else ResaltarTab(btnTabFotogenia);

            // Renderizo todo de golpe.
            flowPanelTarjetas.ResumeLayout(); 
        }

        private void ResaltarTab(Button btnSeleccionado)
        {
            // Reset visual: Todos grises.
            btnTabReinas.ForeColor = Color.Gray;
            btnTabFotogenia.ForeColor = Color.Gray;

            // Activo: Azul oscuro para indicar selección.
            btnSeleccionado.ForeColor = Color.FromArgb(44, 62, 80); 
        }

        // --- RENDERIZADO DE COMPONENTES (UI CARD) ---

        /// <summary>
        /// Crea una "Tarjeta de Producto" visual para una candidata.
        /// Incluye Foto, Nombre, Facultad y Botón de acción.
        /// </summary>
        private Panel CrearTarjetaCandidata(Candidata candidata)
        {
            // 1. Contenedor Base (Tarjeta)
            Panel card = new Panel
            {
                Size = new Size(260, 450), // Dimensiones fijas para consistencia en la grilla.
                BackColor = Color.White,
                Margin = new Padding(20) // Espacio entre tarjetas.
            };

            // Borde suave (Custom Painting)
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                                        Color.FromArgb(220, 220, 220), ButtonBorderStyle.Solid);

            // 2. Imagen Principal
            PictureBox pic = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(240, 300),
                BackColor = Color.FromArgb(245, 245, 245), // Placeholder gris mientras carga.
                SizeMode = PictureBoxSizeMode.CenterImage 
            };

            // Carga de imagen defensiva: Validamos que el archivo exista antes de intentar cargarlo
            // para evitar excepciones fatales (Crash) por archivos no encontrados.
            if (!string.IsNullOrEmpty(candidata.ImagenPrincipal) && System.IO.File.Exists(candidata.ImagenPrincipal))
            {
                try
                {
                    using (var imgOriginal = Image.FromFile(candidata.ImagenPrincipal))
                    {
                        // Procesamiento de imagen: Escalado de alta calidad.
                        pic.Image = RedimensionarImagen(imgOriginal, 240, 300);
                    }
                }
                catch { /* Fallback silencioso: se queda el color de fondo */ }
            }
            card.Controls.Add(pic);

            // 3. Datos de Texto
            // Nombre (Truncado si es muy largo visualmente)
            Label lblNombre = new Label
            {
                Text = $"{candidata.Nombres} {candidata.Apellidos}",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = false,
                Size = new Size(240, 45), // Altura para soportar 2 líneas de texto.
                Location = new Point(10, 315),
                TextAlign = ContentAlignment.TopCenter
            };
            card.Controls.Add(lblNombre);

            // Facultad (Subtítulo)
            Label lblFacultad = new Label
            {
                // Null Coalescing (??) para mostrar un fallback si la facultad es nula.
                Text = candidata.Carrera?.Facultad?.Nombre ?? "Facultad Desconocida",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(240, 35), 
                Location = new Point(10, 365), 
                TextAlign = ContentAlignment.TopCenter
            };
            card.Controls.Add(lblFacultad);

            // 4. Botón de Acción ("Call to Action")
            Button btnVerInfo = new Button
            {
                Text = "VER PERFIL",
                Size = new Size(220, 35),
                Location = new Point(20, 405),
                BackColor = Color.Black,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnVerInfo.FlatAppearance.BorderSize = 0;
            // Evento Click: Navega al detalle pasándole la entidad completa.
            btnVerInfo.Click += (s, e) => AbrirPerfilCandidata(candidata);
            card.Controls.Add(btnVerInfo);

            return card;
        }


        /// <summary>
        /// Algoritmo de redimensionado de imágenes de Alta Calidad.
        /// Evita que las fotos se vean pixeladas (Artefactos) al reducirlas para la tarjeta.
        /// </summary>
        private Image RedimensionarImagen(Image imgOriginal, int ancho, int alto)
        {
            var radio = (double)ancho / imgOriginal.Width; 
            var nuevoAlto = (int)(imgOriginal.Height * radio); 

            var imagenFinal = new Bitmap(ancho, alto);

            using (var graphics = Graphics.FromImage(imagenFinal))
            {
                // Configuración crítica: Activamos interpolación bicúbica para suavidad.
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                graphics.DrawImage(imgOriginal, 0, 0, ancho, nuevoAlto);
            }
            return imagenFinal;
        }

        private void AbrirPerfilCandidata(Candidata candidataSeleccionada)
        {
            // Patrón Master-Detail: Instancio el detalle y lo muestro como modal.
            FormPerfilCandidata formPerfil = new FormPerfilCandidata(candidataSeleccionada);
            formPerfil.ShowDialog();
        }
    }
}
