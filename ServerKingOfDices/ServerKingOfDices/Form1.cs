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
using System.Net.NetworkInformation;

namespace ServerKingOfDices
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress ip = getIpAddress();                

                Socket server = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint endpoint = new IPEndPoint(ip, 9999);
                server.Bind(endpoint);
                server.Listen(10);

                Thread Accept = new Thread(() =>
                {
                    clientAccept(server);
                });
                Accept.Start();
                button1.Enabled = false;
            }
            catch (Exception errore)
            {
                MessageBox.Show(errore.Message);
            }
        }

        private IPAddress getIpAddress()
        {
            List<IPAddress> allIP = new List<IPAddress>(Dns.GetHostAddresses(Dns.GetHostName()));
            IPAddress ip = IPAddress.Parse("127.0.0.1");

            foreach (var inf in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (inf.Name.Equals("Ethernet") || inf.Name.Equals("Wi-Fi"))
                {
                    foreach (UnicastIPAddressInformation ip_interface in inf.GetIPProperties().UnicastAddresses)
                    {
                        if (ip_interface.Address.AddressFamily == AddressFamily.InterNetwork && allIP.Contains(ip_interface.Address))
                            ip = IPAddress.Parse(ip_interface.Address.ToString());

                    }
                }
            }
            return ip;
        }

        /**
         * I thread.Sleep servono per rallentare l'invio sul canale socket altrimenti il tempo per raggiungere 
         * l'host remoto non è sufficiente e si perderebbero i pacchetti. In caso non arrivano tutti i numeri ai vari client,
         * aumentare il thread sleep in modo da lasciare il tempo di far arrivare tutti i pacchetti
         */
        private int clientThread(Socket client)
        {
            int addrolls = 0;
            try
            {
                int RollDice;
                Random rn = new Random(Guid.NewGuid().GetHashCode());

                RollDice = rn.Next(1, 7);
                addrolls = RollDice;
                client.Send(BitConverter.GetBytes(RollDice));
                Thread.Sleep(200);

                RollDice = rn.Next(1, 7);
                client.Send(BitConverter.GetBytes(RollDice));
                Thread.Sleep(200);

                addrolls += RollDice;
                client.Send(BitConverter.GetBytes(addrolls));
                Thread.Sleep(200);
            }
            catch (Exception errore)
            {
                MessageBox.Show(errore.Message);
            }
            return addrolls;
        }

        private void clientAccept(Socket server)
        {
            int N_client = 0;
            Dictionary<Socket,int> ValoriClient = new Dictionary<Socket, int>();
            while (true)
            {
                if (N_client < 2)
                {
                    try
                    {
                        Socket client = server.Accept();

                        if (client.Connected == true)
                        {
                            N_client++;
                            ValoriClient.Add(client, 0);
                        }

                        Thread ThreadMulticlient = new Thread(() =>
                        {
                            if (N_client == 2)
                            {
                                int somma_dadi = clientThread(ValoriClient.ElementAt(0).Key);
                                if (somma_dadi == 0)
                                    throw new NullReferenceException("Non sono stati lanciati i dati a seguito di una eccezione");
                                ValoriClient[ValoriClient.ElementAt(0).Key] = somma_dadi;

                                somma_dadi = clientThread(ValoriClient.ElementAt(1).Key);
                                if (somma_dadi == 0)
                                    throw new NullReferenceException("Non sono stati lanciati i dati a seguito di una eccezione");
                                ValoriClient[ValoriClient.ElementAt(1).Key] = somma_dadi;
                            }
                        });
                        ThreadMulticlient.Start();
                        ThreadMulticlient.Join();
                    }
                    catch (Exception errore)
                    {
                        MessageBox.Show(errore.Message);
                    }
                } else 
                {
                    try
                    {
                        Thread player1 = new Thread(() =>
                        {
                            int counter = 0, ris = 0;
                            bool stop = false;
                            while (counter < 3 && !stop)
                            {
                                ris = playTurn(ValoriClient.ElementAt(0).Key, ValoriClient.ElementAt(1).Key);
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
                                ris = playTurn(ValoriClient.ElementAt(1).Key, ValoriClient.ElementAt(0).Key);
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

                        switch (whoWins(ValoriClient))
                        {
                            case 0:
                                ValoriClient.ElementAt(0).Key.Send(BitConverter.GetBytes(0));
                                ValoriClient.ElementAt(1).Key.Send(BitConverter.GetBytes(0));
                                break;
                            case -1:
                                ValoriClient.ElementAt(0).Key.Send(BitConverter.GetBytes(-2));
                                ValoriClient.ElementAt(1).Key.Send(BitConverter.GetBytes(-1));
                                break;
                            case -2:
                                ValoriClient.ElementAt(0).Key.Send(BitConverter.GetBytes(-1));
                                ValoriClient.ElementAt(1).Key.Send(BitConverter.GetBytes(-2));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("Valore non presente nel range");
                        }

                        ValoriClient.ElementAt(0).Key.Shutdown(SocketShutdown.Both);
                        ValoriClient.ElementAt(0).Key.Close();
                        ValoriClient.ElementAt(1).Key.Shutdown(SocketShutdown.Both);
                        ValoriClient.ElementAt(1).Key.Close();
                    } catch(Exception errore)
                    {
                        MessageBox.Show(errore.Message);
                    }                        
                    N_client = 0;
                    ValoriClient.Clear();
                }
            }
        }

        private int playTurn(Socket player1, Socket player2)
        {
            byte[] buffer = new byte[1024];
            int risultato = 0;
            try
            {
                player1.Receive(buffer);
                if (BitConverter.ToBoolean(buffer, 0))
                {
                    int somma_dadi = clientThread(player2);
                    if (somma_dadi == 0)
                        throw new NullReferenceException("Non sono stati lanciati i dati a seguito di una eccezione");
                    risultato = somma_dadi;
                }
            }
            catch(Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
            return risultato;
        }

        private int whoWins(Dictionary<Socket, int> mappa)
        {
            foreach (var x in mappa)
            {
                MessageBox.Show(x.Value.ToString());
            }
            if (mappa.ElementAt(0).Value > mappa.ElementAt(1).Value)
                return -1;
            if (mappa.ElementAt(0).Value < mappa.ElementAt(1).Value)
                return -2;
            return 0;
        }
    }
}
