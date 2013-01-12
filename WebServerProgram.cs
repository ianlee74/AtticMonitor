using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using System.Text;
using Networking;
using Microsoft.SPOT.Hardware;

using System.IO;


namespace NetworkingExample
{

    public class WebServerProgram
    {
        private static string rootDirectory = null;

        public static void Main()
        {
            Debug.Print("Program Started.");

            #region Static IP example
            /*
            Networking.Adapter.IPAddress = new byte[] { 192, 168, 1, 95 };  //192.168.1.95
            Networking.Adapter.DomainNameServer = new byte[] { 192, 168, 1, 1 };  // 192.168.1.1
            Networking.Adapter.Gateway = new byte[] { 192, 168, 1, 1 };  // 192.168.1.1
            Networking.Adapter.DhcpDisabled = true;
            */
            #endregion

            // http://forums.netduino.com/index.php?/topic/322-experimental-drivers-for-wiznet-based-ethernet-shields/page__view__findpost__p__3170
            // 5C-86-4A-00-00-DD   This is a test MAC address from Secret Labs
            // Note: This MAC address should be Unique, but it should work fine on a local network (as long as there is only one instance running with this MAC)
            Networking.Adapter.Start(new byte[] { 0x5c, 0x86, 0x4a, 0x00, 0x00, 0xde }, "mip", InterfaceProfile.Cerberus_Socket6_ENC28);
            
            Networking.Adapter.OnHttpReceivedPacketEvent += new Adapter.HttpPacketReceivedEventHandler(Adapter_OnHttpReceivedPacketEvent);
            Networking.Adapter.ListenToPort(80);  // Listen on Port 80, the default web server port
            
            // Loop to keep program alive
            while (true) Thread.Sleep(100);

        }

        static void Adapter_OnHttpReceivedPacketEvent(HttpRequest request)
        {
            byte[] webPage = Encoding.UTF8.GetBytes("<html><head><meta http-equiv=\"refresh\" content=\"5\"></head><body><font face=\"verdana\"><h1>Mouse Trap Alerter</h1><p><a href=\"http://ianlee.info\">http://ianlee.info/</a></p></font></body></html>");
            var s = new System.IO.MemoryStream(webPage);  // substitute a FileStream here when reading from MicroSD
            request.SendResponse(new HttpResponse(s));
        }
    }
}
