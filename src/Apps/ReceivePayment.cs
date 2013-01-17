using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;

using FreeSWITCH;
using FreeSWITCH.Native;

using Weavver.Data;
using AuthorizeNet;

namespace Weavver.Interfaces.CommEngine
{
     public class ReceivePayment : IAppPlugin
     {
          FreeSWITCH.Native.switch_dialplan_interface dialpan = new switch_dialplan_interface();

          const string logFile = @"C:\Weavver\Main\Projects\CommEngine\bin\PlasticOutput.log";
//-------------------------------------------------------------------------------------------
          /// <summary>
          /// Payment processing IVR              
          /// </summary>
          /// <param name="context"></param>
          public void Run(AppContext context)
          {
               WeavverEntityContainer data = new WeavverEntityContainer();
               Logistics_Organizations org = null;

               var session = context.Session;
               try
               {
                    session.Answer();

Start:
                    // Welcome to the account payment system.
                    session.Execute("Playback", @"receivepayment\WelcomeToTheAccountPaymentSystem.wav");

                    // Figure out the caller's balance
                    decimal Balance = 0.0m;
                    bool hasBalance = Decimal.TryParse(session.GetVariable("Balance"), out Balance);
                    if (!hasBalance)
                    {
                         string OrganizationId = session.GetVariable("OrganizationId");
                         if (String.IsNullOrEmpty(OrganizationId))
                         {
LogIn:
                              string UserCode = GetVariable(ref session, "UserCode", "number iterated", null);
                              string PassCode = GetVariable(ref session, "PassCode", "number iterated", null);
                           
                              var user = (from x in data.System_Users
                                             where x.UserCode == UserCode &&
                                                  x.PassCode == PassCode
                                             select x).FirstOrDefault();

                              if (user != null)
                              {
                                   OrganizationId = user.OrganizationId.ToString();
                              }
                              else if (session.getState() == "CS_EXECUTE")
                              {
                                   session.Execute("Playback", @"receivepayment\AccountNotFound.wav");
                                   goto LogIn;
                              }
                              else
                              {
                                   session.SetVariable("PaymentStatus", "Failed");
                                   session.SetVariable("ResponseCode", "0");
                                   session.SetVariable("Message", "User did not log-in.");
                                   return;
                              }
                         }

                         Guid orgGuid = new Guid(OrganizationId);
                         org = (from orgs in data.Logistics_Organizations
                                where orgs.OrganizationId == orgGuid
                                select orgs).FirstOrDefault();

                         Balance = org.ReceivableBalance.Value;
                    }

                    // Play: Your account balance is:
                    session.Execute("Playback", @"receivepayment\YourAccountBalanceIs.wav");
                    session.Execute("Say", "en currency pronounced masculine " + Balance);

                    decimal PaymentAmount = Balance;
                    bool customPayment = false;
                    if (Balance == 0m)
                    {
                         string balanceIsEvenOption = PlayAndGet(ref session, 1, 1, @"YourBalanceIs0", null);
                         if (balanceIsEvenOption == "1")
                         {
                              customPayment = true;
                         }
                         else
                         {
                              session.SetVariable("PaymentStatus", "Failed");
                              session.SetVariable("ResponseCode", "0");
                              session.SetVariable("Message", "User has no balance to pay.");
                              return;
                         }
                    }
                    else
                    {
                         // Press 1 to pay your balance in full or 2 to enter another amount.
                         string entered = PlayAndGet(ref session, 1, 1, "Press1Or2", "^[1-2]$");
                         if (entered.Trim() == "2")
                         {
                              customPayment = true;
                         }
                    }
                    if (customPayment)
                    {
                         string enteredAmount = GetVariable(ref session, "PaymentAmount", "currency pronounced", null);
                         Decimal.TryParse(enteredAmount, out PaymentAmount);
                    }
                    string CardNumber     = GetVariable(ref session, "CardNumber", "number iterated", "^(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|6(?:011|5[0-9][0-9])[0-9]{12}|3[47][0-9]{13})$");
                    string CardExpiration = GetVariable(ref session, "CardExpiration", "number iterated", "^[0-9][0-9][0-9][0-9]$"); // MMDD
                    string CardCode       = GetVariable(ref session, "CardCode", "number iterated", "^[0-9][0-9][0-9][0-9]|[0-9][0-9][0-9]$"); // 1234|123

                    // Process Card
                    session.Execute("Playback", @"receivepayment\ProcessingPayment.wav");

                    var request = new AuthorizationRequest(CardNumber, CardExpiration, PaymentAmount, "Phone Payment", true);
                    //request.AddCustomer("", org.BillingAddressFK.Name, order.PrimaryContactNameLast, primaryAddress.Line1, primaryAddress.State, primaryAddress.ZipCode);

                    string referenceId = session.GetVariable("ReferenceId");
                    if (!String.IsNullOrEmpty(referenceId))
                    {
                         request.AddMerchantValue("ReferenceId", referenceId);
                    }
                    if (org != null)
                    {
                         request.AddMerchantValue("OrganizationId", org.Id.ToString());
                    }
                    var gate = new Gateway(ConfigurationManager.AppSettings["authorize.net_loginid"],
                                           ConfigurationManager.AppSettings["authorize.net_transactionkey"],
                                          // we use the reverse negative of testmode for safety in case the setting is missing from the app.config
                                          (ConfigurationManager.AppSettings["authorize.net_testmode"] != "false")
                                          );

                    var response = gate.Send(request, "Phone Payment");
                    if (response.Approved)
                    {
                         session.SetVariable("PaymentStatus", "Success");
                         session.SetVariable("PaymentAmount", PaymentAmount.ToString());
                         session.SetVariable("TransactionId", response.TransactionID);

                         session.Execute("Playback", @"receivepayment\PaymentSuccessYourTransactionCodeIs.wav");
                         session.Execute("Say", "en name_spelled iterated masculine " + response.TransactionID);
                    }
                    else
                    {
                         session.SetVariable("PaymentStatus", "Failed");
                         session.SetVariable("PaymentAmount", "0");
                         session.SetVariable("ResponseCode", response.ResponseCode);
                         session.SetVariable("Message", response.Message);

                         if (PlayAndGet(ref session, 1, 1, "PaymentFailure", null) == "1")
                         {
                              session.Execute("Playback", @"receivepayment\ThankYou.wav");

                              if (session.getState() == "CS_EXECUTE")
                                   goto Start;
                              else
                                   return;
                         }
                    }
               }
               catch (Exception ex)
               {
                    Debug(ref session, ex.Message);

                    session.SetVariable("PaymentStatus", "Failed");
                    session.SetVariable("Message", ex.Message);

                    WriteToLog("SystemError: " + ex.ToString());
                    session.Execute("Playback", @"receivepayment\SystemError.wav");
               }
          }
//-------------------------------------------------------------------------------------------
          private void SendLead(string phoneNumber)
          {
               try
               {
                    string message = "New Credit Card Lead: " + phoneNumber;
                    SmtpClient smtpclient = new SmtpClient(ConfigurationManager.AppSettings["smtp_server"]);
                    MailMessage msg = new MailMessage(ConfigurationManager.AppSettings["smtp_from"], ConfigurationManager.AppSettings["smtp_leadsto"]);
                    msg.Priority = MailPriority.Normal;
                    msg.IsBodyHtml = false;
                    //msg.Bcc.Add(ConfigurationManager.AppSettings["admin_address"]);
                    msg.Body = message;
                    msg.Subject = "New Lead for CC IVR";
                    smtpclient.Send(msg);
               }
               catch (Exception ex)
               {
                    WriteToLog(ex.ToString());
               }
          }
//-------------------------------------------------------------------------------------------
          public void WriteToLog(string txt)
          {
               try
               {
                    if (txt != "CS_EXECUTE")
                         System.IO.File.AppendAllText(logFile, txt + System.Environment.NewLine);
               }
               catch { }
          }
//-------------------------------------------------------------------------------------------
          public string GetVariable(ref FreeSWITCH.Native.ManagedSession session, string settingName, string expectedType, string regex)
          {
               Debug(ref session, session.HookState.ToString());
               string value = session.GetVariable(settingName);
               if (!String.IsNullOrWhiteSpace(value))
               {
                    session.Execute("log", "INFO Loaded pre-set variable \"" + settingName + "\": '" + value + "'");
                    return value;
               }

               string entered = null;
               while (true)
               {
                    entered = PlayAndGet(ref session, 1, 20, settingName, null).Replace("*", ".");
                    WriteToLog("Entered: " + entered);
                    if (regex == null || Regex.Match(entered, regex).Success)
                    {
                         session.Execute("Playback", @"receivepayment\YouEntered.wav");
                         session.Execute("Say", "en " + expectedType + " masculine " + entered);

                         if (PlayAndGet(ref session, 1, 1, "IsThisCorrect", null) == "1")
                         {
                              session.Execute("Playback", @"receivepayment\ThankYou.wav");
                              break;
                         }
                    }
                    else
                    {
                         string failFile = @"receivepayment\" + settingName + "-TryAgain.wav";
                         session.Execute("Playback", failFile);
                    }
               }
               return entered;
          }
//-------------------------------------------------------------------------------------------
          public string PlayAndGet(ref FreeSWITCH.Native.ManagedSession session, int mindigits, int maxdigits, string promptFile, string regex)
          {
               string playFile = @"receivepayment\" + promptFile + ".wav";
               string failFile = @"receivepayment\" + promptFile + "-TryAgain.wav";

               if (session.getState() == "CS_EXECUTE")
               {
                    return session.PlayAndGetDigits(mindigits, maxdigits, 5, 7000, "#", playFile, failFile, regex, "response", 10000, null);
               }
               else
               {
                    throw new Exception("Call is not in the proper state: " + session.getState());
               }
          }
//-------------------------------------------------------------------------------------------
          public void Debug(ref FreeSWITCH.Native.ManagedSession session, string output)
          {
               session.Execute("log", "INFO " + output);
          }
//-------------------------------------------------------------------------------------------
     }
}
