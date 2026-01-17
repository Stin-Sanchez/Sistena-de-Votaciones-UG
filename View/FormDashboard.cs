using SIVUG.Controllers;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SIVUG.View
{
    public partial class FormDashboard : Form
    {
        private DashboardController controller;
        private Timer timerActualizacion;
        private List<ResultadoPreliminarDTO> _resultadosCache;
        private TabControl tabResultados;

        // Controles de Layout (Nuevos para Responsive)
        private Panel panelContenidoPrincipal;
        private TableLayoutPanel layoutPrincipal;
        private TableLayoutPanel layoutStats;

        // Paneles de contenido
        private Panel panelProgresoFacultades;
        private Panel panelResultados;

        // Referencias a etiquetas para actualización
        private Label lblTotalEstudiantes, lblVotosEmitidos, lblPorcentaje, lblCandidatasActivas;

        // Botones Navegación
        private Button btnActualizar, btnEstudiantes, btnCandidatas, btnVotaciones, btnResultados;

        public FormDashboard()
        {
            InitializeComponent();
            controller = new DashboardController();
            ConfigurarFormulario();
            InicializarComponentes(); // Aquí está la magia del responsive
            CargarDatos();
            IniciarActualizacionAutomatica();
        }

        private void FormDashboard_Load(object sender, EventArgs e)
        {
        }

        private void ConfigurarFormulario()
        {
            this.Text = "SIVUG - Dashboard Principal";
            this.Size = new Size(1200, 700);
            this.MinimumSize = new Size(1024, 600); // Evitar que se haga muy pequeño
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 245);
            // Permitir redimensionar
            this.FormBorderStyle = FormBorderStyle.Sizable;
        }

        private void InicializarComponentes()
        {
            // 1. Panel Lateral (Navegación) - Se queda fijo a la izquierda
            CrearPanelNavegacion();

            // 2. Contenedor Principal (DERECHA) - Ocupa todo el resto del espacio
            panelContenidoPrincipal = new Panel
            {
                Dock = DockStyle.Fill, // Importante: Llenar el espacio restante
                BackColor = Color.FromArgb(240, 240, 245),
                Padding = new Padding(20) // Margen interno
            };
            this.Controls.Add(panelContenidoPrincipal);
            // Traer al frente para asegurar que no quede tapado
            panelContenidoPrincipal.BringToFront();

            // 3. Crear Estructura de Grilla (Layout Principal)
            // Fila 0: Tarjetas (Stats)
            // Fila 1: Contenido (Progreso y Top 3)
            layoutPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            // Definir proporciones de columnas y filas
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F)); // Columna Izquierda (Progreso) 70%
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // Columna Derecha (Top 3) 30%
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));     // Fila Arriba (Alto fijo para cards)
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));      // Fila Abajo (El resto)

            panelContenidoPrincipal.Controls.Add(layoutPrincipal);

            // 4. Panel Superior con estadísticas (Ahora dentro de la grilla)
            CrearPanelEstadisticasResponsive();

            // 5. Paneles centrales
            CrearPanelProgresoFacultadesResponsive();
            CrearPanelResultadosResponsive();
        }

        private void CrearPanelEstadisticasResponsive()
        {
            // Creamos un sub-layout para las 4 tarjetas
            layoutStats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 20) // Separación inferior
            };

            // 4 columnas iguales (25% cada una)
            for (int i = 0; i < 4; i++)
                layoutStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // Agregamos este layout a la Fila 0 del layout principal, abarcando las 2 columnas
            layoutPrincipal.Controls.Add(layoutStats, 0, 0);
            layoutPrincipal.SetColumnSpan(layoutStats, 2);

            // Card: Total Estudiantes
            var cardEstudiantes = CrearCardResponsive("Total Estudiantes", "0", Color.FromArgb(52, 152, 219));
            lblTotalEstudiantes = (Label)cardEstudiantes.Controls["lblValor"];
            layoutStats.Controls.Add(cardEstudiantes, 0, 0);

            // Card: Votos Emitidos
            var cardVotos = CrearCardResponsive("Votos Emitidos", "0", Color.FromArgb(46, 204, 113));
            lblVotosEmitidos = (Label)cardVotos.Controls["lblValor"];
            layoutStats.Controls.Add(cardVotos, 1, 0);

            // Card: Porcentaje Votación
            var cardPorcentaje = CrearCardResponsive("Porcentaje Votación", "0%", Color.FromArgb(155, 89, 182));
            lblPorcentaje = (Label)cardPorcentaje.Controls["lblValor"];
            layoutStats.Controls.Add(cardPorcentaje, 2, 0);

            // Card: Candidatas Activas
            var cardCandidatas = CrearCardResponsive("Candidatas Activas", "0", Color.FromArgb(243, 156, 18));
            lblCandidatasActivas = (Label)cardCandidatas.Controls["lblValor"];
            layoutStats.Controls.Add(cardCandidatas, 3, 0);
        }

        // Ya no necesitamos posX porque el TableLayout lo acomoda solo
        private Panel CrearCardResponsive(string titulo, string valor, Color colorFondo)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill, // Llenar la celda de la tabla
                BackColor = colorFondo,
                Margin = new Padding(5) // Espacio entre tarjetas
            };

            Label lblTitulo = new Label
            {
                Text = titulo,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(15, 15),
                AutoSize = true
            };
            card.Controls.Add(lblTitulo);

            Label lblValor = new Label
            {
                Name = "lblValor",
                Text = valor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                Location = new Point(15, 45),
                AutoSize = true
            };
            card.Controls.Add(lblValor);

            return card;
        }

        private void CrearPanelNavegacion()
        {
            // 1. Panel Lateral Base
            Panel panelNav = new Panel
            {
                Width = 250, // Un poco más ancho para los submenús
                BackColor = Color.FromArgb(44, 62, 80),
                Dock = DockStyle.Left,
                AutoScroll = true // Por si el menú es muy largo
            };
            this.Controls.Add(panelNav);

            // --- NUEVO CÓDIGO UX: PANEL INFERIOR FIJO ---
            // Yo creo este panel y lo agrego PRIMERO para que el Dock=Bottom tenga prioridad 
            // visual sobre el contenido de relleno (el menú).
            Panel panelLogout = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(34, 49, 63), // Un tono más oscuro para diferenciarlo
                Padding = new Padding(10)
            };
            panelNav.Controls.Add(panelLogout);

            // Yo diseño el botón de salir con un color rojizo (Alizarin) para indicar 
            // que es una acción de salida o destructiva, mejorando la UX.
            Button btnLogout = new Button
            {
                Text = "🚪 Cerrar Sesión", // Icono visual para claridad
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(192, 57, 43), // Rojo elegante
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += BtnLogout_Click; // Asocio mi evento
            panelLogout.Controls.Add(btnLogout);


            // 2. Logo y Títulos (Se mantienen fijos arriba)
            Panel panelLogo = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Color.Transparent };
            panelNav.Controls.Add(panelLogo);

            Label lblLogo = new Label
            {
                Text = "SIVUG",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(210, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelLogo.Controls.Add(lblLogo);

            Label lblSubtitulo = new Label
            {
                Text = "Sistema de Gestión\nde Votaciones UG",
                ForeColor = Color.FromArgb(189, 195, 199),
                Font = new Font("Segoe UI", 8F),
                Location = new Point(10, 75),
                Size = new Size(230, 35),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelLogo.Controls.Add(lblSubtitulo);

            if (Sesion.EstaLogueado())
            {
                Label lblBienvenida = new Label
                {
                    Text = $"Hola, {Sesion.UsuarioLogueado.Nombres}", // Usamos el nombre de la sesión
                    ForeColor = Color.FromArgb(46, 204, 113), // Un verde suave o el color que prefieras
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Location = new Point(10, 115), // Debajo del subtítulo
                    Size = new Size(230, 25),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                panelLogo.Controls.Add(lblBienvenida);

                Label lblRol = new Label
                {
                    Text = "• Estudiante •",
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 8F),
                    Location = new Point(10, 135),
                    Size = new Size(230, 20),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                panelLogo.Controls.Add(lblRol);
            }

            // 3. Contenedor de Menú (FlowLayout hace la magia del acordeón)
            FlowLayoutPanel flowMenu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
            panelNav.Controls.Add(flowMenu);
            // Traer al frente para que no lo tape el panelLogo si hay scroll
            flowMenu.BringToFront();
            panelLogo.SendToBack(); // Invertimos orden visual: Logo arriba (Dock Top), Flow abajo (Dock Fill)


            // --- GENERACIÓN DE BOTONES ---

            // A. Inicio (Botón simple)
            flowMenu.Controls.Add(CrearBotonMenuSimple("🏠 Inicio", (s, e) => CargarDatos()));

            // B. Estudiantes (Botón simple)
            flowMenu.Controls.Add(CrearBotonMenuSimple("👥 Estudiantes", BtnEstudiantes_Click));

            // C. CANDIDATAS (MENÚ DESPLEGABLE)
            // Aquí definimos las sub-opciones y sus acciones
            var subMenuCandidatas = new Panel(); // Placeholder variable
            subMenuCandidatas = CrearGrupoMenu("👤 Candidatas", flowMenu, new List<(string, EventHandler)>
    {
        ("📋 Registro / Admin", BtnCandidatas_Click), // Tu form antiguo
        ("🌟 Catálogo Visual", (s,e) => { new FormCatalogoCandidatas().ShowDialog(); }), // El catálogo nuevo
        ("📸 Gestión de Álbumes", BtnGestionAlbumes_Click) // Nueva función
    });
            flowMenu.Controls.Add(subMenuCandidatas);

            // D. Votaciones (Botón simple)
            flowMenu.Controls.Add(CrearBotonMenuSimple("🗳️ Votaciones", BtnVotaciones_Click));

            // E. Resultados (Botón simple)
            flowMenu.Controls.Add(CrearBotonMenuSimple("📊 Resultados", BtnResultados_Click));

            // Botón Actualizar (Fijo al final o agregado al flujo)
            flowMenu.Controls.Add(CrearBotonMenuSimple("🔄 Actualizar Datos", BtnActualizar_Click));
        }

        private void BtnGestionAlbumes_Click(object sender, EventArgs e)
        {
            // Ahora lo abrimos directo, porque tiene su propio buscador interno
            FormGestionAlbumes formGestor = new FormGestionAlbumes();
            formGestor.ShowDialog();
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            // Yo pregunto amablemente al usuario si está seguro, para evitar clics accidentales.
            DialogResult confirmacion = MessageBox.Show(
                "¿Estás seguro de que deseas cerrar tu sesión?",
                "Cerrar Sesión",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmacion == DialogResult.Yes)
            {
                // Yo uso try-catch para asegurar que la transición sea limpia
                try
                {
                    // 1. Limpio la sesión global para que nadie más pueda usarla
                    Sesion.CerrarSesion();



                    // 3. Cierro este Dashboard y reinicio la app
                    Application.Restart();

                    // Forzamos el cierre del hilo actual para evitar que siga ejecutando código
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ocurrió un error al intentar salir: " + ex.Message);
                }
            }
        }

        // 1. Crea un botón simple sin submenú
        private Button CrearBotonMenuSimple(string texto, EventHandler eventoClick)
        {
            Button btn = new Button
            {
                Text = texto,
                Size = new Size(250, 45), // Ancho igual al panel
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(44, 62, 80), // Color fondo oscuro
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
                Margin = new Padding(0) // Sin espacios entre botones
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 73, 94);

            if (eventoClick != null) btn.Click += eventoClick;

            return btn;
        }

        // 2. Crea un GRUPO (Botón Principal + Panel Oculto con Subbotones)
        private Panel CrearGrupoMenu(string tituloPrincipal, FlowLayoutPanel contenedorPadre, List<(string, EventHandler)> subOpciones)
        {
            // Panel contenedor del grupo (se ajusta automáticamente)
            Panel panelGrupo = new Panel
            {
                Size = new Size(250, 45), // Altura inicial (solo botón principal)
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };

            // Panel oculto para los sub-items
            Panel panelSubItems = new Panel
            {
                Size = new Size(250, subOpciones.Count * 40), // Altura = cant items * alto item
                Location = new Point(0, 45), // Debajo del botón principal
                BackColor = Color.FromArgb(34, 49, 63), // Un poco más oscuro
                Visible = false // Empieza oculto
            };

            // Botón Principal (El que despliega)
            Button btnPrincipal = new Button
            {
                Text = tituloPrincipal + " ▼", // Indicador visual
                Size = new Size(250, 45),
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnPrincipal.FlatAppearance.BorderSize = 0;

            // Lógica de Acordeón: Al hacer clic, mostramos/ocultamos y ajustamos altura
            btnPrincipal.Click += (s, e) =>
            {
                bool estadoActual = panelSubItems.Visible;
                panelSubItems.Visible = !estadoActual; // Invertir visibilidad

                // Ajustar altura del contenedor padre para empujar los otros botones
                panelGrupo.Height = !estadoActual ? (45 + panelSubItems.Height) : 45;
            };

            // Crear los botones hijos
            int ySub = 0;
            foreach (var (texto, evento) in subOpciones)
            {
                Button btnSub = new Button
                {
                    Text = texto,
                    Size = new Size(250, 40),
                    Location = new Point(0, ySub),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent, // Hereda el oscuro del panelSubItems
                    ForeColor = Color.Silver, // Texto un poco más gris
                    Font = new Font("Segoe UI", 10F),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(40, 0, 0, 0), // Más sangría (indentación)
                    Cursor = Cursors.Hand
                };
                btnSub.FlatAppearance.BorderSize = 0;
                btnSub.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 73, 94);
                btnSub.Click += evento;

                panelSubItems.Controls.Add(btnSub);
                ySub += 40;
            }

            panelGrupo.Controls.Add(panelSubItems);
            panelGrupo.Controls.Add(btnPrincipal);

            return panelGrupo;
        }
        private Button CrearBotonNavegacion(string texto, int posY)
        {
            Button btn = new Button
            {
                Text = texto,
                Location = new Point(0, posY),
                Size = new Size(220, 45), // Ancho completo del panel
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 73, 94);
            return btn;
        }

        private void CrearPanelProgresoFacultadesResponsive()
        {
            // Panel contenedor blanco
            Panel panelContenedor = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 10, 0) // Margen derecho para separar del otro panel
            };

            // Agregamos a la celda (1,0) del layout principal (Abajo Izquierda)
            layoutPrincipal.Controls.Add(panelContenedor, 0, 1);

            Label lblTitulo = new Label
            {
                Text = "Progreso de Votación por Facultad",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(20, 15),
                AutoSize = true
            };
            panelContenedor.Controls.Add(lblTitulo);

            panelProgresoFacultades = new Panel
            {
                Location = new Point(20, 55),
                // Usamos Anchor para que el panel interno crezca con el contenedor blanco
                Size = new Size(panelContenedor.Width - 40, panelContenedor.Height - 75),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = Color.White
            };
            panelContenedor.Controls.Add(panelProgresoFacultades);
        }

        private void CrearPanelResultadosResponsive()
        {
            // Panel contenedor blanco
            Panel panelContenedor = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(10, 0, 0, 0) // Margen izquierdo
            };

            // Agregamos a la celda (1,1) del layout principal (Abajo Derecha)
            layoutPrincipal.Controls.Add(panelContenedor, 1, 1);

            Label lblTitulo = new Label
            {
                Text = "Resultados Preliminares (Top 3)",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(15, 15),
                AutoSize = true
            };
            panelContenedor.Controls.Add(lblTitulo);

            tabResultados = new TabControl
            {
                Location = new Point(15, 45),
                Size = new Size(200, 30),
                Appearance = TabAppearance.FlatButtons,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right // Se estira
            };
            tabResultados.TabPages.Add("Reina");
            tabResultados.TabPages.Add("Fotogenia");
            tabResultados.SelectedIndexChanged += (s, e) => ActualizarListaPorTab();

            panelContenedor.Controls.Add(tabResultados);

            panelResultados = new Panel
            {
                Location = new Point(15, 85),
                Size = new Size(panelContenedor.Width - 30, panelContenedor.Height - 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = Color.White
            };
            panelContenedor.Controls.Add(panelResultados);
        }

        // --- LÓGICA DE DATOS ---

        private void CargarDatos()
        {
            // Lógica intacta
            try
            {
                DashboardDTO datos = controller.ObtenerDatosDashboard();
                if (datos != null)
                {
                    lblTotalEstudiantes.Text = datos.TotalEstudiantes.ToString("#,##0");
                    lblVotosEmitidos.Text = datos.VotosEmitidos.ToString("#,##0");
                    lblPorcentaje.Text = $"{datos.PorcentajeVotacion:F1}%";
                    lblCandidatasActivas.Text = datos.CandidatasActivas.ToString();

                    CargarProgresoFacultades(datos.ProgresoFacultades);
                    CargarTop3Resultados(datos.Top3Candidatas);
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores silencioso o log para no interrumpir UI en carga automática
                Console.WriteLine(ex.Message);
            }
        }

        private void CargarProgresoFacultades(List<Facultad> facultades)
        {
            panelProgresoFacultades.Controls.Clear();
            int posY = 0;

            // Calculamos el ancho disponible dinámicamente
            // Restamos un poco para el scrollbar y padding
            int anchoDisponible = panelProgresoFacultades.Width - 100;
            if (anchoDisponible < 200) anchoDisponible = 200; // Mínimo de seguridad

            foreach (var facultad in facultades)
            {
                Panel itemFacultad = new Panel
                {
                    Location = new Point(0, posY),
                    Size = new Size(panelProgresoFacultades.Width - 20, 35), // Ancho dinámico
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };

                Label lblNombre = new Label
                {
                    Text = facultad.Nombre,
                    Font = new Font("Segoe UI", 9F),
                    Location = new Point(0, 0),
                    Size = new Size(200, 20),
                    ForeColor = Color.FromArgb(44, 62, 80)
                };
                itemFacultad.Controls.Add(lblNombre);

                // Barra de fondo
                Panel barraFondo = new Panel
                {
                    Location = new Point(0, 22),
                    Size = new Size(anchoDisponible, 10), // Usamos el ancho calculado
                    BackColor = Color.FromArgb(230, 230, 230),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                itemFacultad.Controls.Add(barraFondo);

                // Barra de progreso (cálculo basado en el ancho disponible)
                int anchoBarra = (int)(anchoDisponible * (facultad.PorcentajeParticipacion / 100));
                Panel barraProgreso = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(anchoBarra, 10),
                    BackColor = Color.FromArgb(52, 152, 219)
                };
                barraFondo.Controls.Add(barraProgreso);

                Label lblPorcentaje = new Label
                {
                    Text = $"{facultad.VotosEmitidos}/{facultad.TotalEstudiantes} ({facultad.PorcentajeParticipacion:F1}%)",
                    Font = new Font("Segoe UI", 8F),
                    Location = new Point(anchoDisponible + 10, 20), // Posición relativa al final de la barra
                    AutoSize = true,
                    ForeColor = Color.FromArgb(127, 140, 141),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                itemFacultad.Controls.Add(lblPorcentaje);

                panelProgresoFacultades.Controls.Add(itemFacultad);
                posY += 40;
            }
        }

        private void CargarTop3Resultados(List<ResultadoPreliminarDTO> resultados)
        {
            _resultadosCache = resultados;
            ActualizarListaPorTab();
        }

        private void ActualizarListaPorTab()
        {
            if (_resultadosCache == null) return;

            panelResultados.Controls.Clear();
            int posY = 0;
            int indiceTab = tabResultados.SelectedIndex;
            string tipoFiltro = indiceTab == 0 ? "Reina" : "Fotogenia";

            var listaFiltrada = _resultadosCache
                                .Where(x => x.TipoCandidatura == tipoFiltro)
                                .OrderByDescending(x => x.Votos)
                                .Take(3)
                                .ToList();

            foreach (var candidata in listaFiltrada)
            {
                Panel itemCandidato = CrearItemCandidato(candidata, posY);
                // Ajustar ancho al padre
                itemCandidato.Width = panelResultados.Width - 25;
                itemCandidato.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                panelResultados.Controls.Add(itemCandidato);
                posY += 110;
            }

            if (listaFiltrada.Count == 0)
            {
                Label lblVacio = new Label
                {
                    Text = "No hay datos aún para " + tipoFiltro,
                    AutoSize = true,
                    Location = new Point(40, 20),
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic)
                };
                panelResultados.Controls.Add(lblVacio);
            }
        }

        private Panel CrearItemCandidato(ResultadoPreliminarDTO candidata, int posY)
        {
            // Tarjeta individual
            Panel item = new Panel
            {
                Location = new Point(0, posY),
                Size = new Size(240, 100), // Tamaño base
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            PictureBox picFoto = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(60, 80),
                BackColor = Color.FromArgb(189, 195, 199),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.None
            };

            if (!string.IsNullOrEmpty(candidata.UrlFoto) && System.IO.File.Exists(candidata.UrlFoto))
            {
                try { picFoto.Image = Image.FromFile(candidata.UrlFoto); } catch { }
            }

            item.Controls.Add(picFoto);

            Label lblNombre = new Label
            {
                Text = candidata.Nombre,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(80, 10),
                Size = new Size(150, 40),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true // Permitir que crezca si el nombre es largo
            };
            item.Controls.Add(lblNombre);

            Label lblVotos = new Label
            {
                Text = $"{candidata.Votos} votos",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(80, 55),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 152, 219)
            };
            item.Controls.Add(lblVotos);

            Label lblFacultad = new Label
            {
                Text = candidata.FacultadNombre,
                Font = new Font("Segoe UI", 8F),
                Location = new Point(80, 75),
                Size = new Size(150, 15),
                ForeColor = Color.FromArgb(127, 140, 141),
                AutoSize = true
            };
            item.Controls.Add(lblFacultad);

            return item;
        }

        private void IniciarActualizacionAutomatica()
        {
            timerActualizacion = new Timer { Interval = 30000 };
            timerActualizacion.Tick += (s, e) => CargarDatos();
            timerActualizacion.Start();
        }

        // Eventos Click (Lógica intacta)
        private void BtnActualizar_Click(object sender, EventArgs e) => CargarDatos();

        private void BtnEstudiantes_Click(object sender, EventArgs e)
        {
            FormListadoEstudiantes formEstudiantes = new FormListadoEstudiantes();
            formEstudiantes.ShowDialog();
        }

        private void BtnCandidatas_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Formulario de Candidatas", "Información");
            FormRegistroCandidatas formRegist = new FormRegistroCandidatas();
            formRegist.ShowDialog();
        }

        private void BtnVotaciones_Click(object sender, EventArgs e)
        {
            FormRegistroVotos formVotos = new FormRegistroVotos();
            formVotos.ShowDialog();
            CargarDatos();
        }

        private void BtnResultados_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Formulario de Resultados", "Información");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (timerActualizacion != null)
            {
                timerActualizacion.Stop();
                timerActualizacion.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}