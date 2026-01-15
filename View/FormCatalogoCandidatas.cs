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
    public partial class FormCatalogoCandidatas : Form
    {
        // Controlador para traer los datos
        private CandidataDAO _controller;
        private List<Candidata> _listaCompleta; // Cache local para no consultar a BD cada vez que cambias de tab

        // UI Components
        private Panel panelHeader;
        private Panel panelTabs;
        private FlowLayoutPanel flowPanelTarjetas; // Aquí se dibujan las tarjetas

        // Botones de Tab (Para cambiar estilos)
        private Button btnTabReinas;
        private Button btnTabFotogenia;
        public FormCatalogoCandidatas()
        {
            InitializeComponent();
            // Configuración básica del Form
            this.Text = "Catálogo de Candidatas SIVUG";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 246, 250); // Gris muy suave

            _controller = new CandidataDAO();
            InicializarUI();
            CargarDatos();
        }

        private void FormCatalogoCandidatas_Load(object sender, EventArgs e)
        {

        }

        private void InicializarUI()
        {
            // 1. HEADER (Título)
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

            // 2. TABS DE FILTRADO (Botones grandes como en tu dibujo)
            panelTabs = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White };
            // Línea separadora visual
            panelTabs.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, panelTabs.ClientRectangle,
                                         Color.White, 0, ButtonBorderStyle.None,
                                         Color.White, 0, ButtonBorderStyle.None,
                                         Color.White, 0, ButtonBorderStyle.None,
                                         Color.LightGray, 1, ButtonBorderStyle.Solid); // Borde inferior
            this.Controls.Add(panelTabs);

            // Botón Tab REINAS
            btnTabReinas = CrearBotonTab("👑 REINAS", 0);
            btnTabReinas.Click += (s, e) => FiltrarCandidatas("Reina");
            panelTabs.Controls.Add(btnTabReinas);

            // Botón Tab FOTOGENIA
            btnTabFotogenia = CrearBotonTab("📸 MISS FOTOGENIA", 1);
            btnTabFotogenia.Click += (s, e) => FiltrarCandidatas("Fotogenia");
            panelTabs.Controls.Add(btnTabFotogenia);


            // 3. CONTENEDOR DE TARJETAS (FlowLayoutPanel)
            // Este control acomoda las tarjetas automáticamente (responsive)
            flowPanelTarjetas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(30,10,30,10), // Margen interno
                BackColor = Color.FromArgb(245, 246, 250)
            };
            this.Controls.Add(flowPanelTarjetas);

            flowPanelTarjetas.BringToFront();
        }

        private Button CrearBotonTab(string texto, int index)
        {
            // Lógica para posicionar los botones al centro o izquierda
            int btnWidth = 250;
            int startX = 30;

            Button btn = new Button
            {
                Text = texto,
                Size = new Size(btnWidth, 58), // Casi el alto del panel
                Location = new Point(startX + (index * (btnWidth + 10)), 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = Color.White,
                ForeColor = Color.Gray
            };
            btn.FlatAppearance.BorderSize = 0;
            // Indicador visual de selección (borde inferior)
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            return btn;
        }

        // --- LÓGICA DE DATOS ---

        private void CargarDatos()
        {
            try
            {
                // Asumo que tu controller tiene este método. Si no, usa tu Service directamente.
                _listaCompleta = _controller.ObtenerActivas();

                // Carga inicial (Por defecto mostramos Reinas o Todas)
                FiltrarCandidatas("Reina");
                ResaltarTab(btnTabReinas);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando candidatas: " + ex.Message);
            }
        }

        private void FiltrarCandidatas(string tipo)
        {
            // Limpiamos el panel
            flowPanelTarjetas.Controls.Clear();
            flowPanelTarjetas.SuspendLayout(); // Congelar pintado para rendimiento

            // Filtramos la lista local
            // Ajusta la lógica de filtro según cómo guardes "TipoVoto" en tu BD (Enum o String)
            var listaFiltrada = _listaCompleta
                .Where(c => c.tipoCandidatura.ToString().Contains(tipo) || c.tipoCandidatura.ToString() == "Ambas")
                .ToList();

            foreach (var candidata in listaFiltrada)
            {
                // Por cada candidata, creamos una tarjeta visual
                Panel tarjeta = CrearTarjetaCandidata(candidata);
                flowPanelTarjetas.Controls.Add(tarjeta);
            }

            // Actualizar estilo de botones
            if (tipo == "Reina") ResaltarTab(btnTabReinas);
            else ResaltarTab(btnTabFotogenia);

            flowPanelTarjetas.ResumeLayout(); // Descongelar
        }

        private void ResaltarTab(Button btnSeleccionado)
        {
            // Resetear estilos
            btnTabReinas.ForeColor = Color.Gray;
            btnTabFotogenia.ForeColor = Color.Gray;

            // Pintar activo
            btnSeleccionado.ForeColor = Color.FromArgb(44, 62, 80); // Azul oscuro
            // Aquí podrías dibujar una línea inferior si quisieras más detalle
        }

        // --- DISEÑO DE LA TARJETA (El cuadro que dibujaste en Paint) ---

        private Panel CrearTarjetaCandidata(Candidata candidata)
        {
            // 1. TARJETA (Aumentamos altura de 420 a 450 para que quepa todo)
            Panel card = new Panel
            {
                Size = new Size(260, 450),
                BackColor = Color.White,
                Margin = new Padding(20)
            };

            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                                        Color.FromArgb(220, 220, 220), ButtonBorderStyle.Solid);

            // 2. FOTO
            PictureBox pic = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(240, 300),
                BackColor = Color.FromArgb(245, 245, 245),
                SizeMode = PictureBoxSizeMode.CenterImage // Mantenemos tu configuración
            };

            if (!string.IsNullOrEmpty(candidata.ImagenPrincipal) && System.IO.File.Exists(candidata.ImagenPrincipal))
            {
                try
                {
                    using (var imgOriginal = Image.FromFile(candidata.ImagenPrincipal))
                    {
                        pic.Image = RedimensionarImagen(imgOriginal, 240, 300);
                    }
                }
                catch { }
            }
            card.Controls.Add(pic);

            // 3. DATOS (Recalculando coordenadas Y)

            Label lblNombre = new Label
            {
                Text = $"{candidata.Nombres} {candidata.Apellidos}",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = false,
                Size = new Size(240, 45), // Alto de 2 líneas
                                          // POSICIÓN: 300 (fin foto) + 15 (espacio) = 315
                Location = new Point(10, 315),
                TextAlign = ContentAlignment.TopCenter
            };
            card.Controls.Add(lblNombre);

            // CORRECCIÓN FACULTAD:
            // El nombre termina en 315 + 45 = 360.
            // Antes la facultad estaba en 350 (Colisión). La bajamos a 365.
            Label lblFacultad = new Label
            {
                Text = candidata.Carrera?.Facultad?.Nombre ?? "Facultad Desconocida",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(240, 35), // Damos más altura por si el nombre es largo
                Location = new Point(10, 365), // Y = 365 (Debajo del nombre)
                TextAlign = ContentAlignment.TopCenter
            };
            card.Controls.Add(lblFacultad);

            // 4. BOTÓN (Bajamos también el botón)
            // Facultad termina en 365 + 35 = 400.
            // Botón en 405.
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
            btnVerInfo.Click += (s, e) => AbrirPerfilCandidata(candidata);
            card.Controls.Add(btnVerInfo);

            return card;
        }


        // Método para redimensionar con Alta Calidad (Equivalente a Java SCALE_SMOOTH)
        private Image RedimensionarImagen(Image imgOriginal, int ancho, int alto)
        {
            var radio = (double)ancho / imgOriginal.Width; // Calculamos el factor de escala
            var nuevoAlto = (int)(imgOriginal.Height * radio); // Mantenemos proporción

            // Creamos un lienzo vacío con las nuevas medidas
            var imagenFinal = new Bitmap(ancho, alto);

            using (var graphics = Graphics.FromImage(imagenFinal))
            {
                // ACTIVAMOS EL "MODO HD" (Interpolación Bicúbica)
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                // Dibujamos la imagen centrada y escalada
                graphics.DrawImage(imgOriginal, 0, 0, ancho, nuevoAlto);
            }
            return imagenFinal;
        }



        private void AbrirPerfilCandidata(Candidata candidataSeleccionada)
        {
            // Instanciamos el form que creamos en el paso anterior
            FormPerfilCandidata formPerfil = new FormPerfilCandidata(candidataSeleccionada);

            // Lo mostramos como diálogo modal (bloquea el catálogo hasta que cierres el perfil)
            // Opcional: formPerfil.Show() si quieres que se mantengan ambos abiertos
            formPerfil.ShowDialog();
        }



    }
}
