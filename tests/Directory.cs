using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

using System.Xml;
using System.Configuration;

namespace Weavver.Testing.Vendors.FreeSwitch
{
//-------------------------------------------------------------------------------------------
     //[TestFixture]
     public partial class Directory
     {
          [StagingTest]
          public void RunTest()
          {
               string weavverurl = ConfigurationManager.AppSettings["weavver_url"];
               
               string queryUrl = "$weavverurl/vendors/freeswitch/directory";
               queryUrl = queryUrl.Replace("$weavverurl", weavverurl);
               System.Net.HttpWebRequest req = (HttpWebRequest) System.Net.HttpWebRequest.Create(queryUrl);
               WebResponse wr = req.GetResponse();

               StreamReader reader = new StreamReader(wr.GetResponseStream());
               string response = reader.ReadToEnd();

               Assert.AreEqual(response, "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>\r\n" +
                    "<document type=\"freeswitch/xml\"><section name=\"result\"><result status=\"not found\" /></section></document>", "Not found xml is invalid");

               string sipusername = ConfigurationManager.AppSettings["vendors:freeswitch_sipusername"];
               string sippasswordhash = ConfigurationManager.AppSettings["vendors:freeswitch_sippasswordhash"];

               queryUrl = "$weavverurl/vendors/freeswitch/directory?hostname=testserver&section=directory&tag_name=domain&key_name=name&key_value=202.123.111.22&Event-Name=REQUEST_PARAMS&Core-UUID=5f8e775c-346f-466e-a11f-44614588e52e&FreeSWITCH-Hostname=testserver&FreeSWITCH-Switchname=testserver&FreeSWITCH-IPv4=333.222.111.222&FreeSWITCH-IPv6=2002%3acd86%3ae117%3a%3acd86%3ae117&Event-Date-Local=2012-07-22+23%3a47%3a40&Event-Date-GMT=Mon%2c+23+Jul+2012+06%3a47%3a40+GMT&Event-Date-Timestamp=1343026060222421&Event-Calling-File=sofia_reg.c&Event-Calling-Function=sofia_reg_parse_auth&Event-Calling-Line-Number=2301&Event-Sequence=143807&action=sip_auth&sip_profile=internal&sip_user_agent=Zoiper+rev.11137&sip_auth_username=$username&sip_auth_realm=192.168.10.6&sip_auth_nonce=2d096a65-b37b-47d2-b306-77f4cee10a62&sip_auth_uri=sip%3a9996%40192.168.10.6%3btransport%3dUDP&sip_contact_user=$username&sip_contact_host=76.95.6.194&sip_to_user=9996&sip_to_host=192.168.10.6&sip_from_user=$username&sip_from_host=192.168.10.6&sip_request_user=9996&sip_request_host=192.168.10.6&sip_auth_qop=auth&sip_auth_cnonce=3f437e297beef709984bc55b26b37468&sip_auth_nc=00000001&sip_auth_response=32ab6e76d423928c01a3bba492bb2eac&sip_auth_method=INVITE&key=id&user=$username&domain=111.222.333.444&ip=192.168.10.201";
               queryUrl = queryUrl.Replace("$weavverurl", weavverurl);
               queryUrl = queryUrl.Replace("$username", sipusername);

               req = (HttpWebRequest)System.Net.HttpWebRequest.Create(queryUrl);
               wr = req.GetResponse();
               StreamReader reader2 = new StreamReader(wr.GetResponseStream());
               string response2 = reader2.ReadToEnd();

               XmlDocument doc = new XmlDocument();
               doc.LoadXml(response2);
               
               var userAttribute = doc.DocumentElement.SelectSingleNode("//user[@id = '" + sipusername + "']/@id");
               Assert.AreEqual(sipusername, userAttribute.Value, "The sip ID passed back does not match.");

               var passAttribute = doc.DocumentElement.SelectSingleNode("//param[@name = 'a1-hash']/@value");
               Assert.AreEqual(sippasswordhash, passAttribute.Value, "The served up password does not match.");
          }
     }
//-------------------------------------------------------------------------------------------
}