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
namespace PickupsVerifier
{
    class Program
    {
        static void Main(string[] args)
        {

            fetch_rt_pickups_for_bookings();
            clsDB db=null;
            DataSet ds=null;

            /*Get mismatched pickups from gds*/
            DataTable dt_result = null;            
            db = new clsDB();
            ds = db.ExecuteSelect("RMS_GET_MISMATCHED_PICKUP_BOOKINGS",CommandType.StoredProcedure,160);
            Dictionary<int,bool> sms_sent_flags=new Dictionary<int,bool>();
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                int i = 0;
                foreach (DataRow row in ds.Tables[0].Rows)
                {                     
                    int booking_id = Convert.ToInt32(row["booking_id"]);                                        
                    string pnr=Convert.ToString(row["pnr"]);                                                            
                    string mobile = Convert.ToString(row["mobile"]);
                    mobile = "7204608909";
                    try
                    {
                        if (Convert.ToInt32(row["pickup_removed"]) == 0)
                        {
                            DateTime dt_jd = Convert.ToDateTime(row["journey_date"]);

                            // Check for mid night pickup changes
                            DateTime new_pickup_time = Convert.ToDateTime(row["new_time"]);                                                        
                            if ((dt_jd.TimeOfDay.Hours - new_pickup_time.Hour) > 18 || (new_pickup_time.Hour - dt_jd.TimeOfDay.Hours) > 18)
                            {
                                new_pickup_time = Convert.ToDateTime(row["journey_date"]).Date.AddDays(1).Add(new_pickup_time.TimeOfDay);
                            }
                            else {
                                new_pickup_time = Convert.ToDateTime(row["journey_date"]).Date.Add(new_pickup_time.TimeOfDay);
                            }

                            // Check for mid night pickup changes
                            DateTime old_pickup_time = Convert.ToDateTime(row["old_time"]);
                            if ((dt_jd.TimeOfDay.Hours - old_pickup_time.Hour) > 18 || (old_pickup_time.Hour - dt_jd.TimeOfDay.Hours) > 18)
                            {
                                old_pickup_time = Convert.ToDateTime(row["journey_date"]).Date.AddDays(1).Add(old_pickup_time.TimeOfDay);
                            }
                            else {
                                old_pickup_time = Convert.ToDateTime(row["journey_date"]).Date.Add(old_pickup_time.TimeOfDay);
                            }                                                                                    
                            
                            row["new_time"] = new_pickup_time;
                            row["old_time"] = old_pickup_time;
                            int sms_status = 0;
                            if (Convert.ToInt32(row["sms_sent"]) == 0)
                            {
                                //TODO:   
                                db = new clsDB();
                                db.AddParameter("BOOKING_ID", booking_id);
                                db.AddParameter("PROVIDER_PNR_NO", pnr,200);

                                DataSet ds_booking = db.ExecuteSelect("WS_GET_BOOKING_INFO_3", CommandType.StoredProcedure, 160);
                                //sms_status = send_sms(booking_id, pnr, mobile, ds_booking);
                            }
                            else
                            {
                                sms_status = 1;
                            }
                            row["sms_sent"]=sms_status;                           

                            update_booking(booking_id, new_pickup_time, sms_status); //Update pickup time in gds and sms_sent in booked_pickups                            
                        }                        
                    }
                    catch (System.Exception ex) { 
                    }
                    i++;
                }
                if (ds.Tables[0].Rows.Count > 0) {
                    string email_content = convert_to_html(ds.Tables[0]);
                    /*Create mismatch pickups email content based on bookings and send to team*/            
                    email_content = "<h1>Mismatching Pickup Bookings</h1></br>"+"Total="+ds.Tables[0].Rows.Count.ToString()+"<br/>" + email_content;
                    string str_email_to = System.Configuration.ConfigurationSettings.AppSettings["TO_EMAILS"].ToString();
                    send_email(str_email_to,"Pickup Mismatch Bookings",email_content);
                }
            }
            
        }
        
        /// <summary>
        /// Fetch real time pickups for each booking having journey in next few hours
        /// </summary>
        private static void fetch_rt_pickups_for_bookings() {
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
                        //TODO:replace with real time pickup api
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
        
        /// <summary>
        /// Update pickup time in gds bookings
        /// </summary>
        /// <param name="booking_id"></param>
        /// <param name="pickup_time"></param>
        /// <param name="sms_sent"></param>
        public static void update_booking(int booking_id, DateTime pickup_time,int sms_sent)
        {
            clsDB db = new clsDB();
            db.AddParameter("booking_id", booking_id);
            db.AddParameter("pickup_time", pickup_time);
            db.AddParameter("sms_sent", sms_sent);
            db.ExecuteSelect("RMS_UPDATE_BOOKING_PICKUP", CommandType.StoredProcedure, 160);

        }
        
        /// <summary>
        /// Create html from data table
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string convert_to_html(DataTable dt) {
            string html = @"<table style=""border:1px solid black"">";
            //add header row
            html += "<tr>";
            for (int i = 0; i < dt.Columns.Count; i++)
                html += @"<th style=""border:1px solid black"">" + dt.Columns[i].ColumnName.ToUpper() + "</th>";
            html += "</tr>";
            //add rows
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                html += "<tr>";
                for (int j = 0; j < dt.Columns.Count; j++)
                    html += @"<td style=""border:1px solid black"">" + dt.Rows[i][j].ToString() + "</td>";
                html += "</tr>";
            }
            html += "</table>";
            return html;
        }
        

        /// <summary>
        /// send email api
        /// </summary>
        /// <param name="str_To"></param>
        /// <param name="strSubject"></param>
        /// <param name="strBody"></param>
        public static void send_email(string str_To, string strSubject, string strBody)
        {
            MailMessage message = null;
            try
            {
                string tyEmailId = System.Configuration.ConfigurationSettings.AppSettings["TYEmailId"];
                string tyEmailPassword = System.Configuration.ConfigurationSettings.AppSettings["TYEmailPassword"];                
                string smtpServer = System.Configuration.ConfigurationSettings.AppSettings["SMTPServer"];
                string smtpPort = System.Configuration.ConfigurationSettings.AppSettings["SMTPPort"];
                string cc_emails = System.Configuration.ConfigurationSettings.AppSettings["CC_EMAILS"];

                SmtpClient smtp = new SmtpClient(smtpServer, Convert.ToInt32(smtpPort));
                message = new MailMessage();

                smtp.UseDefaultCredentials = false;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Timeout = 60000;
                smtp.EnableSsl = true;
                
                smtp.Credentials = new System.Net.NetworkCredential(tyEmailId, tyEmailPassword);
                message.From = new MailAddress(tyEmailId);

                string[] strArrEmails = str_To.Split(',');
                foreach (string strToEmail in strArrEmails)
                    message.To.Add(new MailAddress(strToEmail));

                strArrEmails = cc_emails.Split(',');
                foreach (string strCCEmail in strArrEmails)
                    message.CC.Add(new MailAddress(strCCEmail));
                message.BodyEncoding = System.Text.Encoding.UTF8;
                message.Body = strBody;
                message.IsBodyHtml = true;
                message.Subject = strSubject;
                
                //if (strAttachment != "")
                //    message.Attachments.Add(new Attachment(strAttachment));
                smtp.Send(message);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (message != null)
                    message.Dispose();
            }
        }
        
        /// <summary>
        /// Send sms : Deprecated
        /// </summary>
        /// <param name="bkgID"></param>
        /// <param name="strPNR"></param>
        /// <param name="strMobile"></param>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static int send_sms(int bkgID, string strPNR, string strMobile, DataSet ds)
        {
            bool blnIsCSMSGone = false;
            try
            {

                string strMsg1 = "", strMsg2 = "";
                //new BusinessLayer().LoadSMSTemplate(ref strMsg1, ref strMsg2);
                clsDB db = new clsDB();
                db.AddParameter("TEMPLATE_ID", 1);
                DataSet ds_result=db.ExecuteSelect("GDS_GET_SMSTemplate", CommandType.StoredProcedure, 160);
                if (ds_result != null && ds_result.Tables.Count > 0 && ds_result.Tables[0].Rows.Count > 0)
                {
                    strMsg1 = ds_result.Tables[0].Rows[0]["CONFIRM_SMS"].ToString();
                    strMsg2 = ds_result.Tables[0].Rows[0]["DEPART_SMS"].ToString();
                    StringBuilder strCSMS = new StringBuilder(strMsg1.ToUpper());
                    StringBuilder strDSMS = new StringBuilder(strMsg2.ToUpper());

                    if (strMsg1 != "" && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        strCSMS.Replace("TEMPID", "tempid");
                        strDSMS.Replace("TEMPID", "tempid");

                        strCSMS.Replace("[PNR]", Convert.ToString(ds.Tables[0].Rows[0]["provider_pnr_no"]).ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[PNR]", Convert.ToString(ds.Tables[0].Rows[0]["provider_pnr_no"]).ToUpper().Replace("&", "%26"));

                        strCSMS.Replace("[OP]", Convert.ToString(ds.Tables[0].Rows[0]["company_name"]).ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[OP]", Convert.ToString(ds.Tables[0].Rows[0]["company_name"]).ToUpper().Replace("&", "%26"));

                        strCSMS.Replace("[RT]", Convert.ToString(ds.Tables[0].Rows[0]["fromcity"]).ToUpper().Replace("&", "%26") + " - " + Convert.ToString(ds.Tables[0].Rows[0]["tocity"]).ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[RT]", Convert.ToString(ds.Tables[0].Rows[0]["fromcity"]).ToUpper().Replace("&", "%26") + " - " + Convert.ToString(ds.Tables[0].Rows[0]["tocity"]).ToUpper().Replace("&", "%26"));

                        strCSMS.Replace("[JD]", Convert.ToDateTime(ds.Tables[0].Rows[0]["journey_date"]).ToString("dd-MMM-yyyy").ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[JD]", Convert.ToDateTime(ds.Tables[0].Rows[0]["journey_date"]).ToString("dd-MMM-yyyy").ToUpper().Replace("&", "%26"));

                        strCSMS.Replace("[JT]", Convert.ToDateTime(ds.Tables[0].Rows[0]["pickup_time"]).ToString("hh:mm tt").ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[JT]", Convert.ToDateTime(ds.Tables[0].Rows[0]["pickup_time"]).ToString("hh:mm tt").ToUpper().Replace("&", "%26"));

                        strCSMS.Replace("[SB]", Convert.ToString(ds.Tables[0].Rows[0]["all_seats"]).ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[SB]", Convert.ToString(ds.Tables[0].Rows[0]["all_seats"]).ToUpper().Replace("&", "%26"));

                        strCSMS.Replace("[TS]", Convert.ToString(ds.Tables[0].Rows[0]["total_seats"]).ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[TS]", Convert.ToString(ds.Tables[0].Rows[0]["total_seats"]).ToUpper().Replace("&", "%26"));

                        strCSMS.Replace("[PN]", Convert.ToString(ds.Tables[0].Rows[0]["customer_name"]).ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[PN]", Convert.ToString(ds.Tables[0].Rows[0]["customer_name"]).ToUpper().Replace("&", "%26"));

                        strCSMS.Replace("[UB]", Convert.ToString(ds.Tables[0].Rows[0]["booked_by_agent"]).ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[UB]", Convert.ToString(ds.Tables[0].Rows[0]["booked_by_agent"]).ToUpper().Replace("&", "%26"));

                        strCSMS.Replace("[PL]", Convert.ToString(ds.Tables[0].Rows[0]["pickup_name"]).ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[PL]", Convert.ToString(ds.Tables[0].Rows[0]["pickup_name"]).ToUpper().Replace("&", "%26"));

                        string strPickup = Convert.ToString(ds.Tables[0].Rows[0]["pickup_name"]) + "," + Convert.ToString(ds.Tables[0].Rows[0]["pickup_landmark"]) + "," + Convert.ToString(ds.Tables[0].Rows[0]["pickup_address"]);
                        strCSMS.Replace("[PL1]", strPickup.ToUpper().Replace("&", "%26"));
                        strDSMS.Replace("[PL1]", strPickup.ToUpper().Replace("&", "%26"));
                        strCSMS.Replace("[TF]", Convert.ToString(Convert.ToDecimal(ds.Tables[0].Rows[0]["booking_total_fare"]) + Convert.ToDecimal(ds.Tables[0].Rows[0]["sub_agent_service_charge"]) + Convert.ToDecimal(ds.Tables[0].Rows[0]["DELIVERY_CHARGE"]) - Convert.ToDecimal(ds.Tables[0].Rows[0]["discount"])));
                        strDSMS.Replace("[TF]", Convert.ToString(Convert.ToDecimal(ds.Tables[0].Rows[0]["booking_total_fare"]) + Convert.ToDecimal(ds.Tables[0].Rows[0]["sub_agent_service_charge"]) + Convert.ToDecimal(ds.Tables[0].Rows[0]["DELIVERY_CHARGE"]) - Convert.ToDecimal(ds.Tables[0].Rows[0]["discount"])));


                        try
                        {
                            if (ds.Tables[0].Rows[0]["company_id"].ToString() == "6933") //KSRTC
                            {
                                string strBusTypeName = Convert.ToString(ds.Tables[0].Rows[0]["bus_type_name"]);
                                string[] strTripTypeName = strBusTypeName.Split('-');
                                string strPlatform = Convert.ToString(ds.Tables[0].Rows[0]["pickup_address"]).Replace("Platform No:", "");

                                string strSMS = "TripCode: " + Convert.ToString(strTripTypeName[0]).ToUpper() + ", Class: " + Convert.ToString(strTripTypeName[1]).ToUpper() + ", Pltfrm: " + (strPlatform == "" ? "--" : strPlatform) + " ";

                                strCSMS.Insert(0, strSMS.ToUpper().Replace("&", "%26"));
                            }
                        }
                        catch
                        { }

                        //strCSMS.Replace("&", "%26");
                        //strDSMS.Replace("&", "%26");
                        string strURL;//= @"http://www.ezeesms.com/sms/user/urlsms.php?username=travelyaari&amp;pass=travel&amp;dest_mobileno=" + strMobile + "&amp;senderid=TvlYaari&amp;message=" + strCSMS;
                        string strURL_DEPART;
                        strURL = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["TYSMSURL"]);
                        strURL_DEPART = strURL;
                        strURL = strURL.Replace("#MOBILENO#", strMobile);
                        strURL_DEPART = strURL_DEPART.Replace("#MOBILENO#", strMobile);
                        strURL = strURL.Replace("#MESSAGE#", strCSMS.ToString());
                        strURL_DEPART = strURL_DEPART.Replace("#MESSAGE#", strDSMS.ToString());
                        string response="";
                        try
                        {
                            HttpWebRequest httpreq = (HttpWebRequest)WebRequest.Create(strURL);
                            HttpWebResponse httpresp = (HttpWebResponse)httpreq.GetResponse();
                            byte[] buffer=new byte[httpresp.ContentLength+1];
                            httpresp.GetResponseStream().Read(buffer, 0, buffer.Length);
                            response = System.Text.Encoding.Default.GetString(buffer).Trim();
                            httpresp.Close();
                            string sPattern = "\\d+-\\d+_\\d+_\\d+";
                            blnIsCSMSGone = System.Text.RegularExpressions.Regex.IsMatch(response, sPattern);                                
                        }
                        catch (System.Exception ex)
                        {
                            blnIsCSMSGone = false;
                        }

                        int smsid = 0;
                        string strErrMsg = "";
                        db = new clsDB();
                        db.AddParameter("SMS_ID", smsid);
                        db.AddParameter("BOOKING_ID", bkgID);
                        db.AddParameter("URL", strURL,1000);
                        db.AddParameter("URL_DEPART", strURL_DEPART,1000);
                        db.AddParameter("SEND_TIME", Convert.ToDateTime(ds.Tables[0].Rows[0]["journey_date"]));
                        db.AddParameter("ERR_MSG", strErrMsg, 200);
                        db.AddParameter("IS_SENT", blnIsCSMSGone);
                        db.ExecuteDML("GDS_SMSLog_Insert", CommandType.StoredProcedure, 160);
                        //new SMSLogBO().SMSLog_Insert(ref smsid, bkgID, strMobile, strURL, strURL_DEPART, Convert.ToDateTime(ds.Tables[0].Rows[0]["journey_date"]), ref strErrMsg, blnIsCSMSGone);


                        //HttpWebRequest httpreq = (HttpWebRequest)WebRequest.Create(strURL);
                        //HttpWebResponse httpresp = (HttpWebResponse)httpreq.GetResponse();
                    }
                }
                
            }
            catch (Exception ex)
            {

            }
            return Convert.ToInt32(blnIsCSMSGone);
        }
        
        public static void test()
        {
            clsDB db = new clsDB();
            string CONFIRM_SMS = "";
            string DEPART_SMS = "";
            db.AddParameter("TEMPLATE_ID", 1);
            db.AddOutParameter("CONFIRM_SMS", CONFIRM_SMS,500);
            db.AddOutParameter("DEPART_SMS", DEPART_SMS,500);
            db.ExecuteSelect("spSMSTemplate_Get", CommandType.StoredProcedure, 160);
            CONFIRM_SMS = db.outParams["CONFIRM_SMS"].ToString();
            DEPART_SMS = db.outParams["DEPART_SMS"].ToString();
        }

        /// <summary>
        /// Send sms : New 
        /// </summary>
        /// <param name="mobile_no"></param>
        /// <param name="ticket_sms"></param>
        /// <returns></returns>
        public static bool send_sms_eazy(string mobile_no,Ticket_SMS ticket_sms)
        { 
            bool sms_success=false;
            string sms_view = "Dear Customer,you ticket $pnrNo is booked on $operatorName from $fromCity to $toCity on $pickup['date']. Boarding is from :$Boarding at $pickup['time']";
            sms_view=sms_view.Replace("$ticketNo", ticket_sms.ticket_no);
            sms_view = sms_view.Replace("$pnrNo", ticket_sms.pnr_no.ToUpper());
            sms_view = sms_view.Replace("$fromCity", ticket_sms.from_city.ToUpper());
            sms_view = sms_view.Replace("$toCity", ticket_sms.to_city);
            sms_view = sms_view.Replace("$Boarding", ticket_sms.pickup);
            sms_view = sms_view.Replace("$pickup['date']", ticket_sms.pickup_time.Date.ToShortDateString());
            sms_view = sms_view.Replace("$pickup['time']", ticket_sms.pickup_time.TimeOfDay.ToString());
            sms_view = sms_view.Replace("$bookedSeats", ticket_sms.seats);
            sms_view = sms_view.Replace("$operatorName", ticket_sms.operator_name);
            sms_view = sms_view.Replace("$operatorPhone", ticket_sms.operator_phone);
            sms_view = sms_view.Replace("$customerName", ticket_sms.name);
            string sms_url = ConfigurationSettings.AppSettings["EAZY_SMS_URL"].ToString();

            sms_url=sms_url.Replace("@@customerMobile", mobile_no);
            sms_url=sms_url.Replace("@@customerMessage",sms_view);
            try
            {
                HttpWebRequest httpreq = (HttpWebRequest)WebRequest.Create(sms_url);
                HttpWebResponse httpresp = (HttpWebResponse)httpreq.GetResponse();
                byte[] buffer = new byte[httpresp.ContentLength + 1];
                httpresp.GetResponseStream().Read(buffer, 0, buffer.Length);
                string response = System.Text.Encoding.Default.GetString(buffer).Trim();
                httpresp.Close();
                if (response.Trim().ToUpper().Contains("SENT"))
                {
                    sms_success = true;
                }
                else {
                    sms_success = false;
                }
                //string pattern = "\\d+-\\d+_\\d+_\\d+";
                //sms_success= System.Text.RegularExpressions.Regex.IsMatch(response, pattern);
            }
            catch (System.Exception ex)
            { 

            }
            return sms_success;
        }
        
    }
    /// <summary>
    /// SMS Content object
    /// </summary>
    public class Ticket_SMS
    {
        public string ticket_no = "";
        public string pnr_no = "";
        public string from_city = "";
        public string to_city = "";
        public DateTime pickup_time;
        public string seats;
        public string operator_name = "";
        public string operator_phone = "";
        public string name = "";
        public string pickup = "";
        public Ticket_SMS(DataSet dsBooking)
        {
            this.pnr_no = dsBooking.Tables[0].Rows[0]["provider_pnr_no"].ToString();
            this.ticket_no = dsBooking.Tables[0].Rows[0]["sub_agent_ticket_no"].ToString();
            this.from_city = dsBooking.Tables[0].Rows[0]["from_city_name_temp"].ToString();
            this.to_city = dsBooking.Tables[0].Rows[0]["to_city_name_temp"].ToString();
            this.pickup_time = Convert.ToDateTime(dsBooking.Tables[0].Rows[0]["pickup_time"]);
            this.pickup = dsBooking.Tables[0].Rows[0]["pickup_name"].ToString();
            this.seats = dsBooking.Tables[0].Rows[0]["all_seats"].ToString();
            this.operator_name =dsBooking.Tables[0].Rows[0]["company_name_temp"].ToString();
            this.operator_phone = dsBooking.Tables[0].Rows[0]["pickup_phone"].ToString();            
            this.name = dsBooking.Tables[1].Rows[0]["name"].ToString();
        }
        public static Ticket_SMS get_test_ticket()
        {
            clsDB db = new clsDB();            
            db.AddParameter("PROVIDER_PNR_NO", "32317034-651931",200);
            db.AddParameter("SUB_AGENT_TICKET_NO","3331415333557",200);
            DataSet ds_booking = db.ExecuteSelect("WS_GET_BOOKING_INFO_3", CommandType.StoredProcedure, 160);
            return new Ticket_SMS(ds_booking);
        }
    }
}
