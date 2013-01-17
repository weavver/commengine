using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FreeSWITCH.Native;
using System.Net.Mail;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace Weavver.Interfaces.CommEngine
{
     public class Program
     {
          private IDisposable search_bind;
          private IDisposable event_bind;
//-------------------------------------------------------------------------------------------
          static void Main(string[] args)
          {
               Program p = new Program();
               p.Start();
          }
//-------------------------------------------------------------------------------------------
          public void Start()
          {
               Stopwatch sw = new Stopwatch();
               sw.Start();
               String err = "";
               const uint flags = (uint)(switch_core_flag_enum_t.SCF_USE_SQL | switch_core_flag_enum_t.SCF_USE_NAT_MAPPING);
               freeswitch.switch_core_set_globals();
               freeswitch.switch_core_init(flags, switch_bool_t.SWITCH_FALSE, ref err);
               //search_bind = FreeSWITCH.SwitchXmlSearchBinding.Bind(xml_search, switch_xml_section_enum_t.SWITCH_XML_SECTION_CONFIG);
               event_bind = FreeSWITCH.EventBinding.Bind("WeavverCommEngine", switch_event_types_t.SWITCH_EVENT_ALL, null, FreeSwitch_Events, true);
               freeswitch.switch_core_init_and_modload(flags, switch_bool_t.SWITCH_FALSE, ref err);
               sw.Stop();

               if (err == "Success")
                    Console.WriteLine("Passed Init");

               Console.ReadLine();
               freeswitch.switch_core_destroy();
          }
//-------------------------------------------------------------------------------------------
          private void FreeSwitch_Events(FreeSWITCH.EventBinding.EventBindingArgs args)
          {
               switch_event evt = args.EventObj;
               try
               {
                    Console.WriteLine("Event " + evt.event_id + ":" + Path.GetFileName(evt.last_header.value));// +blockConsole.Text
               }
               catch { }

               if (evt.event_id.ToString() == "SWITCH_EVENT_CUSTOM" &&
                   evt.last_header.value == "UP")
                    Console.WriteLine("Switch Started");
          }
//-------------------------------------------------------------------------------------------
          private string xml_search(FreeSWITCH.SwitchXmlSearchBinding.XmlBindingArgs args)
          {
               return null;
          }
//-------------------------------------------------------------------------------------------
     }
}
