using System;
using System.Net;

namespace SocksProx
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the ipAddres your proxy server and press Enter, \n after that enter the port number" +
                "and press enter ");
            Socks5 socks5;
            try
            {
                socks5 = new Socks5(IPAddress.Parse(Console.ReadLine()), int.Parse(Console.ReadLine()));
                socks5.StartServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now + "  " + ex.Message + "\nPress Enter to close application");
                Console.ReadLine();
                Environment.Exit(1);
            }
        }
    }
}