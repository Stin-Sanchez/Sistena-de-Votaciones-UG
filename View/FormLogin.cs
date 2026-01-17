using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
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

namespace SIVUG.View
{
    public partial class FormLogin : Form
    {

        // --- UX: Colores y Diseño ---
        private Color colorFondo = Color.FromArgb(15, 15, 15); // Negro suave
        private Color colorPanelInput = Color.FromArgb(30, 30, 30);
        private Color colorPrimario = Color.FromArgb(52, 152, 219); // Azul SIVUG
        private Color colorTexto = Color.DimGray;
        private Color colorTextoFocus = Color.White;

        // Controles
        private TextBox txtUsuario;
        private TextBox txtPassword;
        private Label lblMensajeError;
        public FormLogin()
        {
            InitializeComponent();
            ConfigurarFormulario();
            InicializarComponentes();
        }

        private void FormLogin_Load(object sender, EventArgs e)
        {

        }

        private void ConfigurarFormulario()
        {
            this.Size = new Size(780, 330);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None; // Sin bordes
            this.BackColor = colorFondo;
            this.Opacity = 0.95; // Un toque elegante de transparencia
        }

        private void InicializarComponentes()
        {
            // --- 1. Panel Izquierdo (Branding) ---
            Panel panelLogo = new Panel { Dock = DockStyle.Left, Width = 250, BackColor = colorPrimario };

            Label lblTitulo = new Label
            {
                Text = "SIVUG",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(70, 100)
            };

            PictureBox pbLogo = new PictureBox
            {
                // Si tienes un logo, cárgalo aquí. Si no, usaremos un icono genérico o nada.
                Size = new Size(100, 100),
                Location = new Point(75, 150),
                SizeMode = PictureBoxSizeMode.Zoom
                // Image = Image.FromFile("ruta_logo.png") 
            };

            panelLogo.Controls.Add(lblTitulo);
            panelLogo.Controls.Add(pbLogo);
            this.Controls.Add(panelLogo);

            // --- 2. Panel Derecho (Inputs) ---
            // Titulo Login
            Label lblLogin = new Label
            {
                Text = "LOGIN",
                Font = new Font("Segoe UI", 16, FontStyle.Regular),
                ForeColor = Color.White,
                Location = new Point(300, 30),
                AutoSize = true
            };
            this.Controls.Add(lblLogin);

            // Input Usuario
            txtUsuario = CrearInput("USUARIO / MATRÍCULA", 300, 80);

            // Input Password
            txtPassword = CrearInput("CONTRASEÑA", 300, 140);
            txtPassword.UseSystemPasswordChar = true; // Ocultar caracteres por defecto

            // Botón Entrar
            Button btnAcceder = new Button
            {
                Text = "ACCEDER",
                BackColor = colorPanelInput,
                ForeColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(400, 40),
                Location = new Point(300, 220),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10)
            };
            btnAcceder.FlatAppearance.BorderSize = 0;
            btnAcceder.Click += BtnAcceder_Click;
            this.Controls.Add(btnAcceder);

            // Botón Cerrar (X)
            Label lblCerrar = new Label
            {
                Text = "X",
                ForeColor = Color.White,
                Location = new Point(750, 5),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                AutoSize = true
            };
            lblCerrar.Click += (s, e) => Application.Exit();
            this.Controls.Add(lblCerrar);

            // Mensaje de Error (Oculto al inicio)
            lblMensajeError = new Label
            {
                Text = "Credenciales incorrectas",
                ForeColor = Color.IndianRed,
                Location = new Point(300, 190),
                AutoSize = true,
                Visible = false,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(lblMensajeError);

            // Funcionalidad para mover la ventana (Drag)
            this.MouseDown += (s, e) => ReleaseCapture();
            this.MouseMove += (s, e) => SendMessage(this.Handle, 0x112, 0xf012, 0);
            panelLogo.MouseDown += (s, e) => ReleaseCapture();
            panelLogo.MouseMove += (s, e) => SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        // Helper para crear inputs bonitos con línea abajo
        private TextBox CrearInput(string placeholder, int x, int y)
        {
            // Label flotante
            /* Label lbl = new Label { Text = placeholder, ForeColor = colorTexto, Location = new Point(x, y), Font = new Font("Segoe UI", 8) };
            this.Controls.Add(lbl); */ // Simplificado para UX limpia

            TextBox txt = new TextBox
            {
                BackColor = colorFondo,
                ForeColor = colorTexto,
                BorderStyle = BorderStyle.None,
                Location = new Point(x, y + 5),
                Width = 400,
                Font = new Font("Segoe UI", 12),
                Text = placeholder
            };

            // Línea debajo
            Panel linea = new Panel { BackColor = colorTexto, Location = new Point(x, y + 30), Size = new Size(400, 1) };
            this.Controls.Add(linea);

            // Eventos para efecto Placeholder
            txt.Enter += (s, e) => {
                if (txt.Text == placeholder)
                {
                    txt.Text = "";
                    txt.ForeColor = colorTextoFocus;
                    if (placeholder == "CONTRASEÑA") txt.UseSystemPasswordChar = true;
                }
            };
            txt.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    txt.Text = placeholder;
                    txt.ForeColor = colorTexto;
                    if (placeholder == "CONTRASEÑA") txt.UseSystemPasswordChar = false;
                }
            };

            // Hack visual para password al inicio
            if (placeholder == "CONTRASEÑA") txt.UseSystemPasswordChar = false;

            this.Controls.Add(txt);
            return txt;
        }

        private void BtnAcceder_Click(object sender, EventArgs e)
        {
            // 1. Validaciones básicas
            if (txtUsuario.Text == "USUARIO / MATRÍCULA" || txtPassword.Text == "CONTRASEÑA")
            {
                MostrarError("Por favor ingrese usuario y contraseña.");
                return;
            }

            // 2. Lógica de Login
            try
            {
                EstudianteDAO dao = new EstudianteDAO(); // Instancia tu DAO real
                // Asumiendo que tu DAO ya tiene el método Login que hicimos arriba
                var estudiante = dao.Login(txtUsuario.Text, txtPassword.Text);

                if (estudiante != null)
                {
                    // LOGIN EXITOSO
                    Sesion.IniciarSesion(estudiante);

                    // Abrir Dashboard
                    FormDashboard principal = new FormDashboard();
                    principal.Show();

                    // Ocultar Login (no cerrar, para que la app no muera)
                    this.Hide();
                    // Opcional: configurar FormClosed del dashboard para cerrar la app
                    principal.FormClosed += (s, args) => this.Close();
                }
                else
                {
                    MostrarError("Usuario o contraseña incorrectos.");
                    txtPassword.Text = "";
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error de conexión: " + ex.Message);
            }
        }

        private void MostrarError(string msg)
        {
            lblMensajeError.Text = "    " + msg;
            lblMensajeError.Visible = true;
        }

        // --- DLL IMPORTS PARA MOVER VENTANA SIN BORDES ---
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hwnd, int wmsg, int wparam, int lparam);
    }
}
    

