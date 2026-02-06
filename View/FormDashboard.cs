using SIVUG.Controllers;
using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SIVUG.View
{
    /// <summary>
    /// Dashboard Principal del sistema con Control de Acceso basado en Roles (RBAC).
    /// Responsabilidades:
    /// - Centralizar la navegación del sistema.
    /// - Mostrar métricas clave en tiempo real (KPIs).
    /// - Adaptar dinámicamente el menú según los permisos del usuario logueado.
    /// </summary>
    public partial class FormDashboard : Form
    {
        // El controlador maneja la lógica de negocio para obtener las estadísticas.
        // Desacoplo la vista del modelo de datos puro.
        private DashboardController controller;
        
        // Uso un Timer para refrescar los datos automáticamente cada cierto tiempo
        // y dar una sensación de "tiempo real" en las votaciones.
        private Timer timerActualizacion;
        
        // Almaceno en caché local los resultados para permitir filtrado rápido (Reina/Fotogenia)
        // sin volver a consultar la base de datos innecesariamente.
        private List<ResultadoPreliminarDTO> _resultadosCache;
        private TabControl tabResultados;

        // Contenedores principales para el diseño fluido (Layout).
        private Panel panelContenidoPrincipal;
        private TableLayoutPanel layoutPrincipal;
        private TableLayoutPanel layoutStats;

        // Paneles específicos para gráficos y listas.
        private Panel panelProgresoFacultades;
        private Panel panelResultados;

        // Referencias a etiquetas para actualización eficiente de la UI.
        private Label lblTotalEstudiantes, lblVotosEmitidos, lblPorcentaje, lblCandidatasActivas;

        // DAO específico para validaciones de rol de candidata.
        private CandidataDAO candidataDAO;

        /// <summary>
        /// Constructor: Punto de entrada. Inicializo componentes y disparo la primera carga de datos.
        /// </summary>
        public FormDashboard()
        {
            InitializeComponent();
            controller = new DashboardController();
            candidataDAO = new CandidataDAO();
            
            ConfigurarFormulario();
            
            // Construyo la UI dinámicamente.
            InicializarComponentes();
            
            // Cargo los datos iniciales.
            CargarDatos();
            
            // Arranco el ciclo de refresco automático.
            IniciarActualizacionAutomatica();
        }

        private void FormDashboard_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Configuro las propiedades base de la ventana para asegurar una experiencia consistente.
        /// </summary>
        private void ConfigurarFormulario()
        {
            // Muestro en el título quién está conectado y con qué rol, vital para seguridad y contexto.
            this.Text = $"SIVUG - Dashboard | {Sesion.NombreCompleto} ({Sesion.NombreRol})";
            this.Size = new Size(1200, 700);
            this.MinimumSize = new Size(1024, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 245);
            this.FormBorderStyle = FormBorderStyle.Sizable;
        }

        /// <summary>
        /// Orquestador de la creación de la interfaz.
        /// Divide la pantalla en: Navegación Lateral (Izquierda) y Contenido (Derecha).
        /// </summary>
        private void InicializarComponentes()
        {
            // 1. Panel Lateral: Aquí es donde aplico la seguridad RBAC para mostrar/ocultar opciones.
            CrearPanelNavegacion();

            // 2. Contenedor Principal: Donde se renderiza el contenido seleccionado.
            panelContenidoPrincipal = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 240, 245),
                Padding = new Padding(20)
            };
            this.Controls.Add(panelContenidoPrincipal);
            panelContenidoPrincipal.BringToFront();

            // 3. Estructura de Grilla (Grid System):
            // Fila 0: Tarjetas de estadísticas (KPIs).
            // Fila 1: Gráficos de progreso y Tablas de resultados.
            layoutPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent                                                                                                                                                                                                                                                                                           
            };
            
            // Distribución del espacio: 70% para progreso, 30% para top resultados.
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            
            // Fila 0 altura fija para KPIs, Fila 1 ocupa el resto.
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            panelContenidoPrincipal.Controls.Add(layoutPrincipal);

            // 4. Generación de los paneles de contenido.
            CrearPanelEstadisticasResponsive();
            CrearPanelProgresoFacultadesResponsive();
            CrearPanelResultadosResponsive();
        }

        /// <summary>
        /// Crea las tarjetas superiores (KPIs) con diseño responsivo.
        /// </summary>
        private void CrearPanelEstadisticasResponsive()
        {
            layoutStats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 20)
            };

            // Distribuyo uniformemente las 4 tarjetas (25% cada una).
            for (int i = 0; i < 4; i++)
                layoutStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            layoutPrincipal.Controls.Add(layoutStats, 0, 0);
            // Hago que las estadísticas ocupen todo el ancho (span de 2 columnas).
            layoutPrincipal.SetColumnSpan(layoutStats, 2);

            // Creo cada tarjeta con su color distintivo para facilitar la lectura rápida.
            var cardEstudiantes = CrearCardResponsive("Total Estudiantes", "0", Color.FromArgb(52, 152, 219));
            lblTotalEstudiantes = (Label)cardEstudiantes.Controls["lblValor"];
            layoutStats.Controls.Add(cardEstudiantes, 0, 0);

            var cardVotos = CrearCardResponsive("Votos Emitidos", "0", Color.FromArgb(46, 204, 113));
            lblVotosEmitidos = (Label)cardVotos.Controls["lblValor"];
            layoutStats.Controls.Add(cardVotos, 1, 0);

            var cardPorcentaje = CrearCardResponsive("Porcentaje Votación", "0%", Color.FromArgb(155, 89, 182));
            lblPorcentaje = (Label)cardPorcentaje.Controls["lblValor"];
            layoutStats.Controls.Add(cardPorcentaje, 2, 0);

            var cardCandidatas = CrearCardResponsive("Candidatas Activas", "0", Color.FromArgb(243, 156, 18));
            lblCandidatasActivas = (Label)cardCandidatas.Controls["lblValor"];
            layoutStats.Controls.Add(cardCandidatas, 3, 0);
        }

        // Helper para estandarizar el diseño de las tarjetas (DRY - Don't Repeat Yourself).
        private Panel CrearCardResponsive(string titulo, string valor, Color colorFondo)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = colorFondo,
                Margin = new Padding(5)
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
                Name = "lblValor", // Importante para recuperar la referencia después
                Text = valor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                Location = new Point(15, 45),
                AutoSize = true
            };
            card.Controls.Add(lblValor);

            return card;
        }

        /// <summary>
        /// Construye el menú lateral aplicando lógica de seguridad RBAC.
        /// </summary>
        private void CrearPanelNavegacion()
        {
            // Panel Lateral Base
            Panel panelNav = new Panel
            {
                Width = 250,
                BackColor = Color.FromArgb(44, 62, 80),
                Dock = DockStyle.Left,
                AutoScroll = true
            };
            this.Controls.Add(panelNav);

            // ========== PANEL LOGOUT (FIJO AL FONDO) ==========
            // Separo el logout visualmente para que siempre esté accesible.
            Panel panelLogout = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(34, 49, 63),
                Padding = new Padding(10)
            };
            panelNav.Controls.Add(panelLogout);

            Button btnLogout = new Button
            {
                Text = "   Cerrar Sesión",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Image=Properties.Resources.icons8_cierre_de_sesión_redondeado_19;
            btnLogout.ImageAlign = ContentAlignment.MiddleLeft;
            btnLogout.TextAlign = ContentAlignment.MiddleLeft;
            btnLogout.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnLogout.Click += BtnLogout_Click;
            panelLogout.Controls.Add(btnLogout);

            // ========== LOGO Y TÍTULOS ==========
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

            // Muestro saludo personalizado si hay sesión activa (confirmación visual de login).
            if (Sesion.EstaLogueado())
            {
                Label lblBienvenida = new Label
                {
                    Text = $"Hola, {Sesion.NombreCompleto}",
                    ForeColor = Color.FromArgb(46, 204, 113),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Location = new Point(10, 115),
                    Size = new Size(230, 25),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                panelLogo.Controls.Add(lblBienvenida);
            }

            // ========== MENÚ DINÁMICO (Flow) ==========
            FlowLayoutPanel flowMenu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
            panelNav.Controls.Add(flowMenu);
            flowMenu.BringToFront();
            panelLogo.SendToBack();

            // Botón Home (Común para todos)
            flowMenu.Controls.Add(CrearBotonMenuSimple("Inicio", Properties.Resources.icons8_casa_20, (s, e) => CargarDatos()));


            // ⭐ LÓGICA CORE DE SEGURIDAD (RBAC)
            // Aquí decido qué botones agregar al menú basándome en el rol de la sesión.
            string rolUsuario = Sesion.NombreRol;
            
            // Verificación especial: ¿Es una estudiante que además es candidata?
            bool esCandidata = EsCandidataLogueada();

            System.Diagnostics.Debug.WriteLine($"[RBAC] Rol: {rolUsuario}, EsCandidata: {esCandidata}");

            switch (rolUsuario)
            {
                case "Administrador":
                    // El Admin ve todo: Gestión de usuarios, candidatas, configuración de votaciones y resultados.
                    flowMenu.Controls.Add(CrearBotonMenuSimple("Estudiantes", Properties.Resources.icons8_gorro_de_graduación_20,
                        BtnEstudiantes_Click));
                    
                    // Agrupo las opciones de candidatas para no saturar el menú.
                    var opcionesAdminCandidatas = new List<(string, EventHandler)>
                    {
                        ("📋 Registro / Admin", BtnCandidatas_Click),
                        ("🌟 Catálogo Visual", (s, e) => { new FormCatalogoCandidatas().ShowDialog(); }),
                        ("📸 Gestión de Álbumes", BtnGestionAlbumes_Click)
                    };
                    
                    flowMenu.Controls.Add(CrearGrupoMenu("Candidatas", flowMenu, opcionesAdminCandidatas));
                    
                    flowMenu.Controls.Add(CrearBotonMenuSimple("Votaciones", Properties.Resources.icons8_elecciones_20, BtnVotaciones_Click));
                    flowMenu.Controls.Add(CrearBotonMenuSimple("Resultados", Properties.Resources.icons8_gráfico_combinado_20, BtnResultados_Click));
                    break;

                case "Estudiante":
                    // El Estudiante tiene acceso restringido: Solo ver catálogo y votar.
                    var opcionesEstudiante = new List<(string, EventHandler)>
                    {
                        ("🌟 Catálogo Visual", (s, e) => { new FormCatalogoCandidatas().ShowDialog(); })
                    };

                    // Si además es candidata, le doy acceso a gestionar SU PROPIO álbum.
                    // Esto es un permiso granular adicional sobre el rol base.
                    if (esCandidata)
                    {
                        opcionesEstudiante.Add(("📸 Mis Álbumes", BtnGestionAlbumes_Click));
                    }

                    flowMenu.Controls.Add(CrearGrupoMenu("👤 Candidatas", flowMenu, opcionesEstudiante));
                    flowMenu.Controls.Add(CrearBotonMenuSimple("Votaciones", Properties.Resources.icons8_elecciones_20, BtnVotaciones_Click));
                    break;

                default:
                    // Fallback de seguridad: Rol desconocido solo ve inicio básico.
                    System.Diagnostics.Debug.WriteLine($"[RBAC] Rol desconocido: {rolUsuario}");
                    break;
            }

            // Botón Actualizar (disponible para todos para forzar refresco manual).
            flowMenu.Controls.Add(CrearBotonMenuSimple("Actualizar Datos", Properties.Resources.icons8_actualizar_20, BtnActualizar_Click));
        }

        // Factory Method para crear botones de menú estandarizados.
        private Button CrearBotonMenuSimple(string texto, Image icon, EventHandler eventoClick)
        {
            Button btn = new Button
            {
                Text = "  " + texto,
                Size = new Size(250, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };

            if (icon != null)
            {
                btn.Image = icon;
                btn.ImageAlign = ContentAlignment.MiddleLeft;
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.TextImageRelation = TextImageRelation.ImageBeforeText;
                btn.Padding = new Padding(15, 0, 0, 0);
            }

            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 73, 94); // Efecto visual al pasar el mouse

            if (eventoClick != null) btn.Click += eventoClick;

            return btn;
        }

        // Crea un menú desplegable (Acordeón) para agrupar opciones relacionadas.
        private Panel CrearGrupoMenu(string tituloPrincipal, FlowLayoutPanel contenedorPadre, List<(string, EventHandler)> subOpciones)
        {
            Panel panelGrupo = new Panel
            {
                Size = new Size(250, 45), // Tamaño inicial colapsado
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };

            // Contenedor de sub-items (oculto por defecto).
            Panel panelSubItems = new Panel
            {
                Size = new Size(250, subOpciones.Count * 40),
                Location = new Point(0, 45),
                BackColor = Color.FromArgb(34, 49, 63), // Color un poco más oscuro para jerarquía
                Visible = false
            };

            Button btnPrincipal = new Button
            {
                Text ="  "+tituloPrincipal + " ▼",
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
            btnPrincipal.Image = Properties.Resources.icons8_estrella_20__1_;
            btnPrincipal.ImageAlign = ContentAlignment.MiddleLeft;
            btnPrincipal.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnPrincipal.TextAlign = ContentAlignment.MiddleLeft;
            btnPrincipal.Padding = new Padding(15, 0, 0, 0);

            // Lógica de Toggle: Expande o colapsa el panel ajustando su altura.
            btnPrincipal.Click += (s, e) =>
            {
                bool estadoActual = panelSubItems.Visible;
                panelSubItems.Visible = !estadoActual;
                panelGrupo.Height = !estadoActual ? (45 + panelSubItems.Height) : 45;
            };

            // Generación dinámica de sub-botones.
            int ySub = 0;
            foreach (var (texto, evento) in subOpciones)
            {
                Button btnSub = new Button
                {
                    Text = texto,
                    Size = new Size(250, 40),
                    Location = new Point(0, ySub),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.Silver, // Color texto secundario
                    Font = new Font("Segoe UI", 10F),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(40, 0, 0, 0), // Indentación para jerarquía visual
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

        private void CrearPanelProgresoFacultadesResponsive()
        {
            Panel panelContenedor = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 10, 0)
            };

            layoutPrincipal.Controls.Add(panelContenedor, 0, 1); // Columna izquierda

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
                Size = new Size(panelContenedor.Width - 40, panelContenedor.Height - 75),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = Color.White
            };
            panelContenedor.Controls.Add(panelProgresoFacultades);
        }

        private void CrearPanelResultadosResponsive()
        {
            Panel panelContenedor = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(10, 0, 0, 0)
            };

            layoutPrincipal.Controls.Add(panelContenedor, 1, 1); // Columna derecha

            Label lblTitulo = new Label
            {
                Text = "Resultados Preliminares (Top 3)",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(15, 15),
                AutoSize = true
            };
            panelContenedor.Controls.Add(lblTitulo);

            // TabControl para alternar entre tipos de candidatura sin recargar todo.
            tabResultados = new TabControl
            {
                Location = new Point(15, 45),
                Size = new Size(200, 30),
                Appearance = TabAppearance.FlatButtons,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
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

        /// <summary>
        /// Solicita los datos frescos al controlador y actualiza toda la UI.
        /// </summary>
        private void CargarDatos()
        {
            try
            {
                // El DTO encapsula toda la data necesaria para una sola ida y vuelta al servidor (Performance).
                DashboardDTO datos = controller.ObtenerDatosDashboard();
                if (datos != null)
                {
                    // Actualizo KPIs
                    lblTotalEstudiantes.Text = datos.TotalEstudiantes.ToString("#,##0");
                    lblVotosEmitidos.Text = datos.VotosEmitidos.ToString("#,##0");
                    lblPorcentaje.Text = $"{datos.PorcentajeVotacion:F1}%";
                    lblCandidatasActivas.Text = datos.CandidatasActivas.ToString();

                    // Renderizo gráficos
                    CargarProgresoFacultades(datos.ProgresoFacultades);
                    
                    // Guardo caché y muestro top
                    CargarTop3Resultados(datos.Top3Candidatas);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Logging básico
            }
        }

        /// <summary>
        /// Genera barras de progreso visuales para cada facultad.
        /// Ilustra la participación de mercado de votos.
        /// </summary>
        private void CargarProgresoFacultades(List<Facultad> facultades)
        {
            panelProgresoFacultades.Controls.Clear();
            int posY = 0;
            int anchoDisponible = panelProgresoFacultades.Width - 100;
            if (anchoDisponible < 200) anchoDisponible = 200;

            foreach (var facultad in facultades)
            {
                Panel itemFacultad = new Panel
                {
                    Location = new Point(0, posY),
                    Size = new Size(panelProgresoFacultades.Width - 20, 35),
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

                // Barra de fondo (gris)
                Panel barraFondo = new Panel
                {
                    Location = new Point(0, 22),
                    Size = new Size(anchoDisponible, 10),
                    BackColor = Color.FromArgb(230, 230, 230),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                itemFacultad.Controls.Add(barraFondo);

                // Barra de progreso (azul) calculada porcentualmente
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
                    Location = new Point(anchoDisponible + 10, 20),
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
            _resultadosCache = resultados; // Actualizo la caché
            ActualizarListaPorTab();       // Refresco la vista actual
        }

        /// <summary>
        /// Filtra la lista en caché según el Tab seleccionado (Reina vs Fotogenia).
        /// </summary>
        private void ActualizarListaPorTab()
        {
            if (_resultadosCache == null) return;

            panelResultados.Controls.Clear();
            int posY = 0;
            int indiceTab = tabResultados.SelectedIndex;
            string tipoFiltro = indiceTab == 0 ? "Reina" : "Fotogenia";

            // Lógica LINQ en memoria: Rápida y eficiente.
            var listaFiltrada = _resultadosCache
                                .Where(x => x.TipoCandidatura == tipoFiltro)
                                .OrderByDescending(x => x.Votos)
                                .Take(3) // Solo muestro el podio
                                .ToList();

            foreach (var candidata in listaFiltrada)
            {
                Panel itemCandidato = CrearItemCandidato(candidata, posY);
                itemCandidato.Width = panelResultados.Width - 25;
                itemCandidato.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                panelResultados.Controls.Add(itemCandidato);
                posY += 110;
            }

            // Empty state UX
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
            Panel item = new Panel
            {
                Location = new Point(0, posY),
                Size = new Size(240, 100),
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

            // Carga defensiva de imagen
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
                AutoSize = true
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
            // Polling cada 30 segundos.
            timerActualizacion = new Timer { Interval = 30000 };
            timerActualizacion.Tick += (s, e) => CargarDatos();
            timerActualizacion.Start();
        }

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

        private void BtnGestionAlbumes_Click(object sender, EventArgs e)
        {
            FormGestionAlbumes formGestor = new FormGestionAlbumes();
            formGestor.ShowDialog();
        }

        private void BtnVotaciones_Click(object sender, EventArgs e)
        {
            FormRegistroVotos formVotos = new FormRegistroVotos();
            formVotos.ShowDialog();
            // Al volver de votar, recargo datos para mostrar resultados actualizados inmediatamente.
            CargarDatos();
        }

        private void BtnResultados_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Formulario de Resultados", "Información");
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            DialogResult confirmacion = MessageBox.Show(
                "¿Estás seguro de que deseas cerrar tu sesión?",
                "Cerrar Sesión",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmacion == DialogResult.Yes)
            {
                try
                {
                    // Limpieza total de sesión y reinicio de la app "en limpio".
                    Sesion.CerrarSesion();
                    Application.Restart();
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ocurrió un error al intentar salir: " + ex.Message);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Limpieza de recursos: Detengo el timer para evitar memory leaks.
            if (timerActualizacion != null)
            {
                timerActualizacion.Stop();
                timerActualizacion.Dispose();
            }
            base.OnFormClosing(e);
        }

        private bool EsCandidataLogueada()
        {
            // Verificación defensiva.
            if (Sesion.UsuarioActual == null)
            {
                System.Diagnostics.Debug.WriteLine("[RBAC] UsuarioActual es NULL");
                return false;
            }

            int idUsuario = Sesion.UsuarioActual.IdUsuario;
            System.Diagnostics.Debug.WriteLine($"[RBAC] IdUsuario sesión: {idUsuario}");

            // Consulto si este usuario tiene un perfil de candidata asociado y activo.
            var candidata = candidataDAO.ObtenerPorIdUsuario(idUsuario);

            System.Diagnostics.Debug.WriteLine(
                $"[RBAC] Candidata encontrada: {(candidata != null ? "SI" : "NO")}, " +
                $"Activa: {(candidata != null ? candidata.Activa.ToString() : "N/A")}"
            );

            return candidata != null && candidata.Activa;
        }
    }
}