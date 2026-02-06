using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using SIVUG.Models.SERVICES;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace SIVUG.View
{
    /// <summary>
    /// Soy el formulario de inicio de sesión.
    /// Mi responsabilidad es autenticar al usuario y redirigirlo al entorno correcto.
    /// Implemento seguridad básica (ocultar contraseña) y flujos críticos como el
    /// "Cambio de Contraseña Obligatorio" en el primer login.
    /// </summary>
    public partial class FormLogin : Form
    {
        // --- UX: Paleta de Colores ---
        // Defino los colores aquí para mantener consistencia visual en todo el formulario.
        // Uso un tema oscuro (Dark Mode) para darle un aspecto moderno y reducir fatiga visual.
        private Color colorFondo = Color.FromArgb(15, 15, 15); // Negro suave
        private Color colorPanelInput = Color.FromArgb(30, 30, 30);
        private Color colorPrimario = Color.FromArgb(52, 152, 219); // Azul corporativo SIVUG
        private Color colorTexto = Color.DimGray;
        private Color colorTextoFocus = Color.White;

        // Referencias a los controles clave para manipularlos desde la lógica.
        private TextBox txtUsuario;
        private TextBox txtPassword;
        private Label lblMensajeError;

        public FormLogin()
        {
            InitializeComponent();
            ConfigurarFormulario();
            
            // Construyo la UI por código para control absoluto del posicionamiento (Pixel Perfect).
            InicializarComponentes();
        }

        private void FormLogin_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Configuro la ventana para que sea un "splash screen" funcional sin bordes de sistema opertivo.
        /// </summary>
        private void ConfigurarFormulario()
        {
            this.Size = new Size(900, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None; // Quito bordes estándar de Windows
            this.BackColor = colorFondo;
            this.Opacity = 0.97; // Ligera transparencia estética
        }

        private void InicializarComponentes()
        {
            // === 1. Panel Izquierdo (Branding & Identidad) ===
            // Uso una imagen de fondo que se adapta (Stretch) para generar impacto visual.
            Panel panelLogo = new Panel 
            { 
                Dock = DockStyle.Left, 
                Width = 350, 
                BackColor = colorPrimario,
                Padding = new Padding(30),
                BackgroundImage = Properties.Resources.unamed, 
                BackgroundImageLayout = ImageLayout.Stretch
            };

            // Logo central superpuesto.
            PictureBox pbLogo = new PictureBox
            {
                Size = new Size(120, 120),
                Location = new Point(115, 165),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
        
            panelLogo.Controls.Add(pbLogo);
            this.Controls.Add(panelLogo);

            // Coordenada X base para alinear todos los controles del lado derecho.
            int xPosition = 420;

            // === 2. Panel Derecho (Formulario) ===
            Label lblLogin = new Label
            {
                Text = "Iniciar Sesión",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(xPosition, 40),
                AutoSize = true,
            };
            this.Controls.Add(lblLogin);

            Label lblInstruccion = new Label
            {
                Text = "Ingresa tus credenciales para acceder al sistema",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(xPosition, 75),
                AutoSize = true
            };
            this.Controls.Add(lblInstruccion);

            // Genero los inputs usando mi helper personalizado.
            txtUsuario = CrearInput("Usuario o Matrícula", xPosition, 120, false);
            txtPassword = CrearInput("Contraseña", xPosition, 210, true); // true para ocultar caracteres

            Button btnAcceder = new Button
            {       
                Text = "ACCEDER",
                BackColor = colorPrimario,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(420, 45),
                Location = new Point(xPosition, 310),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TabIndex = 2
            };
            btnAcceder.FlatAppearance.BorderSize = 0;
            btnAcceder.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            btnAcceder.Click += BtnAcceder_Click;
            this.Controls.Add(btnAcceder);

            // Configuro el comportamiento del teclado: ENTER dispara el botón y el foco inicia en Usuario.
            this.AcceptButton = btnAcceder;
            this.ActiveControl = txtUsuario;

            // Botón de cierre manual (X) ya que no tenemos bordes del sistema.
            Label lblCerrar = new Label
            {
                Text = "X",
                ForeColor = Color.White,
                Location = new Point(860, 5),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                AutoSize = true
            };
            lblCerrar.Click += (s, e) => Application.Exit();
            this.Controls.Add(lblCerrar);

            // Label para mensajes de error. Inicialmente invisible.
            lblMensajeError = new Label
            {
                ForeColor = Color.FromArgb(231, 76, 60), // Rojo alerta
                Location = new Point(xPosition, 280),
                AutoSize = false,
                Size = new Size(420, 20),
                Visible = false,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblMensajeError);

            // Habilito el arrastre de la ventana desde el cliente y el panel lateral.
            this.MouseDown += (s, e) => ReleaseCapture();
            this.MouseMove += (s, e) => SendMessage(this.Handle, 0x112, 0xf012, 0);
            panelLogo.MouseDown += (s, e) => ReleaseCapture();
            panelLogo.MouseMove += (s, e) => SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        /// <summary>
        /// Helper Method (UX): Crea un campo de texto estilizado (Material Design-ish).
        /// Combina Titulo (Label), Campo (TextBox) y una línea inferior visual.
        /// </summary>
        private TextBox CrearInput(string etiqueta, int x, int y, bool esPassword)
        {
            Panel panelInput = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(420, 70),
                BackColor = colorFondo
            };

            Label lbl = new Label
            {
                Text = etiqueta,
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(0, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                AutoSize = true
            };

            TextBox txt = new TextBox
            {
                BackColor = Color.FromArgb(25, 25, 25), // Ligeramente más claro que el fondo
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(0, 25),
                Width = 420,
                Height = 35,
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = esPassword,
                Padding = new Padding(8)
            };

            // Eventos de foco para dar feedback visual al usuario.
            txt.Enter += (s, e) => 
            {
                txt.BackColor = Color.FromArgb(35, 35, 35); // Resalte
                lbl.ForeColor = colorPrimario;                 // Color activo
            };
            
            txt.Leave += (s, e) => 
            {
                txt.BackColor = Color.FromArgb(25, 25, 25); // Vuelta a normalidad
                lbl.ForeColor = Color.FromArgb(200, 200, 200);
            };

            panelInput.Controls.Add(lbl);
            panelInput.Controls.Add(txt);
            this.Controls.Add(panelInput);

            return txt;
        }

        private void BtnAcceder_Click(object sender, EventArgs e)
        {
            try
            {
                // PASO 1: Validación básica de campos vacíos.
                if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MostrarError("Completa usuario y contraseña");
                    return;
                }

                //  PASO 2: Llamada al servicio de autenticación.
                UsuarioService usuarioService = new UsuarioService();
                Usuario usuario = usuarioService.Autenticar(txtUsuario.Text, txtPassword.Text);

                if (usuario == null)
                {
                    // Mensaje genérico por seguridad. No decir "usuario no existe".
                    MostrarError("Usuario o contraseña incorrectos");
                    txtPassword.Clear();
                    return;
                }

                //  PASO 3: Autenticación exitosa. Guardo la sesión.
                Sesion.IniciarSesion(usuario);

                // ⭐ PASO 4: Regla de Negocio Crítica - Primer Login
                // Si el usuario tiene marcado el flag de cambio obligatorio, interrumpo el flujo normal.
                if (RequiereCambioContraseña(usuario))
                {
                    System.Diagnostics.Debug.WriteLine($"[PRIMER LOGIN DETECTADO] Usuario: {usuario.NombreUsuario}");

                    // Abro el formulario de cambio de contraseña en modo bloqueante (True).
                    FormCambiarContraseña frmCambio = new FormCambiarContraseña(esPrimerLogin: true);
                    DialogResult resultado = frmCambio.ShowDialog();

                    // Si el usuario cierra el formulario sin cambiar la contraseña, le niego el acceso.
                    if (resultado != DialogResult.OK)
                    {
                        MessageBox.Show(
                            "Debes cambiar tu contraseña para acceder al sistema.",
                            "Cambio de Contraseña Obligatorio",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        
                        Sesion.CerrarSesion();
                        return;
                    }

                    // Si tuvo éxito, recargo los datos del usuario (ya con la nueva contraseña y flag actualizado).
                    usuario = usuarioService.ObtenerPorIdConPersona(usuario.IdUsuario);
                    Sesion.IniciarSesion(usuario);
                }

                //  PASO 5: Todo correcto, abro el Dashboard y oculto el login.
                FormDashboard frm = new FormDashboard();
                frm.Show();
                
                this.Hide();
            }
            catch (Exception ex)
            {
                MostrarError(" Error: " + ex.Message);
                System.Diagnostics.Debug.WriteLine($"[ERROR LOGIN] {ex.Message}");
            }
        }

        /// <summary>
        /// Valida si el usuario tiene pendiente el cambio de contraseña inicial.
        /// La fuente de verdad es la base de datos (propiedad RequiereCambioContraseña).
        /// </summary>
        private bool RequiereCambioContraseña(Usuario usuario)
        {
            if (usuario == null) return false;

            bool requiereCambio = usuario.RequiereCambioContraseña;
            
            if (requiereCambio)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[CAMBIO REQUERIDO] Usuario: {usuario.NombreUsuario}, " +
                    $"Flag: {usuario.RequiereCambioContraseña}"
                );
            }
            
            return requiereCambio;
        }

        private void MostrarError(string msg)
        {
            lblMensajeError.Text = msg;
            lblMensajeError.Visible = true;
        }

        // --- INTEROP: Métodos nativos de Windows para mover la ventana sin barra de título ---
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hwnd, int wmsg, int wparam, int lparam);
    }
}

