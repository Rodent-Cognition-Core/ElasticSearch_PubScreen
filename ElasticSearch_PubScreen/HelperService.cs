using System;
using System.Net.Mail;

namespace CBAS.Helpers
{
    public class HelperService
    {
        public static int? ConvertToNullableInt(string s)
        {
            int i;
            if (int.TryParse(s, out i)) return i;
            return null;
        }

        public static string NullToString(object Value)
        {

            return Value == null ? "" : Value.ToString();

        }

        public static DateTime? ConvertToNullableDateTime(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }
            else
            {
                return DateTime.Parse(s).ToLocalTime();
            }
        }

        public static string EscapeSql(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }
            s = s.Replace("'", @"''");
            return s;
        }

        public static bool SendEmail(string fromEmailAddress, string toEmailAddress, string subject, string body)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.office365.com");

                if (fromEmailAddress == "")
                {
                    fromEmailAddress = "mousebytes@uwo.ca";
                }

                if (toEmailAddress == "")
                {
                    toEmailAddress = "mousebytes@uwo.ca";
                }

                mail.From = new MailAddress(fromEmailAddress);
                mail.To.Add(toEmailAddress);
                mail.Subject = subject;
                mail.IsBodyHtml = true;
                mail.Body = body;

                SmtpServer.Port = 587;

                SmtpServer.Credentials = new System.Net.NetworkCredential("mousebyt@uwo.ca", "");

                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

    }
}
