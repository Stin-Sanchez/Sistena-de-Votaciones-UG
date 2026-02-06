using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.SERVICES;
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
    /// VISTA DE VOTACIÓN.
    ///Donde los estudiantes ejercen su derecho al voto.
    /// Responsabilidades principales:
    /// 1. Validar la identidad y elegibilidad del estudiante (¿Ya votó?).
    /// 2. Filtrar candidatas según la categoría elegida (Reina vs Fotogenia).
    /// 3. Garantizar una confirmación explícita antes de persistir el voto.
    /// </summary>
    public partial class FormRegistroVotos : Form
    {
        // Servicios de dominio para la lógica de negocio.
        private EstudianteService estudianteService;
        private VotacionService votacionService;
        
        // Acceso a datos para obtener el catálogo de candidatas.
        private CandidataDAO candidataDAO;

        // Estado del formulario (Contexto actual).
        private Estudiante estudianteActual;
        private List<Candidata> candidatasActivas;
        private int candidataSeleccionadaId = 0; // 0 indica ninguna selección.
        private TipoVoto tipoVotacionSeleccionado;

        // Componentes de la interfaz generados manualmente.
        private TextBox txtCedula;
        private Button btnBuscar;
        private ComboBox cboTipoVotacion;
        private Panel panelEstudiante;
        private Panel panelCandidatas; // Contenedor dinámico de tarjetas.
        private Button btnConfirmarVoto;
        private Button btnCancelar;
        private Label lblEstadoVoto;
        private Label lblTipoVotacionSeleccionado;


        public FormRegistroVotos()
        {
            InitializeComponent();
            
            // Inyección de dependencias manual.
            estudianteService = new EstudianteService();
            votacionService = new VotacionService();
            candidataDAO = new CandidataDAO();

            ConfigurarFormulario();
            
            // Construcción del Layout visual.
            InicializarComponentes();
        }

        private void FormRegistroVotos_Load(object sender, EventArgs e)
        {
        }

        private void ConfigurarFormulario()
        {
            this.Text = "SIVUG - Registro de Votos";
            this.Size = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 245);
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // Tamaño fijo para mantener la integridad del diseño.
            this.MaximizeBox = false;
        }

        /// <summary>
        /// Orquesta la creación de todos los paneles de la interfaz paso a paso.
        /// </summary>
        private void InicializarComponentes()
        {
            // Header
            Label lblTitulo = new Label
            {
                Text = "REGISTRO DE VOTACIÓN",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(30, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitulo);

            // Secciones lógicas del proceso de votación.
            CrearPanelBusqueda();
            CrearPanelTipoVotacion();
            CrearPanelEstudiante();
            CrearPanelCandidatas();
            CrearBotonesAccion();
        }

        private void CrearPanelBusqueda()
        {
            Panel panelBusqueda = new Panel
            {
                Location = new Point(30, 70),
                Size = new Size(920, 80),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelBusqueda);

            Label lblInstruccion = new Label
            {
                Text = "Ingrese la cédula del estudiante:",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(20, 15),
                AutoSize = true
            };
            panelBusqueda.Controls.Add(lblInstruccion);

            txtCedula = new TextBox
            {
                Location = new Point(20, 42),
                Size = new Size(250, 30),
                Font = new Font("Segoe UI", 12F)
            };
            // UX: Permitir buscar al presionar Enter.
            txtCedula.KeyPress += TxtCedula_KeyPress;
            panelBusqueda.Controls.Add(txtCedula);

            btnBuscar = new Button
            {
                Text = "🔍 Buscar Estudiante",
                Location = new Point(280, 40),
                Size = new Size(180, 35),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += BtnBuscar_Click;
            panelBusqueda.Controls.Add(btnBuscar);

            // Etiqueta para feedback inmediato sobre el estado del votante (habilitado/bloqueado).
            lblEstadoVoto = new Label
            {
                Location = new Point(480, 42),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            panelBusqueda.Controls.Add(lblEstadoVoto);
        }

        private void CrearPanelTipoVotacion()
        {
            Panel panelTipo = new Panel
            {
                Location = new Point(30, 170),
                Size = new Size(920, 90),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false // Se oculta hasta que se valide un estudiante.
            };
            panelTipo.Name = "panelTipoVotacion";
            this.Controls.Add(panelTipo);

            Label lblTitulo = new Label
            {
                Text = "SELECCIONE EL TIPO DE VOTACIÓN",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(20, 10),
                AutoSize = true
            };
            panelTipo.Controls.Add(lblTitulo);

            Label lblInstruccion = new Label
            {
                Text = "Tipo de votación:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(52, 73, 94),
                Location = new Point(20, 45),
                AutoSize = true
            };
            panelTipo.Controls.Add(lblInstruccion);

            cboTipoVotacion = new ComboBox
            {
                Location = new Point(160, 43),
                Size = new Size(250, 30),
                Font = new Font("Segoe UI", 11F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboTipoVotacion.SelectedIndexChanged += CboTipoVotacion_SelectedIndexChanged;
            panelTipo.Controls.Add(cboTipoVotacion);

            // Populo el combo con los valores del Enum.
            CargarTiposVotacion();

            lblTipoVotacionSeleccionado = new Label
            {
                Location = new Point(430, 43),
                Size = new Size(450, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(127, 140, 141),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelTipo.Controls.Add(lblTipoVotacionSeleccionado);
        }

        private void CargarTiposVotacion()
        {
            cboTipoVotacion.Items.Clear();

            foreach (TipoVoto tipo in Enum.GetValues(typeof(TipoVoto)))
            {
                cboTipoVotacion.Items.Add(new ComboBoxItem
                {
                    Text = ObtenerDescripcionTipoVotacion(tipo),
                    Value = tipo
                });
            }

            cboTipoVotacion.DisplayMember = "Text";
            cboTipoVotacion.ValueMember = "Value";
        }

        private string ObtenerDescripcionTipoVotacion(TipoVoto tipo)
        {
            switch (tipo)
            {
                case TipoVoto.Reina: return "👑 Reina de la Universidad";
                case TipoVoto.Fotogenia: return "📸 Miss Fotogenia";
                default: return tipo.ToString();
            }
        }

        // Wrapper class para elementos del combobox.
        private class ComboBoxItem
        {
            public string Text { get; set; }
            public TipoVoto Value { get; set; }
        }

        private void CrearPanelEstudiante()
        {
            panelEstudiante = new Panel
            {
                Location = new Point(30, 280),
                Size = new Size(920, 100),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            this.Controls.Add(panelEstudiante);

            Label lblTituloEstudiante = new Label
            {
                Text = "INFORMACIÓN DEL ESTUDIANTE",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(20, 10),
                AutoSize = true
            };
            panelEstudiante.Controls.Add(lblTituloEstudiante);
        }

        private void CrearPanelCandidatas()
        {
            Label lblTituloCandidatas = new Label
            {
                Text = "SELECCIONE UNA CANDIDATA:",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(30, 400),
                AutoSize = true
            };
            lblTituloCandidatas.Name = "lblTituloCandidatas";
            this.Controls.Add(lblTituloCandidatas);

            // FlowLayoutPanel o Panel con AutoScroll para permitir muchas candidatas.
            panelCandidatas = new Panel
            {
                Location = new Point(30, 430),
                Size = new Size(920, 250),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Visible = false
            };
            this.Controls.Add(panelCandidatas);
        }

        private void CrearBotonesAccion()
        {
            btnConfirmarVoto = new Button
            {
                Text = "✓ CONFIRMAR VOTO",
                Location = new Point(550, 695),
                Size = new Size(200, 45),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false // Se habilita solo cuando hay selección válida.
            };
            btnConfirmarVoto.FlatAppearance.BorderSize = 0;
            btnConfirmarVoto.Click += BtnConfirmarVoto_Click;
            this.Controls.Add(btnConfirmarVoto);

            btnCancelar = new Button
            {
                Text = "✗ Cancelar",
                Location = new Point(760, 695),
                Size = new Size(190, 45),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancelar);
        }

        // ==================== LÓGICA DE NEGOCIO ====================

        private void TxtCedula_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Validar solo números para la cédula.
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }

            if (e.KeyChar == (char)Keys.Enter)
            {
                BtnBuscar_Click(sender, e);
            }
        }

        /// <summary>
        /// Busca al estudiante y valida su elegibilidad para votar.
        /// REGLA DE NEGOCIO: Un estudiante solo puede votar una vez por categoría.
        /// </summary>
        private void BtnBuscar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCedula.Text))
            {
                MessageBox.Show("Por favor ingrese una cédula", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCedula.Focus();
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor; // Feedback visual

                estudianteActual = estudianteService.ObtenerPorCedula(txtCedula.Text);

                if (estudianteActual == null)
                {
                    MessageBox.Show("No se encontró ningún estudiante con esa cédula",
                        "Estudiante no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpiarFormulario();
                    return;
                }

                // Checkeo rápido: Si ya votó en ambas, le avisamos y bloqueamos.
                if (estudianteActual.HavotadoReina && estudianteActual.HavotadoFotogenia)
                {
                    lblEstadoVoto.Text = "⚠️ Este estudiante ya ha emitido su voto";
                    lblEstadoVoto.ForeColor = Color.FromArgb(231, 76, 60);
                    lblEstadoVoto.Visible = true;

                    MostrarInformacionEstudiante();
                    OcultarPanelesPosteriorBusqueda();
                    return;
                }

                // Si puede votar, avanzamos.
                lblEstadoVoto.Text = "✓ Estudiante habilitado para votar";
                lblEstadoVoto.ForeColor = Color.FromArgb(46, 204, 113);
                lblEstadoVoto.Visible = true;

                MostrarInformacionEstudiante();
                MostrarPanelTipoVotacion();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar estudiante: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void CboTipoVotacion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboTipoVotacion.SelectedItem == null) return;

            var itemSeleccionado = (ComboBoxItem)cboTipoVotacion.SelectedItem;
            tipoVotacionSeleccionado = itemSeleccionado.Value;

            ActualizarMensajeTipoVotacion();

            // Carga dinámica: Solo muestro candidatas que compiten en la categoría seleccionada.
            CargarCandidatasPorTipo();
        }

        private void ActualizarMensajeTipoVotacion()
        {
            switch (tipoVotacionSeleccionado)
            {
                case TipoVoto.Reina:
                    lblTipoVotacionSeleccionado.Text = "Votará por la Reina de la Universidad";
                    break;
                case TipoVoto.Fotogenia:
                    lblTipoVotacionSeleccionado.Text = "Votará por Miss Fotogenia";
                    break;
            }
        }

        private void MostrarPanelTipoVotacion()
        {
            var panelTipo = this.Controls.Find("panelTipoVotacion", false).FirstOrDefault();
            if (panelTipo != null)
            {
                panelTipo.Visible = true;
                cboTipoVotacion.SelectedIndex = -1; // Reset selección.
                lblTipoVotacionSeleccionado.Text = "";
            }
            panelCandidatas.Visible = false;
            btnConfirmarVoto.Enabled = false;
        }

        private void OcultarPanelesPosteriorBusqueda()
        {
            var panelTipo = this.Controls.Find("panelTipoVotacion", false).FirstOrDefault();
            if (panelTipo != null)
                panelTipo.Visible = false;

            panelCandidatas.Visible = false;
            btnConfirmarVoto.Enabled = false;
        }

        private void MostrarInformacionEstudiante()
        {
            panelEstudiante.Controls.Clear();

            Label lblTituloEstudiante = new Label
            {
                Text = "INFORMACIÓN DEL ESTUDIANTE",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(20, 10),
                AutoSize = true
            };
            panelEstudiante.Controls.Add(lblTituloEstudiante);

            Label lblNombre = new Label
            {
                Text = $"Nombre: {estudianteActual.Nombres} {estudianteActual.Apellidos}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(52, 73, 94),
                Location = new Point(20, 40),
                AutoSize = true
            };
            panelEstudiante.Controls.Add(lblNombre);

            Label lblCedula = new Label
            {
                Text = $"Cédula: {estudianteActual.DNI}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(52, 73, 94),
                Location = new Point(20, 65),
                AutoSize = true
            };
            panelEstudiante.Controls.Add(lblCedula);

            Label lblFacultad = new Label
            {
                Text = $"Facultad: {estudianteActual.Carrera.Facultad.Nombre}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(52, 73, 94),
                Location = new Point(400, 40),
                AutoSize = true
            };
            panelEstudiante.Controls.Add(lblFacultad);

            panelEstudiante.Visible = true;
        }

        /// <summary>
        /// Genera la cuadrícula de tarjetas de candidatas.
        /// Filtra la lista en memoria usando LINQ para mostrar solo las relevantes.
        /// </summary>
        private void CargarCandidatasPorTipo()
        {
            panelCandidatas.Controls.Clear();
            candidatasActivas = candidataDAO.ObtenerActivas();

            var candidatasFiltradas = candidatasActivas
                 .Where(c => c.tipoCandidatura == tipoVotacionSeleccionado)
                 .ToList();

            if (candidatasFiltradas.Count == 0)
            {
                Label lblSinCandidatas = new Label
                {
                    Text = "No hay candidatas inscritas para esta categoría.",
                    Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(127, 140, 141),
                    Location = new Point(250, 100),
                    AutoSize = true
                };
                panelCandidatas.Controls.Add(lblSinCandidatas);
                panelCandidatas.Visible = true;
                return;
            }

            // Layout manual de grilla (3 columnas).
            int x = 20;
            int y = 20;
            int itemsPorFila = 3;
            int contador = 0;

            foreach (var candidata in candidatasFiltradas)
            {
                Panel itemCandidato = CrearItemCandidato(candidata);
                itemCandidato.Location = new Point(x, y);
                panelCandidatas.Controls.Add(itemCandidato);

                contador++;
                if (contador % itemsPorFila == 0)
                {
                    x = 20;
                    y += 210;
                }
                else
                {
                    x += 290;
                }
            }

            // Actualizo el título de la sección para dar contexto.
            var lblTitulo = this.Controls.Find("lblTituloCandidatas", false).FirstOrDefault() as Label;
            if (lblTitulo != null)
            {
                lblTitulo.Text = $"SELECCIONE UNA CANDIDATA PARA {tipoVotacionSeleccionado.ToString().ToUpper()}:";
                lblTitulo.ForeColor = Color.FromArgb(44, 62, 80);
                lblTitulo.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            }

            panelCandidatas.Visible = true;
            candidataSeleccionadaId = 0;
            btnConfirmarVoto.Enabled = false;
        }

        /// <summary>
        /// Crea una "Tarjeta" visual seleccionable para cada candidata.
        /// </summary>
        private Panel CrearItemCandidato(Candidata candidata)
        {
            Panel item = new Panel
            {
                Size = new Size(270, 180),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = candidata.CandidataId, // Guardo el ID oculto para usarlo al seleccionar.
                Cursor = Cursors.Hand
            };

            // Avatar de candidata.
            PictureBox picFoto = new PictureBox
            {
                Location = new Point(75, 15),
                Size = new Size(120, 100),
                BackColor = Color.FromArgb(189, 195, 199),
                SizeMode = PictureBoxSizeMode.Zoom 
            };

            bool fotoCargada = false;
            if (!string.IsNullOrEmpty(candidata.ImagenPrincipal) && System.IO.File.Exists(candidata.ImagenPrincipal))
            {
                try
                {
                    picFoto.Image = Image.FromFile(candidata.ImagenPrincipal);
                    fotoCargada = true;
                }
                catch { }
            }

            // Fallback: Si no hay foto, muestro iniciales generadas.
            if (!fotoCargada)
            {
                Label lblIniciales = new Label
                {
                    Text = ObtenerIniciales(candidata.Nombres),
                    Font = new Font("Segoe UI", 32F, FontStyle.Bold),
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                picFoto.Controls.Add(lblIniciales); 
            }

            item.Controls.Add(picFoto);

            // Información visible
            Label lblNombre = new Label
            {
                Text = candidata.Nombres,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(10, 125),
                Size = new Size(250, 35),
                TextAlign = ContentAlignment.TopCenter
            };
            item.Controls.Add(lblNombre);

            Label lblFacultad = new Label
            {
                Text = candidata.Carrera.Facultad.Nombre,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(127, 140, 141),
                Location = new Point(10, 158),
                Size = new Size(250, 15),
                TextAlign = ContentAlignment.TopCenter
            };
            item.Controls.Add(lblFacultad);

            // PROPAGACIÓN DE EVENTOS:
            // Todos los controles hijos deben disparar el click del padre para simular que toda la tarjeta es un botón.
            item.Click += (s, e) => SeleccionarCandidato(item);
            picFoto.Click += (s, e) => SeleccionarCandidato(item); 
            lblNombre.Click += (s, e) => SeleccionarCandidato(item);
            lblFacultad.Click += (s, e) => SeleccionarCandidato(item);

            if (picFoto.Controls.Count > 0)
                picFoto.Controls[0].Click += (s, e) => SeleccionarCandidato(item);

            return item;
        }

        private string ObtenerIniciales(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto)) return "??";

            // Elimino entradas vacías para manejar espacios extra accidentales.
            string[] palabras = nombreCompleto.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (palabras.Length >= 2)
            {
                return (palabras[0][0].ToString() + palabras[1][0].ToString()).ToUpper();
            }

            return nombreCompleto.Substring(0, Math.Min(2, nombreCompleto.Length)).ToUpper();
        }

        /// <summary>
        /// Maneja el estado visual de selección (Highlight).
        /// </summary>
        private void SeleccionarCandidato(Panel itemSeleccionado)
        {
            // Reset visual: Todas a blanco.
            foreach (Control ctrl in panelCandidatas.Controls)
            {
                if (ctrl is Panel)
                {
                    ctrl.BackColor = Color.FromArgb(250, 250, 250);
                }
            }

            // Highlight: Seleccionada a azul.
            itemSeleccionado.BackColor = Color.FromArgb(52, 152, 219);
            candidataSeleccionadaId = (int)itemSeleccionado.Tag;
            btnConfirmarVoto.Enabled = true;
        }

        /// <summary>
        /// ACUMULACIÓN Y PERSISTENCIA DEL VOTO.
        /// </summary>
        private void BtnConfirmarVoto_Click(object sender, EventArgs e)
        {
            // Validaciones finales de integridad.
            if (cboTipoVotacion.SelectedIndex == -1)
            {
                MessageBox.Show("Por favor seleccione el tipo de votación", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboTipoVotacion.Focus();
                return;
            }

            if (candidataSeleccionadaId == 0)
            {
                MessageBox.Show("Por favor seleccione una candidata", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var candidataSeleccionada = candidatasActivas.Find(c => c.CandidataId == candidataSeleccionadaId);

            // Confirmación explícita para evitar errores humanos.
            DialogResult confirmacion = MessageBox.Show(
                $"CONFIRME SU VOTO:\n\n" +
                $"Tipo: {ObtenerDescripcionTipoVotacion(tipoVotacionSeleccionado)}\n" +
                $"Candidata: {candidataSeleccionada.Nombres}\n" +
                $"Facultad: {candidataSeleccionada.Carrera.Facultad.Nombre}\n\n" +
                "¿Está seguro? Esta acción no se puede deshacer.",
                "Confirmar Voto",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion == DialogResult.Yes)
            {
                try
                {
                    Cursor = Cursors.WaitCursor;

                    // Debugging log.
                    MessageBox.Show($"ID Estudiante: {estudianteActual.Id}\nID Candidata: {candidataSeleccionada.CandidataId}\nTipo: {tipoVotacionSeleccionado}", "Depuración");

                    // Llamada al servicio transaccional.
                    bool exito = votacionService.RegistrarVoto(
                        estudianteActual,
                        candidataSeleccionada, tipoVotacionSeleccionado);

                    if (exito)
                    {
                        MessageBox.Show(
                            $"¡Voto registrado exitosamente!\n\n" +
                            $"Tipo: {tipoVotacionSeleccionado}\n" +
                            $"Candidata: {candidataSeleccionada.Nombres}\n\n" +
                            "Gracias por participar.",
                            "Éxito",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        LimpiarFormulario();
                        txtCedula.Focus(); // Listo para el siguiente votante.
                    }
                    else
                    {
                        MessageBox.Show(
                            "No se pudo registrar el voto. El estudiante ya ha votado.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al registrar voto: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private void LimpiarFormulario()
        {
            txtCedula.Text = "";
            estudianteActual = null;
            candidataSeleccionadaId = 0;
            cboTipoVotacion.SelectedIndex = -1;
            lblTipoVotacionSeleccionado.Text = "";

            panelEstudiante.Visible = false;

            var panelTipo = this.Controls.Find("panelTipoVotacion", false).FirstOrDefault();
            if (panelTipo != null)
                panelTipo.Visible = false;

            panelCandidatas.Visible = false;
            lblEstadoVoto.Visible = false;
            btnConfirmarVoto.Enabled = false;

            var lblTitulo = this.Controls.Find("lblTituloCandidatas", false).FirstOrDefault() as Label;
            if (lblTitulo != null)
            {
                lblTitulo.Text = "SELECCIONE UNA CANDIDATA:";
            }
        }
    }
}