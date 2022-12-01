using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;

namespace buildTabla2
{
    public class IPSearcher
    {
        private static IPAddress host = null;
        private static bool isHostFound = false;
        private static string hostname = "";
        private static List<Ping> pings;
        public delegate void log(string str);
        private static log logTarget;
        public IPSearcher()
        {
        }

        public void setLog(log target)
        {
            logTarget = target;
        }
        private void init()
        {
            pings = new List<Ping>();
            isHostFound = false;
            host = null;
        }
        public IPAddress search(string hostname, string ipBase)
        {
            init();
            IPSearcher.hostname = hostname;
            IPAddress ip = IPAddress.Parse(ipBase);
            Byte[] bytes = ip.GetAddressBytes();
            object token = new object();
            for (int i = 2; i < 256; i++)
            {
                bytes[3] = (Byte)i;
                var _ip = new IPAddress(bytes);

                try
                {
                    Ping p = new Ping();
                    
                    p.SendAsync(_ip, token);
                    p.PingCompleted += new PingCompletedEventHandler(inspect);
                    pings.Add(p);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ex: " + ex);
                }
            }



            host = waitForIp();
            return host;
        }
        public static IPAddress waitForIp()
        {
            while (!isHostFound) { }
            cancelAllPings();
            return host;
        }
        private static void cancelAllPings()
        {
            foreach (Ping p in pings)
            {
                try
                {
                    //p.SendAsyncCancel();
                }
                catch (Exception ex) { }
            }
        }
        public static void inspect(object sender, PingCompletedEventArgs e)
        {
            try
            {

                if (e.Reply != null && e.Reply.Status == IPStatus.Success)
                {
                    string ip = e.Reply.Address.ToString();
                    Console.WriteLine(ip);
                    string name;
                    try
                    {
                        IPHostEntry hostEntry = Dns.GetHostEntry(ip);
                        name = hostEntry.HostName;
                        if (logTarget != null)
                        {
                            logTarget(name + ": " + ip.ToString());
                        }
                        if (name.ToLower().StartsWith(IPSearcher.hostname.ToLower()))
                        {
                            IPSearcher.isHostFound = true;
                            IPSearcher.host = e.Reply.Address;
                        }
                    }
                    catch (SocketException ex)
                    {
                        name = "?";
                    }


                }
                else if (e.Reply == null)
                {

                }

            }
            catch (Exception ex) { Console.WriteLine(DateTime.Now + " " + ex.Message); }
        }
    }

    public class DataParser
    {
        public static Dictionary<string, string> getData(string str)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            string[] split = str.Split(';');
            if (split.Length == 2)
            {
                dictionary.Add("id", split[0].Replace("[59]", ";"));
                dictionary.Add("data", split[1].Replace("[59]", ";"));
            }
            //str = str.Replace("[;]", ";");
            //str = str.Replace("[:]", ":");

            return dictionary;
        }

        public static string parseData(string id, string data)
        {
            //string str = "" + id.Trim().Replace(";", "[59]").Replace(":", "[58]") + ";" + "" + data.Trim().Replace(";", "[59]").Replace(":", "[58]");
            string str = "" + id.Trim().Replace(";", "[59]") + ";" + "" + data.Trim().Replace(";", "[59]");
            return str;
        }

    }
    public class PluginConsole
    {

        public const int SERVER = 0;
        public const int CLIENT = 1;
        private IPEndPoint ipEnd;
        private UdpClient udpClient;
        //private Socket tcpClient;
        //Socket tcpServer;
        private string ipAddress;
        private string ipBase;
        private int port;
        public PluginConsole(string ipAddress, int port, int type)
        {
            this.ipAddress = ipAddress;
            //this.ipBase = ipBase;
            this.port = port;
            if (type == CLIENT)
            {
                connectUDPClient();
            }
            else if (type == SERVER)
            {
                connectUDPServer();
            }
        }

        private PluginConsole()
        {
            // TODO: Complete member initialization
        }
        ~PluginConsole()
        {

            //tcpClient.Close();
            //tcpServer.Close();
            try
            {
                udpClient.Close();
            }
            catch (Exception e) { }
        }
        private IPEndPoint getValidEndPoint(string ipAddress, int port)
        {
            IPAddress[] ipAddresses = Dns.GetHostAddresses(ipAddress);
            IPEndPoint ipEnd = null;
            foreach (IPAddress _ipAddress in ipAddresses)
            {
                try
                {
                    if (_ipAddress.GetAddressBytes().Length == 4)
                    {

                        ipEnd = new IPEndPoint(_ipAddress, port);
                        break;
                    }
                }
                catch (Exception e)
                {

                }
            }
            //IPEndPoint ipEnd = null;
            //try
            //{
            //    IPSearcher searcher = new IPSearcher();
            //    IPAddress ip = searcher.search(hostname, ipBase);
            //    ipEnd = new IPEndPoint(ip, port);
            //}
            //catch (Exception ex) { }
            return ipEnd;
        }
        //public void connectTCPClient()
        //{
        //    IPEndPoint ipEnd = getValidEndPoint(host, port);

        //    tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        //    tcpClient.Connect(ipEnd);

        //}

        //public void sendTCPMessage(string message)
        //{
        //    Byte[] buffer = Encoding.UTF8.GetBytes(message);
        //    tcpClient.Send(buffer);
        //}

        //public void closeTCPClient()
        //{
        //    tcpClient.Close();
        //}

        //public void connectTCPServer()
        //{
        //    IPEndPoint ipEnd = getValidEndPoint(host, port);
        //    tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        //    tcpServer.Bind(ipEnd);
        //    tcpServer.Listen(100);
        //    Console.WriteLine("Conectado a " + ipEnd.Address);
        //}




        //public void listenTCPClients()
        //{
        //    tcpClient = tcpServer.Accept();
        //    Console.WriteLine("Cliente aceptado " + tcpClient.RemoteEndPoint);
        //}
        //public string readTCPMessage()
        //{
        //    string header = "[" + DateTime.Now + "] ";
        //    string message = null;
        //    Byte[] buffer = new Byte[5000];
        //    if (tcpClient.Connected)
        //    {
        //        tcpClient.Receive(buffer);
        //        message = Encoding.UTF8.GetString(buffer);
        //    }

        //    return header + message.Trim();
        //}


        private void connectUDPClient()
        {
            ipEnd = getValidEndPoint(ipAddress, port);
            if (ipEnd != null)
            {
                udpClient = new UdpClient();
                udpClient.Connect(ipEnd);
            }

        }

        public void sendUDPMessage(string message)
        {
            if (udpClient != null)
            {
                Byte[] size = new Byte[4];
                Byte[] buffer = Encoding.UTF8.GetBytes(message);

                int bufferLength = buffer.Length;
                size = BitConverter.GetBytes(bufferLength);
                udpClient.Send(size, size.Length);
                udpClient.Send(buffer, buffer.Length);
            }
        }

        public void writeLine(string line)
        {
            sendUDPMessage(line);
        }

        public void closeUDPClient()
        {
            udpClient.Close();
        }

        private void connectUDPServer()
        {
            //IPEndPoint ipEnd = getValidEndPoint(hostname, ipBase, port);

            udpClient = new UdpClient(port);


        }

        public string readUDPMessage()
        {

            string message = null;
            Byte[] size = new Byte[4];
            size = udpClient.Receive(ref ipEnd);
            int bufferLength = BitConverter.ToInt32(size, 0);

            Byte[] buffer = new Byte[bufferLength];

            {
                buffer = udpClient.Receive(ref ipEnd);
                message = Encoding.UTF8.GetString(buffer);
            }

            return message.Trim();

        }
    }
}
