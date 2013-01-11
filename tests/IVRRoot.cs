using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Weavver.Vendors.FreeSWITCH;
using System.Configuration;

namespace Weavver.Testing.CommEngine
{
     class IVRRoot
     {
          public void RunTests()
          {
               string freeSwitchServer = ConfigurationManager.AppSettings["vendors:freeswitch_server"];
               string freeSwitchApiKey = ConfigurationManager.AppSettings["vendors:freeswitch_apikey"];

               FreeSwitchConnection cEngine = new FreeSwitchConnection();
               cEngine.ConnectFreeSwitch(freeSwitchServer, 8021, freeSwitchApiKey);

               // Call the IVR - ext 5000 drops us into the Root IVR
               cEngine.Send("api originate loopback/virtualSoftphone/qa 5000 xml internal\n\n");

               //cEngine.SendDTMF(newChanUUID, "3");

               //// expect "Welcome to Sprockets, Inc.s Payment system.";
               //// expect "Please enter your account ID:";
               //cEngine.Expect("Application: managed\n" +
               //               "Application-Data: ReceivePayment\n\n");
          }
     }
}
