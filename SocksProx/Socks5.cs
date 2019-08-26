using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace SocksProx
{
    class Socks5
    {
        TcpListener tcpListener;
        IPEndPoint localEndPoint;

        public Socks5(IPAddress address, int port)
        {
            try
            {
                tcpListener = new TcpListener(address, port);
                localEndPoint = new IPEndPoint(address, port);
            }
            catch(Exception ex)
            {
                Console.WriteLine(DateTime.Now + "  " + ex.Message + "\nPress Enter to close application");
                Console.ReadLine();
                Environment.Exit(1);
            }
        }

        public void StartServer()
        {
            try
            {
                tcpListener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now + "  " + ex.Message + "\nPress Enter to close application");
                Console.ReadLine();
                Environment.Exit(1);
            }
            Console.WriteLine(DateTime.Now + " Server Started");
            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                Task.Run(() =>
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        byte[] firstRequest = new byte[client.ReceiveBufferSize];
                        client.Client.Receive(firstRequest);
                        if (ChekMethods(firstRequest))
                        {
                            client.Client.Send(SetFirstResponse());
                        }
                        else
                        {
                            byte[] error = new byte[] { 0x05, 0xff };
                            client.Client.Send(error);
                        }
                        byte[] secondRecive = new byte[client.ReceiveBufferSize];
                        client.Client.Receive(secondRecive);
                        IPEndPoint endPoint = GetConnectionInf(secondRecive); 
                        client.Client.Send(SetThirdResponce());
                        socket.Connect(endPoint);
                        DataExchange(socket, client);
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine(DateTime.Now + "  " + ex.Message + "\n" + "Connection closed");
                    }
                    finally
                    {
                        client.Close();
                        socket.Close();
                        Console.WriteLine(DateTime.Now + " Thread closed");
                    }
                });
            }
        }

        bool ChekMethods(byte[] firstRequest)
        {
            if (firstRequest[0] == 5 && firstRequest[1] > 0)
            {
                byte[] methods = new byte[firstRequest.Length - 2];
                Array.Copy(firstRequest, 2, methods, 0, methods.Length);
                foreach (byte method in methods)
                {
                    if (method == 0)
                        return true;
                }
            }
            return false;
        }

        byte[] SetFirstResponse()
        {
            byte[] responce = new byte[] { 0x05, 0x00 };
            return responce;
        }

        IPEndPoint GetConnectionInf(byte[] responce)
        {
            if (responce[1] == 1)
            {
                if (responce[3] == 1)
                {
                    byte[] ip = new byte[4];
                    Array.Copy(responce, 4, ip, 0, 4);
                    IPAddress iPAddress = new IPAddress(ip);
                    byte[] port = new byte[2];
                    Array.Copy(responce, 8, port, 0, 2);
                    Array.Reverse(port);
                    int portNumber = BitConverter.ToInt16(port, 0);
                    IPEndPoint endPoint = new IPEndPoint(iPAddress, portNumber);
                    return endPoint;
                }
            }
            else
            {
                Console.WriteLine(DateTime.Now +  " Close connection");
                throw new Exception(DateTime.Now + " Connection inf isn't valid");
            }
            return null;
        }

        byte[] SetThirdResponce()
        {
            byte[] port = BitConverter.GetBytes(localEndPoint.Port);
            IPAddress address = localEndPoint.Address;
            byte[] ip = address.GetAddressBytes(); 
            byte[] responce = new byte[]
            { 5, 0, 0, 1, ip[0], ip[1], ip[2], ip[3], port[1], port[0] };
            return responce;
        }

        void DataExchange(Socket socket, TcpClient client)
        {
            Console.WriteLine(DateTime.Now + " Start Data exchange");
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    using (NetworkStream remoteStream = new NetworkStream(socket))
                    {
                        Task ClientToHost = new Task(() =>
                        {
                            try
                            {
                                SendData(stream, remoteStream, client.ReceiveBufferSize);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(DateTime.Now + " " + ex.Message);
                            }
                        }, TaskCreationOptions.AttachedToParent);
                        Task hostToClient = new Task(()=> 
                        {
                            try
                            {
                                SendData(remoteStream, stream, socket.ReceiveBufferSize); 
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(DateTime.Now + " " + ex.Message);
                            }
                        }, TaskCreationOptions.AttachedToParent );
                        ClientToHost.Start();
                        hostToClient.Start();
                        Task.WaitAll(new Task[] { ClientToHost, hostToClient });
                        GC.Collect();
                        Console.WriteLine(DateTime.Now  + " Data Exchange Finiched");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void SendData(NetworkStream sender, NetworkStream recipient, long bufferSize)
        {
            try
            {
                byte[] buffer;
                long dataLength = 1;
                while (dataLength > 0)
                {
                    buffer = new byte[bufferSize];
                    dataLength = sender.Read(buffer, 0, buffer.Length);
                    Array.Resize(ref buffer, (int)dataLength);
                    recipient.Write(buffer, 0, buffer.Length);
                    GC.Collect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now + "  Data sending finished " + ex.Message);
            }
        }
    }
}
