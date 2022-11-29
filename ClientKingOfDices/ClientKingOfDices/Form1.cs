using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ClientKingOfDices
{
    public partial class Form1 : Form
    {
        Socket socket;
        int counter = 0;
        bool stop = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread start = new Thread(new ThreadStart(start_connect));
            start.Start();
        }

        private void start_connect()
        {
            IPAddress ip = IPAddress.Parse("10.0.0.146");
            IPEndPoint EP = new IPEndPoint(ip, 9999);
            socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                label1.Invoke((MethodInvoker)delegate ()
                {
                    label1.Text = "";
                });
                label2.Invoke((MethodInvoker)delegate ()
                {
                    label2.Text = "";
                });
                label3.Invoke((MethodInvoker)delegate ()
                {
                    label3.Text = "";
                });
                button1.Invoke((MethodInvoker)delegate ()
                {
                    button1.Enabled = false;
                });
                button2.Invoke((MethodInvoker)delegate ()
                {
                    button2.Enabled = true;
                });
                button3.Invoke((MethodInvoker)delegate ()
                {
                    button3.Enabled = true;
                });

                socket.Connect(EP);
                scrivi_risultato();

                counter = 0;
                int messaggio = 0;
                Thread ascolto = new Thread(() =>
                {
                    while (!stop)
                    {
                        messaggio = scrivi_risultato();
                        if (messaggio <= 0)
                            stop = true;
                    }
                    stop = false;
                });
                ascolto.Start();
                ascolto.Join();

                switch (messaggio)
                {
                    case 0:
                        MessageBox.Show("Avete pareggiato");
                        break;
                    case -1:
                        MessageBox.Show("Hai vinto");
                        break;
                    case -2:
                        MessageBox.Show("Hai perso");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Il valore ritornato non è quello corretto");
                }

                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception errore)
            {
                button1.Invoke((MethodInvoker)delegate ()
                {
                    button1.Enabled = true;
                });
                MessageBox.Show(errore.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            socket.Send(BitConverter.GetBytes(true));
            counter++;
            if(counter == 3)
            {
                button2.Enabled = false;
                button3.Enabled = false;
                button1.Enabled = true;
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            socket.Send(BitConverter.GetBytes(false));
            button2.Enabled = false;
            button3.Enabled = false;
            button1.Enabled = true;
        }

        private int scrivi_risultato()
        {
            byte[] BRecive = new byte[1024];
            socket.Receive(BRecive);
            int messaggio = BitConverter.ToInt32(BRecive, 0);
            if (messaggio >= 1)
            {
                label1.Invoke((MethodInvoker)delegate ()
                {
                    label1.Text = messaggio.ToString();
                });

                socket.Receive(BRecive);
                int messaggio2 = BitConverter.ToInt32(BRecive, 0);
                label2.Invoke((MethodInvoker)delegate ()
                {
                    label2.Text = messaggio2.ToString();
                });

                socket.Receive(BRecive);
                int messaggio3 = BitConverter.ToInt32(BRecive, 0);                
                label3.Invoke((MethodInvoker)delegate ()
                {
                    label3.Text = messaggio3.ToString();
                });
                return messaggio3;
            } else
            {
                return messaggio;
            }
        }
    }
}
