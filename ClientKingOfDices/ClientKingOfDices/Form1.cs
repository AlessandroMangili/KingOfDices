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
using System.IO;
using System.Runtime.ExceptionServices;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

namespace ClientKingOfDices
{
    public partial class Form1 : Form
    {
        Socket socket;
        int counter = 0;
        bool stop = false;
        private string path = @"..\..\src\{}";
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RollDadi.Enabled = true;
            RollDadi2.Enabled = true;
            try
            {
                if (counter == 0)
                {
                    label1.Text = "";
                    label2.Text = "";
                    label3.Text = "";
                    RollDadi.Image = Image.FromFile(path.Replace("{}", "Lancio_Dadi.gif"));
                    RollDadi2.Image = Image.FromFile(path.Replace("{}", "Lancio_Dadi.gif"));
                    button3.Enabled = true;

                    Thread start = new Thread(new ThreadStart(start_connect));
                    start.Start();
                    counter++;
                }
                else
                {
                    socket.Send(BitConverter.GetBytes(true));
                    counter++;
                    if (counter == 4)
                    {
                        button1.Enabled = false;
                        button3.Enabled = false;
                    }
                }
            } catch(Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }

        private void start_connect()
        {
            IPAddress ip = IPAddress.Parse("10.0.0.165");
            IPEndPoint EP = new IPEndPoint(ip, 9999);
            socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(EP);
                scrivi_risultato();

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

                button1.Invoke((MethodInvoker)delegate ()
                {
                    button1.Enabled = true;
                });
                counter = 0;
            }
            catch (Exception errore)
            {
                counter = 0;
                MessageBox.Show(errore.Message);
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            socket.Send(BitConverter.GetBytes(false));
            button3.Enabled = false;
            button1.Enabled = false;
        }

        private int scrivi_risultato()
        {
            byte[] BRecive = new byte[1024];
            int messaggio = 0;
            try
            {
                socket.Receive(BRecive);
                messaggio = BitConverter.ToInt32(BRecive, 0);
                if (messaggio >= 1)
                {
                    label1.Invoke((MethodInvoker)delegate ()
                    {
                        label1.Text = messaggio.ToString();
                    });

                    RollDadi.Image = new Bitmap(path.Replace("{}", "Dado_" + messaggio.ToString() + ".jpg"));

                    socket.Receive(BRecive);
                    messaggio = BitConverter.ToInt32(BRecive, 0);
                    label2.Invoke((MethodInvoker)delegate ()
                    {
                        label2.Text = messaggio.ToString();
                    });

                    RollDadi2.Image = new Bitmap(path.Replace("{}", "Dado_" + messaggio.ToString() + ".jpg"));

                    socket.Receive(BRecive);
                    messaggio = BitConverter.ToInt32(BRecive, 0);
                    label3.Invoke((MethodInvoker)delegate ()
                    {
                        label3.Text = messaggio.ToString();
                    });
                }

                RollDadi.Invoke((MethodInvoker)delegate ()
                {
                    RollDadi.Enabled = false;
                });
                RollDadi2.Invoke((MethodInvoker)delegate ()
                {
                    RollDadi2.Enabled = false;
                });
            } catch(Exception exp)
            {
                MessageBox.Show(exp.Message);
            }

            return messaggio;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RollDadi.Enabled = false;
            RollDadi2.Enabled = false;
            button3.Enabled = false;
            try
            {
                RollDadi.Image = Image.FromFile(path.Replace("{}", "Lancio_Dadi.gif"));
                RollDadi2.Image = Image.FromFile(path.Replace("{}", "Lancio_Dadi.gif"));
            } catch(Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }
    }
}
