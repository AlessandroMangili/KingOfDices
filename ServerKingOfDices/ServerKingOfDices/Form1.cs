using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace ServerKingOfDices
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            CheckForIllegalCrossThreadCalls=false;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
  
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            Socket socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endpoint = new IPEndPoint(ip, 9999);
            try
            {
                socket.Bind(endpoint);
                socket.Listen(10);
                Thread Accept = new Thread(() =>
                {
                    ClientAccept(socket);
                    button1.Enabled = true;
                });
                Accept.Start();
                button1.Enabled = false;
            }
            catch (Exception errore)
            {
                MessageBox.Show(errore.Message);
            }
        }

        private int ClientThread(Socket client)
        {
            int addrolls = 0;
            try
            {
                int RollDice;
                byte[] buffer;
                Random rn = new Random(Guid.NewGuid().GetHashCode());

                RollDice = rn.Next(1, 7);
                addrolls = RollDice;
                label1.Text = "dado 1 = " + RollDice.ToString();
                buffer = BitConverter.GetBytes(RollDice);
                client.Send(buffer);

                RollDice = rn.Next(1, 7);
                addrolls += RollDice;
                label2.Text = "dado 2 = " + RollDice.ToString();
                buffer = BitConverter.GetBytes(RollDice);
                client.Send(buffer);

                buffer = BitConverter.GetBytes(addrolls);
                label3.Text = "Somma = " + addrolls.ToString();
                client.Send(buffer);
            }
            catch (Exception errore)
            {
                MessageBox.Show(errore.Message);
            }
            return addrolls;
        }

        private void ClientAccept (Socket socket)
        {
            int N_client = 0;
            Dictionary<Socket,int> ValoriClient = new Dictionary<Socket, int>();
            while (true)
            {
                if (N_client < 2)
                {
                    Socket client = socket.Accept();

                    if (client.Connected == true)
                    {
                        N_client++;
                        ValoriClient.Add(client, 0);
                    }

                    Thread ThreadMulticlient = new Thread(() => {
                        if (N_client==2) //Entrerà soltanto il secondo client
                        {
                            int somma_dadi = ClientThread(ValoriClient.ElementAt(0).Key);
                            if (somma_dadi == 0)
                                throw new NullReferenceException("Non sono stati lanciati i dati a seguito di una eccezione");
                            ValoriClient[ValoriClient.ElementAt(1).Key] = somma_dadi;

                            somma_dadi = ClientThread(ValoriClient.ElementAt(1).Key);
                            if (somma_dadi == 0)
                                throw new NullReferenceException("Non sono stati lanciati i dati a seguito di una eccezione");
                            ValoriClient[ValoriClient.ElementAt(0).Key] = somma_dadi;
                        }
                    });
                    ThreadMulticlient.Start();
                    ThreadMulticlient.Join();
                } else 
                {
                    Thread player1 = new Thread(() =>
                    {
                        int counter = 0, ris = 0;
                        bool stop = false;
                        while (counter < 3 && !stop)
                        {
                            ris = gioca_turno(ValoriClient.ElementAt(0).Key);
                            if (ris != 0)
                                ValoriClient[ValoriClient.ElementAt(1).Key] = ris;
                            else
                                stop = true;
                            counter++;
                        }
                    });
                    player1.Start();

                    Thread player2 = new Thread(() =>
                    {
                        int counter = 0, ris = 0;
                        bool stop = false;
                        while (counter < 3 && !stop)
                        {
                            ris = gioca_turno(ValoriClient.ElementAt(1).Key);
                            if (ris != 0)
                                ValoriClient[ValoriClient.ElementAt(0).Key] = ris;
                            else
                                stop = true;
                            counter++;
                        }
                    });
                    player2.Start();

                    player1.Join();
                    player2.Join();

                    switch (who_wins(ValoriClient))
                    {
                        case 0:
                            ValoriClient.ElementAt(0).Key.Send(BitConverter.GetBytes(0));
                            ValoriClient.ElementAt(1).Key.Send(BitConverter.GetBytes(0));
                            break;
                        case -1:
                            ValoriClient.ElementAt(0).Key.Send(BitConverter.GetBytes(-1));
                            ValoriClient.ElementAt(1).Key.Send(BitConverter.GetBytes(-2));
                            break;
                        case -2:
                            ValoriClient.ElementAt(0).Key.Send(BitConverter.GetBytes(-2));
                            ValoriClient.ElementAt(1).Key.Send(BitConverter.GetBytes(-1));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Valore non presente nel range");
                    }

                    ValoriClient.ElementAt(0).Key.Shutdown(SocketShutdown.Both);
                    ValoriClient.ElementAt(0).Key.Close();
                    ValoriClient.ElementAt(1).Key.Shutdown(SocketShutdown.Both);
                    ValoriClient.ElementAt(1).Key.Close();
                    N_client = 0;
                    ValoriClient.Clear();
                    //label1.Text = "";
                    //label2.Text = "";
                    //label3.Text = "";
                }
            }
        }

        private int gioca_turno(Socket player)
        {
            byte[] buffer = new byte[1024];
            int risultato = 0;
           
            player.Receive(buffer);
            if (BitConverter.ToBoolean(buffer, 0))
            {
                int somma_dadi = ClientThread(player);
                if (somma_dadi == 0)
                    throw new NullReferenceException("Non sono stati lanciati i dati a seguito di una eccezione");
                risultato = somma_dadi;
            }
            return risultato;
        }

        private int who_wins(Dictionary<Socket, int> mappa)
        {
            if (mappa.ElementAt(0).Value > mappa.ElementAt(1).Value)
            {
                MessageBox.Show("Ha vinto il giocatore: " + mappa.ElementAt(0).Key.RemoteEndPoint + " totalizzando " + mappa.ElementAt(1).Value + " punti contro i " + mappa.ElementAt(0).Value + " punti");
                return -1;

            } 
            
            if (mappa.ElementAt(0).Value < mappa.ElementAt(1).Value)
            {
                MessageBox.Show("Ha vinto il giocatore: " + mappa.ElementAt(1).Key.RemoteEndPoint + " totalizzando " + mappa.ElementAt(0).Value + " punti contro i " + mappa.ElementAt(1).Value + " punti");
                return -2;
            }

            MessageBox.Show("La partita è finita in pareggio con un punteggio di " + mappa.ElementAt(0).Value + " punti");
            return 0;
        }
    }
}
