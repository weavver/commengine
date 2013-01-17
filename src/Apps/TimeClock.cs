using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreeSWITCH;
using FreeSWITCH.Native;
using System.Net.Mail;
using System.Configuration;
using Weavver;
//using Weavver.Company;
//using Weavver.Security;

namespace Weavver.Interfaces.CommEngine
{
     public class TimeClock : IAppPlugin
     {
          //FreeSWITCH.Native.switch_dialplan_interface dialpan = new switch_dialplan_interface();          
          
          string buffer = "";

          #region variables
          // Environment Variables
           string _emplId = ConfigurationManager.AppSettings["EmplID"];
           string _password = ConfigurationManager.AppSettings["Password"];
           int _maxTries = int.Parse(ConfigurationManager.AppSettings["MaxTries"]);
           int _timeout = int.Parse(ConfigurationManager.AppSettings["Timeout"]);

           // Log Settings
           string _logFile = ConfigurationManager.AppSettings["LogFile"];

           // Email Settings
           string _subject = ConfigurationManager.AppSettings["Subject"];
           string _message = ConfigurationManager.AppSettings["Message"];
           string _smtpClient = ConfigurationManager.AppSettings["SMTPClient"];
           string _fromEmail = ConfigurationManager.AppSettings["FromEmail"];
           string _toEmail = ConfigurationManager.AppSettings["ToEmail"];

           // FreeSWITCH Variables
           string _callerIDNumber = ConfigurationManager.AppSettings["CallerIDNumber"];
           string _executeState = ConfigurationManager.AppSettings["ExecuteState"];
           string _playbackCommand = ConfigurationManager.AppSettings["PlaybackCommand"];

           // Audio Prompts
           string _welcome = ConfigurationManager.AppSettings["Welcome"];
           string _enterID = ConfigurationManager.AppSettings["EnterID"];
           string _badId = ConfigurationManager.AppSettings["BadID"];
           string _enterPasscode = ConfigurationManager.AppSettings["EnterPasscode"];
           string _badPasscode = ConfigurationManager.AppSettings["BadPasscode"];
           string _successfulLogin = ConfigurationManager.AppSettings["SuccesfulLogin"];
           string _punchIn = ConfigurationManager.AppSettings["PunchIn"];
           string _successfulPunchIn = ConfigurationManager.AppSettings["SuccessfulPunchIn"];
           string _punchOut = ConfigurationManager.AppSettings["PunchOut"];
           string _successfulPunchOut = ConfigurationManager.AppSettings["SuccessfulPunchOut"];
           string _absence = ConfigurationManager.AppSettings["Absence"];
           string _absenceRecorded = ConfigurationManager.AppSettings["AbsenceRecorded"];
          #endregion
//-------------------------------------------------------------------------------------------
           //        const string ConfirmationKey = "1";
           private void sendEmail(string phoneNumber)
           {
                try
                {
                     SmtpClient smtpClient = new SmtpClient(_smtpClient);
                     MailMessage msg = new System.Net.Mail.MailMessage(_fromEmail, _toEmail);
                     msg.Priority = MailPriority.Normal;
                     msg.IsBodyHtml = false;
                     //msg.Bcc.Add(ConfigurationManager.AppSettings["admin_address"]);
                     msg.Body = _message + phoneNumber;
                     msg.Subject = _subject;
                     smtpClient.Send(msg);
                }
                catch (Exception ex)
               
                {
                     WriteToLog(ex.ToString());
                }
           }
//-------------------------------------------------------------------------------------------
          public void Run(AppContext context)
          {
               Weavver.Security.WeavverMembershipProvider wmp = new Weavver.Security.WeavverMembershipProvider();
               Weavver.Data.System_User user = new Weavver.Data.System_User();
               //Weavver.Company.HumanResources.Common hr = new Weavver.Company.HumanResources.Common();

               Guid employeeID = Guid.Empty;
               Guid timeLogId = Guid.Empty;

               string emplId = string.Empty;
               string password = string.Empty;
               string prompt = string.Empty;
               string response = string.Empty;
               string confirmation = string.Empty;

               int selection = 0;

               bool loggedIn = false;
               bool punchedIn = false;

               try
               {
                    var session = context.Session;

                    sendEmail(session.GetVariable(_callerIDNumber));

                    session.Answer();
                    //sendEmail(session.ge
                    session.Execute(_playbackCommand, _welcome);
                    //WriteToLog("Answered Phone: " + session.GetVariable("orig_addr"));               
                    // get emplid
                    //WriteToLog(session.getState());
                    emplId = PlayAndGet(ref session, 5, _enterID, "");
                    while (!loggedIn)
                    {
                         while (emplId != _emplId && session.getState() == _executeState)
                         {
                              WriteToLog(session.getState());
                              emplId = PlayAndGet(ref session, 5, _badId, "");
                         }

                         // get password
                         password = PlayAndGet(ref session, 4, _enterPasscode, "");
                         while (password != _password && session.getState() == _executeState)
                         {
                              WriteToLog(session.getState());
                              password = PlayAndGet(ref session, 4, _badPasscode, "");

                              // TODO: Uncomment for live implementation
                              /*
                              //Verify Security
                              if (wmp.LogIn_byPhoneNumber(emplId, password, out user))
                                   loggedIn = true;
                              */
                         }
                         // TODO:  Remove for live implementation
                         loggedIn = true;
                    }

                    // TODO: Uncomment for live implementation
                    //punchedIn = Weavver.Company.HumanResources.Common.IsPunchedIn(user.Id);

                    // enter prompt loop
                    session.Execute(_playbackCommand, _successfulLogin);
                    while (session.getState() == _executeState)
                    {
                         WriteToLog(session.getState());
                         if (!punchedIn)
                         {
                              selection = int.Parse(PlayAndGet(ref session, 1, _punchIn, ""));
                              switch (selection)
                              {
                                   case 1:
                                        // TODO: Uncomment for live implementation
                                        //Weavver.Company.HumanResources.Common.PunchIn(user.OrganizationId, user.Id);
                                        punchedIn = true;
                                        session.Execute(_playbackCommand, _successfulPunchIn);
                                        break;
                                   case 2:
                                        // TODO: Implement absence handling in Weavver
                                        string absence = PlayAndGet(ref session, 1, _absence, "");
                                        if (absence.Length == 1)
                                             session.Execute(_playbackCommand, _absenceRecorded);
                                        break;
                              }
                         }
                         else
                         {
                              selection = int.Parse(PlayAndGet(ref session, 1, _punchOut, ""));
                              switch (selection)
                              {
                                   case 1:
                                        // TODO: Uncomment for live implementation
                                        //Weavver.Company.HumanResources.Common.PunchOut(user.OrganizationId, user.Id);
                                        punchedIn = true;
                                        session.Execute(_playbackCommand, _successfulPunchOut);
                                        break;
                              }
                         }
                    }

                    session.HangupFunction();
               }
               catch (Exception ex)
               {
                    WriteToLog(ex.ToString());
               }
          }
//-------------------------------------------------------------------------------------------
          public string PlayAndGet(ref FreeSWITCH.Native.ManagedSession session, int digits, string promptFile, string failFile)
          {
               if (session.getState() == _executeState)
                    return session.PlayAndGetDigits(digits, digits, _maxTries, _timeout, "", promptFile, failFile, "", "response", 10000, "0");
               else
                    return string.Empty;
          }
//-------------------------------------------------------------------------------------------
          public void Buffer(char arg1, TimeSpan arg2)
          {
              // obj.Assert("xyz");
          }
//-------------------------------------------------------------------------------------------
          public void WriteToLog(string txt)
          {
               if (txt!= _executeState)
                    System.IO.File.AppendAllText(_logFile, txt + System.Environment.NewLine);
          }
//-------------------------------------------------------------------------------------------
          public void WriteToLog(string phonenumber, string password)
          {
               System.IO.File.AppendAllText(_logFile, "Phone #:  " + phonenumber.PadRight(10) + "\t" + System.DateTime.Now.ToString() + System.Environment.NewLine);
               System.IO.File.AppendAllText(_logFile, "Password: " + password.PadRight(10) + "\t" + System.DateTime.Now.ToString() + System.Environment.NewLine);
          }
//-------------------------------------------------------------------------------------------
     }
}
