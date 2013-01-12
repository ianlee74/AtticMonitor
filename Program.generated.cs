﻿
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Gadgeteer Designer.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Gadgeteer;
using GTM = Gadgeteer.Modules;

namespace IanLee.AtticMonitor
{
    public partial class Program : Gadgeteer.Program
    {
        // GTM.Module definitions
        Gadgeteer.Modules.GHIElectronics.Button button;
        Gadgeteer.Modules.GHIElectronics.MulticolorLed multicolorLed1;
        Gadgeteer.Modules.GHIElectronics.MulticolorLed multicolorLed2;
        Gadgeteer.Modules.GHIElectronics.Extender extender;
        Gadgeteer.Modules.GHIElectronics.Display_HD44780 display;

        public static void Main()
        {
            //Important to initialize the Mainboard first
            Mainboard = new GHIElectronics.Gadgeteer.FEZCerberus();			

            Program program = new Program();
            program.InitializeModules();
            program.ProgramStarted();
            program.Run(); // Starts Dispatcher
        }

        private void InitializeModules()
        {   
            // Initialize GTM.Modules and event handlers here.		
            button = new GTM.GHIElectronics.Button(2);
		
            extender = new GTM.GHIElectronics.Extender(3);
		
            display = new GTM.GHIElectronics.Display_HD44780(4);
		
            multicolorLed1 = new GTM.GHIElectronics.MulticolorLed(7);
		
            multicolorLed2 = new GTM.GHIElectronics.MulticolorLed(multicolorLed1.DaisyLinkSocketNumber);

        }
    }
}
