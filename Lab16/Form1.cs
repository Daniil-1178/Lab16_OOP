using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace Lab16
{
    public partial class Form1 : Form
    {
        private bool alive = false;
        private UdpClient client;

        private int port = 8001;
        private string hostAddress = "255.255.255.255";
        private string userName;

        public Form1()
        {
            InitializeComponent();

            loginButton.Enabled = true;
            logoutButton.Enabled = false;
            sendButton.Enabled = false;
            chatTextBox.ReadOnly = true;

            txtIpAddress.Text = hostAddress;
            txtPort.Text = port.ToString();
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(userNameTextBox.Text))
            {
                MessageBox.Show("Введіть своє ім'я перед входом!", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            userName = userNameTextBox.Text;
            userNameTextBox.ReadOnly = true;

            hostAddress = txtIpAddress.Text.Trim();
            if (!int.TryParse(txtPort.Text.Trim(), out port))
            {
                MessageBox.Show("Некоректний формат порту! Введіть число.", "Помилка");
                userNameTextBox.ReadOnly = false;
                return;
            }

            try
            {
                client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                client.EnableBroadcast = true;

                alive = true;
                Task receiveTask = new Task(ReceiveMessages);
                receiveTask.Start();

                System.Threading.Thread.Sleep(50);

                string message = userName + " увійшов(ла) в чат";
                SendMessage(message);

                loginButton.Enabled = false;
                logoutButton.Enabled = true;
                sendButton.Enabled = true;

                txtIpAddress.Enabled = false;
                txtPort.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка підключення: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                userNameTextBox.ReadOnly = false;
            }
        }

        private void ReceiveMessages()
        {
            try
            {
                while (alive)
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = client.Receive(ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);

                    this.Invoke(new MethodInvoker(() =>
                    {
                        string time = DateTime.Now.ToShortTimeString();
                        chatTextBox.AppendText($"[{time}] {message}\r\n");

                        chatTextBox.SelectionStart = chatTextBox.Text.Length;
                        chatTextBox.ScrollToCaret();
                    }));
                }
            }
            catch (ObjectDisposedException)
            {
                if (!alive) return;
                throw;
            }
            catch (Exception ex)
            {
                if (alive)
                {
                    this.Invoke(new MethodInvoker(() => MessageBox.Show(ex.Message)));
                }
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(messageTextBox.Text)) return;

            try
            {
                string message = string.Format("{0}: {1}", userName, messageTextBox.Text);
                SendMessage(message);
                messageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Помилка відправки");
            }
        }

        private void SendMessage(string msg)
        {
            byte[] data = Encoding.Unicode.GetBytes(msg);
            client.Send(data, data.Length, hostAddress, port);
        }

        private void logoutButton_Click(object sender, EventArgs e)
        {
            ExitChat();
        }

        private void ExitChat()
        {
            if (!alive) return;

            try
            {
                string message = userName + " залишає чат";
                SendMessage(message);

                alive = false;
                client.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при виході з чату: " + ex.Message);
            }
            finally
            {
                loginButton.Enabled = true;
                logoutButton.Enabled = false;
                sendButton.Enabled = false;
                userNameTextBox.ReadOnly = false;

                txtIpAddress.Enabled = true;
                txtPort.Enabled = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (alive)
            {
                ExitChat();
            }
        }

        private void btnChangeFont_Click(object sender, EventArgs e)
        {
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                chatTextBox.Font = fontDialog1.Font;
                messageTextBox.Font = fontDialog1.Font;
            }
        }

        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(chatTextBox.Text))
            {
                MessageBox.Show("Історія повідомлень порожня. Немає чого зберігати!", "Інформація", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            saveFileDialog1.Filter = "Текстові файли (*.txt)|*.txt|Усі файли (*.*)|*.*";
            saveFileDialog1.Title = "Зберегти лог чату";
            saveFileDialog1.FileName = $"ChatLog_{DateTime.Now:yyyy-MM-dd}";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveFileDialog1.FileName, chatTextBox.Text, Encoding.UTF8);
                    MessageBox.Show("Лог чату успішно експортовано у файл!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Сталася помилка при збереженні файлу: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}