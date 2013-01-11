using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Weavver.Vendors.FreeSWITCH;
using System.Configuration;

namespace Weavver.Testing.CommEngine
{
     public class ReceivePayment
     {
          [StagingTest]
          public void RunTests()
          {
               string freeSwitchServer = ConfigurationManager.AppSettings["vendors:freeswitch_server"];
               string freeSwitchApiKey = ConfigurationManager.AppSettings["vendors:freeswitch_apikey"];

               FreeSwitchConnection cEngine = new FreeSwitchConnection();
               cEngine.ConnectFreeSwitch(freeSwitchServer, 8021, freeSwitchApiKey);

               // Call the IVR
               cEngine.Send("api originate loopback/virtualSoftphone/qa 7968 xml internal\n\n");

               FreeSwitchPacket chanCreate = cEngine.Expect("variable_other_loopback_leg_uuid");
               string newChanUUID = chanCreate["variable_other_loopback_leg_uuid"];

               // Welcome to the Weavver Account Payment System demo, pLease enter your ID followed by Pound.
               cEngine.Expect("Application: playback\n" +
                              "Application-Data: receivepayment%5CWelcomeToTheAccountPaymentSystem.wav\n" +
                              "Application-Response: FILE%20PLAYED\n");

               //cEngine.Expect("wvvr-CC-enterID.wav\n" +
               //               "Playback-Status: done\n\n");

               //// expect "you have sent the wrong code"
               //// expect "Please enter your account ID:"
               //// HACK fix audio message to remove "#"
               //cEngine.SendDTMF(newChanUUID, "12345");
               //// expect "Please enter your passcode:"
               //cEngine.SendDTMF(newChanUUID, "0000");
               //// expect: "Your balance is.";

               cEngine.Hangup(newChanUUID);
          }
     }
}
