#define DEBUG
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using System.Net.Mail;
using System.Web;
using System.Net;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.IO;
using System.Globalization;

namespace PickupReminder
{
    class clsReminder
    {
        static int total_reminders = 0;
        static int total_sms_sent = 0;
        static int total_email_sent = 0;
        static int total_ignored = 0;
        static int total_failed = 0;
        static string strapi_partners = System.Configuration.ConfigurationSettings.AppSettings["API_PARTNERS"];
        static int[] api_partners = Array.ConvertAll(strapi_partners.Split(','), s => int.Parse(s));

        static void Main(string[] args)
        {
            /* Fetch pickups in the range 1hour to 2hour from now.*/
            clsDB db = new clsDB();
            db.ExecuteDML("RMS_UPDATE_PICKUP_REMINDERS", CommandType.StoredProcedure, 160);
            DataSet ds = db.ExecuteSelect("RMS_GET_PENDING_REMINDERS", CommandType.StoredProcedure, 160);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                total_reminders = ds.Tables[0].Rows.Count;
                Console.WriteLine("Processing " + total_reminders.ToString() + " reminders");
                for (int i = 0; i < total_reminders; i++)
                {
                    DataRow row = ds.Tables[0].Rows[i];
                    process(row);
                }
            }
            Console.WriteLine("Mismatch-" + total_reminders.ToString() + 
                                " SMS-" + total_sms_sent.ToString() +
                                " EMAIL-" + total_email_sent.ToString() +
                                " Ignored-" + total_ignored.ToString() + 
                                " Failed-" + total_failed.ToString());
        }
 
        private static void process(DataRow row)
        {
            int user_id = Convert.ToInt32(row["user_id"].ToString());
            string type = "";
            if(api_partners.Contains(user_id))
            {
                Console.WriteLine("Ignored");
                total_ignored += 1;
                update_informed_status(Convert.ToInt32(row["booking_id"].ToString()), -1, -1);
                return;
            }

            if(user_id==333 || user_id == 4642)
                type = "TY";
            else
                type = "GDS";

            try
            {
                int sms_status = 0;
                int email_status = 0;
                if (Convert.ToInt32(row["sms_sent"]) == 0)
                {
                    sms_status = process_sms(row, type);
                    Console.WriteLine("SMS sent");
                }
                else
                {
                    sms_status = 1;
                    Console.WriteLine("SMS was sent");
                }
                if (Convert.ToInt32(row["email_sent"]) == 0)
                {
                    email_status = process_email(row);
                    total_email_sent += email_status;
                    Console.WriteLine("Email sent");
                }
                else
                {
                    email_status = 1;
                    Console.WriteLine("Email was sent");
                }

                // Update sms and email status in pickup_reminder
                #if !DEBUG
                    update_informed_status(Convert.ToInt32(row["booking_id"].ToString()), sms_status, email_status);
                #endif

            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Process failed");
                total_failed += 1;
            }
        }

        private static int process_sms(DataRow row, string type)
        {
            int sms_status = 0;
            int booking_id = Convert.ToInt32(row["booking_id"].ToString());
            string pnr = row["pnr"].ToString();
            string tno = row["ticket_no"].ToString();
            string pickup = row["pickup"].ToString();
            string mobile = Convert.ToString(row["mobile"]);
            #if DEBUG
            mobile = "7676036717"; // Amritesh Anand Mobile Number
            #endif
            Dictionary<string, string> content = new Dictionary<string, string>();
            content["fcity"] = row["fcity"].ToString();
            content["tcity"] = row["tcity"].ToString();
            content["operator"] = row["operator"].ToString();
            content["pickup"] = pickup;
            content["pickup_time"] = Convert.ToDateTime(row["pickup_time"]).ToString(@"hh:mmtt dd/MM/yyyy", CultureInfo.InvariantCulture);
            string url = System.Configuration.ConfigurationSettings.AppSettings["DETAILS_URL"];
            url = url.Replace("#PNR#",pnr);
            url = url.Replace("#TNO#", tno);
            content["url"] = create_tinyURL(url);
            sms_status = send_sms(booking_id, pnr, mobile, content, type);
            total_sms_sent += sms_status;
            return sms_status;
        }

        private static int process_email(DataRow row)
        {
            int email_status = 0;
            int booking_id = Convert.ToInt32(row["booking_id"].ToString());
            string to_email_id = row["customer_email"].ToString();
            #if DEBUG
                to_email_id = "amritesh.anand@travelyaari.com"; // Amritesh Anand Mobile Number
            #endif
            string cc_email_id = "";
            string bcc_email_id = "";
            string subject = "";
            string pnr = row["pnr"].ToString();
            string tno = row["ticket_no"].ToString();
            clsDB db = new clsDB();
            db.AddParameter("provider_pnr_no", pnr, 50);
            db.AddParameter("sub_agent_ticket_no", tno, 20);
            DataSet ds = db.ExecuteSelect("WS_GET_TICKET_INFO_TY", CommandType.StoredProcedure, 160);
            Dictionary<string, object> contents = new Dictionary<string, object>();
            Dictionary<string, object> attachments = new Dictionary<string, object>();
            if (ds != null && ds.Tables.Count > 2 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow details in ds.Tables[0].Rows)
                {
                    contents = details.Table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => details.Field<object>(col.ColumnName));
                    attachments = details.Table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => details.Field<object>(col.ColumnName));
                }
                string pickup_place_style = @"class=""points-right"" style=""padding: 8px 0;vertical-align: middle;border-top: 1px dashed #d3bcb9;""";
                string pickup_time_style = @"class=""points-left"" style=""font-weight: bold;vertical-align: middle;width: 80px;padding: 8px 0;border-top: 1px dashed #d3bcb9;""";
                string passenger_style = @"class=""passenger-body"" style=""font-size: 13px;padding: 5px 0;border-bottom: 1px solid #E0DACF;""";
                contents["PASSENGERS"] = convert_to_html2(ds.Tables[1], passenger_style, passenger_style);
                contents["PICKUPS"] = convert_to_html2(ds.Tables[2], pickup_time_style, pickup_place_style);
                attachments["PASSENGERS"] = convert_to_html2(ds.Tables[1], passenger_style, passenger_style);
                attachments["PICKUPS"] = convert_to_html2(ds.Tables[2], pickup_time_style, pickup_place_style);
            }
            string etype = System.Configuration.ConfigurationSettings.AppSettings["EMAIL_PREMINDER_TYPE"];
            string key = System.Configuration.ConfigurationSettings.AppSettings["EMAIL_PREMINDER_KEY"];
            email_status = send_email(booking_id, to_email_id, cc_email_id, bcc_email_id, subject, contents, attachments, etype);
            return email_status;
        }

        private static void update_informed_status(int booking_id, int sms_status, int email_status)
        {
            clsDB db = new clsDB();
            db.AddParameter("booking_id", booking_id);
            db.AddParameter("sms_sent", sms_status);
            db.AddParameter("email_sent", email_status);
            db.ExecuteDML("RMS_UPDATE_PICKUP_REMINDER_STATUS", CommandType.StoredProcedure, 160);
        }

        public static string convert_to_html2(DataTable dt, string style1, string style2)
        {
            string html = "";
            string style = "";
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                style = style1;
                html += "<tr>";
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    if (j == 1)
                        style = style2;
                    html += @"<td " + style + ">" + dt.Rows[i][j].ToString() + "</td>";
                }
                html += "</tr>";
            }
            return html;
        }

        private static string create_tinyURL(string url)
        {
            System.Uri address = new System.Uri("http://tinyurl.com/api-create.php?url=" + url);
            System.Net.WebClient client = new System.Net.WebClient();
            string tinyUrl = client.DownloadString(address);
            return tinyUrl;
        }

        public static int send_email(int booking_id, string email_ids, string cc_email_ids, string bcc_email_ids, string subject, Dictionary<string, object> contents, Dictionary<string, object> attachments, string type)
        {
            bool blnIsEmailGone = false;
            try
            {
                string strURL = System.Configuration.ConfigurationSettings.AppSettings["EMAIL_URL"];
                try
                {
                    HttpWebRequest httpreq = (HttpWebRequest)WebRequest.Create(strURL);
                    httpreq.Method = "POST";
                    httpreq.ContentType = "application/json";
                    Dictionary<string, object> post_dict = new Dictionary<string, object>();
                    post_dict["type"] = type;
                    post_dict["booking_id"] = booking_id;
                    post_dict["email_ids"] = email_ids;
                    post_dict["cc_email_ids"] = cc_email_ids;
                    post_dict["bcc_email_ids"] = bcc_email_ids;
                    post_dict["subject"] = subject;
                    post_dict["content_dict"] = (new JavaScriptSerializer()).Serialize(contents);
                    post_dict["attachments_dict"] = (new JavaScriptSerializer()).Serialize(attachments);
                    string post_json = (new JavaScriptSerializer()).Serialize(post_dict);
                    //Console.WriteLine(post_json);
                    Stream dataStream = httpreq.GetRequestStream();
                    byte[] post_data = Encoding.UTF8.GetBytes(post_json);
                    dataStream.Write(post_data, 0, post_data.Length);
                    dataStream.Close();
                    HttpWebResponse httpresp = (HttpWebResponse)httpreq.GetResponse();
                    byte[] buffer = new byte[httpresp.ContentLength + 1];
                    httpresp.GetResponseStream().Read(buffer, 0, buffer.Length);
                    string response = System.Text.Encoding.Default.GetString(buffer).Trim();
                    httpresp.Close();
                    string sPattern = "\"error\":\"\",\"status\":true";
                    blnIsEmailGone = System.Text.RegularExpressions.Regex.IsMatch(response, sPattern);
                }
                catch (System.Exception ex)
                {
                    blnIsEmailGone = false;
                    string x = ex.Message;
                }
            }
            catch (Exception ex)
            {
            }
            return Convert.ToInt32(blnIsEmailGone);
        }

        public static int send_sms(int booking_id, string strPNR, string strMobile, Dictionary<string, string> content, string type)
        {
            bool blnIsCSMSGone = false;
            try
            {
                string strURL = System.Configuration.ConfigurationSettings.AppSettings["SMS_URL"];
                string stype = System.Configuration.ConfigurationSettings.AppSettings["SMS_" + type];
                string key = System.Configuration.ConfigurationSettings.AppSettings["KEY_SMS_" + type];
                try
                {
                    HttpWebRequest httpreq = (HttpWebRequest)WebRequest.Create(strURL);
                    httpreq.Method = "POST";
                    httpreq.ContentType = "application/json";
                    Dictionary<string, object> post_dict = new Dictionary<string, object>();
                    post_dict["type"] = stype;
                    post_dict["booking_id"] = booking_id;
                    post_dict["mobile_no"] = strMobile;
                    post_dict["content_dict"] = (new JavaScriptSerializer()).Serialize(content);
                    post_dict["key"] = key;
                    string post_json = (new JavaScriptSerializer()).Serialize(post_dict);
                    //Console.WriteLine(post_json);
                    Stream dataStream = httpreq.GetRequestStream();
                    byte[] post_data = Encoding.UTF8.GetBytes(post_json);
                    dataStream.Write(post_data, 0, post_data.Length);
                    dataStream.Close();
                    HttpWebResponse httpresp = (HttpWebResponse)httpreq.GetResponse();
                    byte[] buffer = new byte[httpresp.ContentLength + 1];
                    httpresp.GetResponseStream().Read(buffer, 0, buffer.Length);
                    string response = System.Text.Encoding.Default.GetString(buffer).Trim();
                    httpresp.Close();
                    string sPattern = "\"error\":\"\",\"status\":true";
                    blnIsCSMSGone = System.Text.RegularExpressions.Regex.IsMatch(response, sPattern);
                }
                catch (System.Exception ex)
                {
                    blnIsCSMSGone = false;
                    string x = ex.Message;
                }
            }
            catch (Exception ex)
            {
            }
            return Convert.ToInt32(blnIsCSMSGone);
        }

    }
}
