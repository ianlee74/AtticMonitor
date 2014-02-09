using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Gadgeteer.Interfaces;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using uPLibrary.IoT.ThingSpeak;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules.GHIElectronics;

namespace IanLee.AtticMonitor
{
    public partial class Program
    {
        // I moved this key to a file called NO_GIT.cs and added it to my .gitignore file so I wouldn't accidentally add it to GIT.
        // You can either add the same file or uncomment below and remove any references to NO_GIT in the project.
        // You will need to go to http://thingspeak.com to create your account and optain your write API key.
        //private const string THINGSPEAK_WRITE_API_KEY = "";

        private const string TWITTER_USER_NAME = "ianlee74_IoT";

        private ThingSpeakClient _thingSpeakClient;
        private readonly DataEntry _thingSpeakDataEntry = new DataEntry();
        private static NetworkInterface _ni = null;
        private static readonly byte[] _macAddress = new byte[] { 0x66, 0x57, 0xBA, 0xBF, 0xA3, 0x6C };
        private static string _ipAddress = "0.0.0.0";
        private DigitalInput _trap1Status = null;
        private DigitalInput _trap2Status = null;
        private static bool _isArmed = false;
        private static GT.Timer _trapTimer = null;
        private static Thread _connectEthernetThread = null;
        private readonly string[] _displayLine = new string[2];

        void ProgramStarted()
        {
#if DEBUG
            Debug.Print("Program Started");
#endif
            Microsoft.SPOT.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += (sender, args) =>
                {
#if DEBUG
                    Debug.Print("Network availability changed!  " + args.IsAvailable);
#endif
                    if (args.IsAvailable && !_connectEthernetThread.IsAlive)
                    {
                        // Initialize the network & ThingSpeak client.
                        //_connectEthernetThread = new Thread(Initialize);
                        //_connectEthernetThread.Start();
                    }
                    else
                    {
                        multicolorLed1.TurnBlue();
                        multicolorLed2.TurnBlue();
                        UpdateDisplay("Network down.");
                    }
                };

            Microsoft.SPOT.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += (sender, args) =>
            {
                Debug.Print("Network address changed!  " + _ni.IPAddress.ToString());
            }; 

            UpdateDisplay("Good morning!");

            _trap1Status = extender.SetupDigitalInput(GT.Socket.Pin.Nine, GlitchFilterMode.Off, ResistorMode.PullUp);
            _trap2Status = extender.SetupDigitalInput(GT.Socket.Pin.Eight, GlitchFilterMode.Off, ResistorMode.PullUp);

            multicolorLed1.GreenBlueSwapped = true;
            multicolorLed2.GreenBlueSwapped = true;

            // Once all communications are up then we can arm & start monitoring the traps.
            var t1Status = false;
            var t2Status = false;
            _trapTimer = new GT.Timer(500);
            _trapTimer.Tick += timer =>
                {
                    t1Status = _trap1Status.Read();
                    t2Status = _trap2Status.Read();
                    UpdateDisplay(null, "T1: " + (t1Status ? "1" : "0") + "   T2: " + (t2Status ? "1" : "0"));
                    if (_isArmed && (t1Status || t2Status))     // Pin will be pulled high when the trap is released.
                    {
                        MouseCaught(_trap1Status.Read() ? 2 : 1);
                    }
                };

            // Initialize the network & ThingSpeak client.
            _connectEthernetThread = new Thread(Initialize);
            _connectEthernetThread.Start();

            button.ButtonReleased += (sender, state) => ArmTrap();
        }

        private void Initialize()
        {
            multicolorLed1.TurnBlue();
            multicolorLed2.TurnBlue();
#if DEBUG
            Debug.Print("Initializing network...");
#endif
            UpdateDisplay("Init network...");
            InitializeNetwork_Static();
            //InitializeNetwork_Dhcp();
            multicolorLed1.TurnGreen();
#if DEBUG
            Debug.Print("Beginning TeamSpeak initialization...");
#endif
            UpdateDisplay("Init TeamSpeak...");
            NtpTime("time.windows.com", -360);
            _thingSpeakClient = new ThingSpeakClient(false);
#if DEBUG
            Debug.Print("TeamSpeak initialized.");
#endif
            multicolorLed2.TurnGreen();
            
            UpdateDisplay("Ready to arm.", "T1: " + (_trap1Status.Read() ? "1" : "0") + "   T2: " + (_trap2Status.Read() ? "1" : "0"));

            // If trap was previously armed, then automatically re-arm.
            if (_isArmed) ArmTrap();
        }

        private void ArmTrap()
        {
            // Make sure traps are ready.
            if (!_trap1Status.Read()) // || !_trap2Status.Read())
            {
                UpdateDisplay("Can't arm.", "Traps not ready.");

                // Blink the button LED a few times.
                for (var i = 0; i < 5; i++ )
                {
                    button.TurnLEDOn();
                    Thread.Sleep(250);
                    button.TurnLEDOff();
                    Thread.Sleep(250);
                }
                return;
            }

            _isArmed = true;
            button.TurnLEDOn();
            multicolorLed1.TurnOff();
            multicolorLed2.TurnOff();
            SendToThingSpeak("Traps armed.");
            _trapTimer.Start();
            UpdateDisplay("Traps armed. Exterminating.");
            Thread.Sleep(10000);
            display.SetBacklight(true);
        }

        private void MouseCaught(int trapNum)
        {
            var statusMsg = "Trap #" + trapNum + " exterminated a mouse!";
            UpdateDisplay(statusMsg);
#if DEBUG
            Debug.Print(statusMsg);
#endif
            _isArmed = false;
            button.TurnLEDOff();

            // Blink a light...
            switch (trapNum)
            {
                case 1:
                    multicolorLed1.BlinkRepeatedly(Gadgeteer.Color.Orange);
                    break;
                case 2:
                    multicolorLed2.BlinkRepeatedly(Gadgeteer.Color.Orange);
                    break;
            }

            SendToThingSpeak(statusMsg, statusMsg);
        }

        private void UpdateDisplay(string line1 = null, string line2 = null)
        {
            display.Clear();
            if(line1 != null) _displayLine[0] = line1;
            if (line2 != null) _displayLine[1] = line2;

            // THIS WORKS
            display.PrintString((_displayLine[0] ?? ""));
            display.SetCursor(1, 0);
            display.PrintString(_displayLine[1] ?? "");

            // THIS CORRUPTS THE FIRMWARE (I'm using the ethernet firmware)
            //display.PrintString(_displayLine[0] + "\r" + _displayLine[1]);
        }

        private void InitializeNetwork_Dhcp()
        {
            try
            {
                var nis = NetworkInterface.GetAllNetworkInterfaces();

                if (nis == null || nis.Length == 0)
                {
                    throw new IndexOutOfRangeException("No network interface was found.  Make sure you have the ethernet firmware installed and a NIC attached.");
                }
                _ni = nis[0];
                _ni.PhysicalAddress = _macAddress;
                _ni.EnableDhcp();          

                // There's a bug in the current SDK.  If connection becomes available after startup then IPAddress does not seem to get populated.
                while (_ni.IPAddress == "0.0.0.0")
                {
#if DEBUG
                    Debug.Print("Waiting for IP address...");
#endif
                    Thread.Sleep(1000);
                }
#if DEBUG
                Debug.Print("Network ready.");
                Debug.Print("  IP Address: " + _ni.IPAddress);
                Debug.Print("  Subnet Mask: " + _ni.SubnetMask);
                Debug.Print("  Default Getway: " + _ni.GatewayAddress);
                Debug.Print("  MAC Address: " + _ni.PhysicalAddress.ToString());
#endif
                UpdateDisplay("IP: " + _ipAddress);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("Could not resolve host via DNS.", exception);
            }
        }

        private static void InitializeNetwork_Static()
        {
            string myIP = "192.168.1.99";
            string subnetMask = "255.255.255.0";
            string gatewayAddress = "192.168.1.1";
            string dnsAddresses = "192.168.1.1";

            Debug.Print("Initializing network...");
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            if (interfaces != null && interfaces.Length > 0)
            {
                _ni = interfaces[0];

                Debug.Print("Setting static IP...");
                _ni.EnableStaticIP(myIP, subnetMask, gatewayAddress);
                _ni.PhysicalAddress = _macAddress;
                _ni.EnableStaticDns(new[] { dnsAddresses });

                Debug.Print("Network ready.");
                Debug.Print(" IP Address: " + _ni.IPAddress);
                Debug.Print(" Subnet Mask: " + _ni.SubnetMask);
                Debug.Print(" Default Gateway: " + _ni.GatewayAddress);
                Debug.Print(" DNS Server: " + _ni.DnsAddresses[0]);
            }
            else
            {
                Debug.Print("No network device found.");
            }
        }

        private void SendToThingSpeak(string statusMsg, string tweet = null)
        {
            _thingSpeakDataEntry.Fields[0] = _trap1Status.Read() ? "1" : "0";
            _thingSpeakDataEntry.Fields[1] = _trap2Status.Read() ? "1" : "0";
            _thingSpeakDataEntry.Tweet = tweet;
            _thingSpeakDataEntry.Status = statusMsg;
            _thingSpeakDataEntry.DateTime = DateTime.Now;

            try
            {
                _thingSpeakDataEntry.Twitter = _thingSpeakDataEntry.Tweet == null ? null : TWITTER_USER_NAME;
                _thingSpeakClient.Update(NO_GIT.THINGSPEAK_WRITE_API_KEY, _thingSpeakDataEntry); //NOTE ThingSpeak is FREE
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        public static bool NtpTime(string timeServer, int gmtOffset = 0)
        {
            Socket s = null;
            try
            {
                EndPoint rep = new IPEndPoint(Dns.GetHostEntry(timeServer).AddressList[0], 123);
                s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                var ntpData = new byte[48];
                Array.Clear(ntpData, 0, 48);
                ntpData[0] = 0x1B; // Set protocol version
                s.SendTo(ntpData, rep); // Send Request   
                if (s.Poll(30 * 1000 * 1000, SelectMode.SelectRead)) // Waiting an answer for 30s, if nothing: timeout
                {
                    s.ReceiveFrom(ntpData, ref rep); // Receive Time
                    const byte offsetTransmitTime = 40;
                    ulong intpart = 0;
                    ulong fractpart = 0;
                    for (var i = 0; i <= 3; i++) intpart = (intpart << 8) | ntpData[offsetTransmitTime + i];
                    for (var i = 4; i <= 7; i++) fractpart = (fractpart << 8) | ntpData[offsetTransmitTime + i];
                    ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);
                    s.Close();
                    var dateTime = new DateTime(1900, 1, 1) + TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
                    var localTime = dateTime.AddMinutes(gmtOffset);
                    Microsoft.SPOT.Hardware.Utility.SetLocalTime(localTime);
#if DEBUG
                    Debug.Print("Local time set to: " + localTime);
#endif
                    return true;
                }
                s.Close();
            }
            catch
            {
                try
                {
                    s.Close();
                }
                catch
                {
                }
            }
            return false;
        }
    }
}
