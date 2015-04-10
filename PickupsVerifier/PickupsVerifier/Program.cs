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

namespace PickupsVerifier
{
    class Program
    {
        
        static void Main(string[] args)
        {
            /* Fetch pickups for today's bookings.*/
            #if !DEBUG
            fetch_rt_pickups_for_bookings();            
            #endif

            /*Get mismatched pickups from gds*/
            clsDB db = new clsDB();
            
            DataSet ds = db.ExecuteSelect("RMS_GET_MISMATCHED_PICKUP_BOOKINGS", CommandType.StoredProcedure, 160);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    process(row);
                }
                if (ds.Tables[0].Rows.Count > 0)
                {
                    process_aggregation(ds.Tables[0]);
                }
            }
        }

        private static void fetch_rt_pickups_for_bookings()
        {
            string auth_hash = ConfigurationSettings.AppSettings["AUTH_HASH"].ToString();
            string security = ConfigurationSettings.AppSettings["SECURITY"].ToString();
            int user_id = Convert.ToInt32(ConfigurationSettings.AppSettings["USER_ID"]);
            clsDB db = new clsDB();
            DataSet ds = db.ExecuteSelect("RMS_GET_COMING_JD_BOOKINGS", CommandType.StoredProcedure, 160);
            gds_api.Service service = new PickupsVerifier.gds_api.Service();
            gds_api.clsAuthenticateRequest auth = new PickupsVerifier.gds_api.clsAuthenticateRequest();
            auth.UserID = user_id;
            auth.UserType = "S";
            auth.Key = auth_hash;
            Dictionary<int, bool> bookings_pickups = new Dictionary<int, bool>();
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                Console.WriteLine("Pickups to refresh=" + ds.Tables[0].Rows.Count.ToString());
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    int route_schedule_id = Convert.ToInt32(dr["route_schedule_id"]);
                    DateTime journey_date = Convert.ToDateTime(dr["journey_date"]);
                    int booked_pickup_id = Convert.ToInt32(dr["pickup_id"]);
                    int booking_id = Convert.ToInt32(dr["booking_id"]);
                    try
                    {
                        gds_api.clsPickups pickup_respnse = service.GetPickupsForJourneyDateRT(auth, route_schedule_id, journey_date, security);
                        gds_api.clsPickup[] pickup_list = pickup_respnse.Pickup;
                        bool found = false;
                        foreach (gds_api.clsPickup pickup_data in pickup_list)
                        {
                            if (pickup_data.PickupId == booked_pickup_id)
                            {
                                found = true;
                                break;
                            }
                        }
                        bookings_pickups[booking_id] = found;
                        Console.WriteLine(booking_id.ToString() + ":" + found.ToString());
                        if (!found)
                        {
                            db = new clsDB();
                            db.AddParameter("booking_id", booking_id);
                            db.ExecuteDML("RMS_BOOKING_PICKUP_DEACTIVATED", CommandType.StoredProcedure, 160);
                        }
                    }
                    catch (System.Exception ex)
                    {
                    }
                }
            }
        }

        private static void process(DataRow row)
        {
            int user_id = Convert.ToInt32(row["user_id"].ToString());
            string type = "";
            int[] api_partners = { 3470,//Abhibus
                                    456,//Goibibo
                                    465,//MMT
                                    630,//VIA
                                    2489,//TicketGoose
                                    463,//Hermes
                                    4139,//TicketBlu
                                    3641//TripGoTrip
                                 };
            foreach(int api_partner in api_partners)
            {
                if(user_id==api_partner)
                    return;
            }

            if(user_id==333 || user_id == 4642)
                type = "SMS_TY";
            else
                type = "SMS_GDS";

            try
            {
                if (Convert.ToInt32(row["pickup_removed"]) == 0)
                {
                    // Check for mid night pickup changes
                    DateTime dt_jd = Convert.ToDateTime(row["journey_date"]);
                    row["new_time"] = midnight_timecheck(Convert.ToDateTime(row["new_time"]), dt_jd);
                    row["old_time"] = midnight_timecheck(Convert.ToDateTime(row["old_time"]), dt_jd);

                    //Update pickup time in gds and sms_sent in booked_pickups
                    #if !DEBUG
                    update_booking(Convert.ToInt32(row["booking_id"].ToString()), Convert.ToDateTime(row["new_time"].ToString())); 
                    #endif

                    int sms_status = 0;
                    int email_status = 0;


                    if (Convert.ToInt32(row["sms_sent"]) == 0)
                    {
                        sms_status = process_sms(row, type);
                    }
                    else
                    {
                        sms_status = 1;
                    }

                    if (Convert.ToInt32(row["email_sent"]) == 0)
                    {
                        email_status = process_email(row);
                    }
                    else
                    {
                        email_status = 1;
                    }

                    // Update sms and email status in BOOKED_PICKUP_UPDATES
                    #if !DEBUG
                        update_informed_status(Convert.ToInt32(row["booking_id"].ToString()), sms_status, email_status);
                    #endif
                }
            }
            catch (System.Exception ex)
            {
            }
        }

        private static DateTime midnight_timecheck(DateTime time, DateTime dt_jd)
        {
            if ((dt_jd.TimeOfDay.Hours - time.Hour) > 18 || (time.Hour - dt_jd.TimeOfDay.Hours) > 18)
            {
                time = dt_jd.Date.AddDays(1).Add(time.TimeOfDay);
            }
            else
            {
                time = dt_jd.Date.Add(time.TimeOfDay);
            }
            return time;
        }

        public static void update_booking(int booking_id, DateTime pickup_time)
        {
            clsDB db = new clsDB();
            db.AddParameter("booking_id", booking_id);
            db.AddParameter("pickup_time", pickup_time);
            db.ExecuteDML("RMS_UPDATE_BOOKING_PICKUP_Amritesh", CommandType.StoredProcedure, 160);
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
            content["pnr"] = pnr;
            string[] pickup_array = pickup.Split(new string[] { "</br>" }, StringSplitOptions.None);
            content["pickup"] = pickup_array[0];
            content["new_time"] = Convert.ToDateTime(row["new_time"]).ToString(@"hh:mmtt dd/MM/yyyy", CultureInfo.InvariantCulture);
            string url = System.Configuration.ConfigurationSettings.AppSettings["DETAILS_URL"];
            url = url.Replace("#PNR#",pnr);
            url = url.Replace("#TNO#", tno);
            content["url"] = create_tinyURL(url);
            sms_status = send_sms(booking_id, pnr, mobile, content, type);
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
            string cc_email_id = System.Configuration.ConfigurationSettings.AppSettings["TO_EMAILS_OMS"];
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
                contents["PICKUPS"] = convert_to_html2(ds.Tables[2], pickup_time_style,pickup_place_style);
                attachments["PASSENGERS"] = convert_to_html2(ds.Tables[1], passenger_style, passenger_style);
                attachments["PICKUPS"] = convert_to_html2(ds.Tables[2], pickup_time_style, pickup_place_style);
            }
            string etype = System.Configuration.ConfigurationSettings.AppSettings["EMAIL_PMISMATCH_TYPE"];
            string key = System.Configuration.ConfigurationSettings.AppSettings["EMAIL_PMISMATCH_KEY"];
            email_status = send_email(booking_id, to_email_id, cc_email_id, subject, contents, attachments, etype);
            return email_status;
        }

        private static void process_aggregation(DataTable mismatch_table)
        {
            /*Create mismatch pickups email content based on bookings and send to team*/
            Dictionary<int,string> email_list = new Dictionary<int,string>();
            clsDB db = new clsDB();
            DataSet ds = db.ExecuteSelect("WS_GET_AGENT_INFO_DETAILS", CommandType.StoredProcedure, 160);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach(DataRow row in ds.Tables[0].Rows)
                {
                    try
                    { 
                        int user_id = Convert.ToInt32(row["sub_agent_id"].ToString());
                        email_list[user_id] = row["ops_email_id"].ToString();
                    }
                    catch{}
                }
            }

            string email_id = "";
            try
            {
                email_id = email_list[333];
                send_aggregate(mismatch_table, "OMS", 0, email_id);
            }
            catch
            {}

            List<DataTable> id_based_tables = mismatch_table.AsEnumerable()
                                        .GroupBy(row => row.Field<int>("user_id"))
                                        .Select(g => g.CopyToDataTable()).ToList();
            foreach (DataTable table in id_based_tables)
            {
                string type = "None";
                email_id = "";
                int user_id = Convert.ToInt32(table.Rows[0]["user_id"].ToString());
                try
                { email_id = email_list[user_id]; }
                catch
                { continue; }
                if (email_id == null || email_id.Length == 0)
                { continue; }
                try
                { send_aggregate(table, type, user_id, email_id); }
                catch
                { continue; }
            }
        }

        private static void send_aggregate(DataTable table, string type, int user_id, string email_ids)
        {
            int booking_id = 0;
            #if DEBUG
                email_ids = "amritesh.anand@travelyaari.com"; // Amritesh Anand email address
            #endif
            string cc_email_ids = System.Configuration.ConfigurationSettings.AppSettings["TO_EMAILS_OMS"];
            string subject = "Mismatching Pickup Bookings";
            Dictionary<string, object> contents = new Dictionary<string, object>();
            string email_content;
            if(type=="OMS")
               email_content  = convert_to_html(table);
            else
               email_content = convert_to_html_selective(table);
            email_content = "<h1>Mismatching Pickup Bookings</h1></br>" + "Total=" + table.Rows.Count.ToString() + "<br/>" + email_content;
            contents["CONTENT"] = email_content;
            Dictionary<string, object> attachments = new Dictionary<string, object>();
            string etype = System.Configuration.ConfigurationSettings.AppSettings["EMAIL_TABLE_TYPE"];
            string key = System.Configuration.ConfigurationSettings.AppSettings["EMAIL_TABLE_KEY"];
            int email_status = send_email(booking_id, email_ids, cc_email_ids, subject, contents, attachments, etype);
        }

        private static void update_informed_status(int booking_id, int sms_status, int email_status)
        {
            clsDB db = new clsDB();
            db.AddParameter("booking_id", booking_id);
            db.AddParameter("sms_sent", sms_status);
            db.AddParameter("email_sent", email_status);
            db.ExecuteDML("RMS_UPDATE_INFORMED_STATUS", CommandType.StoredProcedure, 160);
        }

        public static string convert_to_html(DataTable dt)
        {
            string html = @"<table style=""border:1px solid black"">";
            //add header row
            html += "<tr>";
            for (int i = 0; i < dt.Columns.Count-1; i++)
                html += @"<th style=""border:1px solid black"">" + dt.Columns[i].ColumnName.ToUpper() + "</th>";
            html += "</tr>";
            //add rows
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                html += "<tr>";
                for (int j = 0; j < dt.Columns.Count-1; j++)
                    html += @"<td style=""border:1px solid black"">" + dt.Rows[i][j].ToString() + "</td>";
                html += "</tr>";
            }
            html += "</table>";
            return html;
        }

        public static string convert_to_html2(DataTable dt, string style1 , string style2)
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

        public static string convert_to_html_selective(DataTable dt)
        {
            int[] valid = {0,1,2,9,15,16,17,3,4,10,11,12}; 
            string html = @"<table style=""border:1px solid black"">";
            //add header row
            html += "<tr>";
            foreach(int i in valid)
                html += @"<th style=""border:1px solid black"">" + dt.Columns[i].ColumnName.ToUpper() + "</th>";
            html += "</tr>";
            //add rows
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                html += "<tr>";
                foreach (int j in valid)
                    html += @"<td style=""border:1px solid black"">" + dt.Rows[i][j].ToString() + "</td>";
                html += "</tr>";
            }
            html += "</table>";
            return html;
        }

        public static int send_email(int booking_id, string email_ids, string cc_email_ids, string subject, Dictionary<string, object> contents, Dictionary<string, object> attachments, string type)
        {
            bool blnIsCEmailGone = false;
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
                    post_dict["subject"] = subject;
                    post_dict["content_dict"] = (new JavaScriptSerializer()).Serialize(contents);
                    post_dict["attachments_dict"] = (new JavaScriptSerializer()).Serialize(attachments);
                    string post_json = (new JavaScriptSerializer()).Serialize(post_dict);
                    Console.WriteLine(post_json);
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
                    blnIsCEmailGone = System.Text.RegularExpressions.Regex.IsMatch(response, sPattern);
                }
                catch (System.Exception ex)
                {
                    blnIsCEmailGone = false;
                    string x = ex.Message;
                }
            }
            catch (Exception ex)
            {
            }
            return Convert.ToInt32(blnIsCEmailGone);
        }

        public static int send_sms(int booking_id, string strPNR, string strMobile, Dictionary<string, string> content, string type)
        {
            bool blnIsCSMSGone = false;
            try
            {
                string strURL = System.Configuration.ConfigurationSettings.AppSettings["SMS_URL"];
                string stype = System.Configuration.ConfigurationSettings.AppSettings[type];
                string key = System.Configuration.ConfigurationSettings.AppSettings["KEY_"+type];
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
                    Console.WriteLine(post_json);
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

        private static string create_tinyURL(string url)
        {
            System.Uri address = new System.Uri("http://tinyurl.com/api-create.php?url=" + url);
            System.Net.WebClient client = new System.Net.WebClient();
            string tinyUrl = client.DownloadString(address);
            return tinyUrl;
        }
    }
}
