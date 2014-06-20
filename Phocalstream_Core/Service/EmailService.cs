using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Service.Service
{
    public class EmailService
    {
        public static void SendMail(string emailAddress, string Subject, string Body)
        {
            MailMessage msg = new MailMessage();
            msg.IsBodyHtml = true;

            msg.To.Add(new MailAddress(emailAddress));
            msg.From = new MailAddress(ConfigurationManager.AppSettings["smtpFrom"]);
            msg.Subject = Subject;
            msg.Body = Body;
            
            SmtpClient smtp = new SmtpClient(ConfigurationManager.AppSettings["smtpClient"], Convert.ToInt16(ConfigurationManager.AppSettings["smtpPort"]));
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["smtpUsername"], ConfigurationManager.AppSettings["smtpPassword"]);

            smtp.Send(msg);
        }
    }
}
