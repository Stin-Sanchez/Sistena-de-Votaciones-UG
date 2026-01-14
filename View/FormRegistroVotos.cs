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

    public partial class FormRegistroVotos : Form
    {
        private EstudianteService estudianteService;
        private VotacionService votacionService;
        private CandidataDAO candidataDAO;

        private Estudiante estudianteActual;
        private List<Candidata> candidatasActivas;
        private int candidataSeleccionadaId = 0;
        private TipoVoto tipoVotacionSeleccionado;

        // Controles
        private TextBox txtCedula;
        private Button btnBuscar;
        private ComboBox cboTipoVotacion;
        private Panel panelEstudiante;
        private Panel panelCandidatas;
        private Button btnConfirmarVoto;
        private Button btnCancelar;
        private Label lblEstadoVoto;
        private Label lblTipoVotacionSeleccionado;


        private void FormRegistroVotos_Load(object sender, EventArgs e)
        {
          

        }
        public FormRegistroVotos()
        {
            InitializeComponent();
            estudianteService = new EstudianteService();
            votacionService = new VotacionService();
            candidataDAO = new CandidataDAO();

            ConfigurarFormulario();
            InicializarComponentes();
        }



        private void ConfigurarFormulario()
        {
            this.Text = "SIVUG - Registro de Votos";
            this.Size = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 245);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private void InicializarComponentes()
        {
            // Título
            Label lblTitulo = new Label
            {
                Text = "REGISTRO DE VOTACIÓN",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(30, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitulo);

            // Panel de búsqueda de estudiante
            CrearPanelBusqueda();

            // Panel de selección de tipo de votación
            CrearPanelTipoVotacion();

            // Panel de información del estudiante
            CrearPanelEstudiante();

            // Panel de candidatas
            CrearPanelCandidatas();

            // Botones de acción
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
                Visible = false
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

            // Cargar enum en el ComboBox
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

            // Agregar cada valor del enum
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
                case TipoVoto.Reina:
                    return "👑 Reina de la Universidad";
                case TipoVoto.Fotogenia:
                    return "📸 Miss Fotogenia";
                default:
                    return tipo.ToString();
            }
        }

        // Clase auxiliar para el ComboBox
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

            // Los datos se llenarán dinámicamente
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
                Enabled = false
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

        // Event Handlers
        private void TxtCedula_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Solo permitir números
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }

            // Enter para buscar
            if (e.KeyChar == (char)Keys.Enter)
            {
                BtnBuscar_Click(sender, e);
            }
        }

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
                Cursor = Cursors.WaitCursor;

                estudianteActual = estudianteService.ValidarEstudiante(txtCedula.Text);

                if (estudianteActual == null)
                {
                    MessageBox.Show("No se encontró ningún estudiante con esa cédula",
                        "Estudiante no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpiarFormulario();
                    return;
                }

                if (estudianteActual.HavotadoReina && estudianteActual.HavotadoFotogenia)
                {
                    lblEstadoVoto.Text = "⚠️ Este estudiante ya ha emitido su voto";
                    lblEstadoVoto.ForeColor = Color.FromArgb(231, 76, 60);
                    lblEstadoVoto.Visible = true;

                    MostrarInformacionEstudiante();
                    OcultarPanelesPosteriorBusqueda();
                    return;
                }

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
            if (cboTipoVotacion.SelectedItem == null)
                return;

            var itemSeleccionado = (ComboBoxItem)cboTipoVotacion.SelectedItem;
            tipoVotacionSeleccionado = itemSeleccionado.Value;

            // Actualizar mensaje informativo
            ActualizarMensajeTipoVotacion();

            // Cargar candidatas según el tipo seleccionado
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
                cboTipoVotacion.SelectedIndex = -1;
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

        private void CargarCandidatasPorTipo()
        {
            panelCandidatas.Controls.Clear();
            candidatasActivas = candidataDAO.ObtenerActivas();

            var candidatasFiltradas = candidatasActivas
         .Where(c => c.tipoCandidatura == tipoVotacionSeleccionado)
         .ToList();
            // -----------------------------

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

            // Actualizar título con el tipo de votación
            var lblTitulo = this.Controls.Find("lblTituloCandidatas", false).FirstOrDefault() as Label;
            if (lblTitulo != null)
            {
                lblTitulo.Text = $"SELECCIONE UNA CANDIDATA PARA {tipoVotacionSeleccionado.ToString().ToUpper()}:";
            }

            panelCandidatas.Visible = true;
            candidataSeleccionadaId = 0;
            btnConfirmarVoto.Enabled = false;
        }

        private Panel CrearItemCandidato(Candidata candidata)
        {
            Panel item = new Panel
            {
                Size = new Size(270, 180),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = candidata.CandidataId,
                Cursor = Cursors.Hand
            };

            // --- CORRECCIÓN: IMPLEMENTACIÓN DE IMAGEN ---

            // 1. Creamos el PictureBox
            PictureBox picFoto = new PictureBox
            {
                Location = new Point(75, 15),
                Size = new Size(120, 100),
                BackColor = Color.FromArgb(189, 195, 199), // Gris por defecto
                SizeMode = PictureBoxSizeMode.Zoom // Para que la foto no se deforme
            };

            // 2. Intentamos cargar la foto
            bool fotoCargada = false;
            if (!string.IsNullOrEmpty(candidata.ImagenPrincipal) && System.IO.File.Exists(candidata.ImagenPrincipal))
            {
                try
                {
                    picFoto.Image = Image.FromFile(candidata.ImagenPrincipal);
                    fotoCargada = true;
                }
                catch { /* Si falla, se queda gris */ }
            }

            // 3. Si NO se cargó foto, mostramos las iniciales (Tu código anterior)
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
                picFoto.Controls.Add(lblIniciales); // Agregamos las iniciales DENTRO del PictureBox
            }

            // Agregamos el PictureBox al item principal
            item.Controls.Add(picFoto);
            // ---------------------------------------------

            // Nombre (Igual que tenías)
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

            // Facultad (Igual que tenías)
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

            // --- EVENTOS DE CLIC ---
            // Es importante agregar el evento click también al PictureBox para que responda
            item.Click += (s, e) => SeleccionarCandidato(item);
            picFoto.Click += (s, e) => SeleccionarCandidato(item); // <--- Nuevo
            lblNombre.Click += (s, e) => SeleccionarCandidato(item);
            lblFacultad.Click += (s, e) => SeleccionarCandidato(item);

            // Si había iniciales, también necesitan el evento
            if (picFoto.Controls.Count > 0)
                picFoto.Controls[0].Click += (s, e) => SeleccionarCandidato(item);

            return item;
        }

        private string ObtenerIniciales(string nombreCompleto)
        {
            // 1. Validación de seguridad por si viene null o vacío
            if (string.IsNullOrWhiteSpace(nombreCompleto)) return "??";

            // 2. CORRECCIÓN: Usamos RemoveEmptyEntries para evitar errores con espacios dobles
            string[] palabras = nombreCompleto.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // 3. Lógica de iniciales
            if (palabras.Length >= 2)
            {
                // Tomamos la primera letra del primer nombre y la primera del segundo (o apellido)
                return (palabras[0][0].ToString() + palabras[1][0].ToString()).ToUpper();
            }

            // 4. Caso si solo tiene un nombre (ej: "Sting")
            return nombreCompleto.Substring(0, Math.Min(2, nombreCompleto.Length)).ToUpper();
        }

        private void SeleccionarCandidato(Panel itemSeleccionado)
        {
            // Deseleccionar todos
            foreach (Control ctrl in panelCandidatas.Controls)
            {
                if (ctrl is Panel)
                {
                    ctrl.BackColor = Color.FromArgb(250, 250, 250);
                }
            }

            // Seleccionar el actual
            itemSeleccionado.BackColor = Color.FromArgb(52, 152, 219);
            candidataSeleccionadaId = (int)itemSeleccionado.Tag;
            btnConfirmarVoto.Enabled = true;
        }

        private void BtnConfirmarVoto_Click(object sender, EventArgs e)
        {
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

                    // DENTRO DE BtnConfirmarVoto_Click, antes del try/catch
                    MessageBox.Show($"ID Estudiante: {estudianteActual.Id}\nID Candidata: {candidataSeleccionada.CandidataId}\nTipo: {tipoVotacionSeleccionado}", "Depuración");

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
                        txtCedula.Focus();
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
   