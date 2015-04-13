using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
namespace PickupsVerifier
{
    public class clsDB
    {
        SqlCommand cmd;
        SqlCommand cmdFilter;
        string strConnString;
        string strConnStringSlave;
        string strConnStringCRS;
        string strConnStringBitla;
        string strAbhibusConnString;
        string strKalladaConnString;
        string strNeetaConnString;
        string strConnStringNeeta = "";
        string strTicketGooseConnString;
        string strKPNConnString;
        string strHindusthanConnString;
        string strConnStringMantis2011;
        string strConnStringPaulo = "";
        string strHermesConnString = "";
        string strConnStringPurple = "";
        string strConnStringPatel = "";
        string strDurgambaConnString = "";
        string strConnStringKesineni = "";
        string strConnStringParveen = "";
        string strOgiveConnString = "";
        string strConnStringOlivea = "";
        string strVishalConnString = "";
        string strSriDurgambaConnString = "";
        string strAbhibusOperatorsConnString = "";
        string strConnStringRSRTC = "";
        string strConnStringUPSRTC = "";
        string strConnStringHRTC = "";
        string strConnStringMSRTC = "";
        public Dictionary<string, object> outParams = new Dictionary<string, object>();
        public clsDB()
        {
            strConnString = System.Configuration.ConfigurationSettings.AppSettings["GDSConnString"].ToString();            

            //this.loadConfig();            

            StartNewCommand();
        }
        private void log()
        { 
        }        

        public void StartNewCommand()
        {
            cmd = new SqlCommand();
            cmdFilter = new SqlCommand();
        }

        public void AddParameter(string strParamName, string strParamValue, int intParamSize)
        {
            cmd.Parameters.Add("@" + strParamName, SqlDbType.VarChar, intParamSize).Value = strParamValue;
        }
        public void AddParameter(string strParamName, int intParamValue)
        {
            cmd.Parameters.Add("@" + strParamName, SqlDbType.Int).Value = intParamValue;
        }
        public void AddParameter(string strParamName, bool blnParamValue)
        {
            cmd.Parameters.Add("@" + strParamName, SqlDbType.Bit).Value = blnParamValue;
        }
        public void AddParameter(string strParamName, DateTime dtParamValue)
        {
            cmd.Parameters.Add("@" + strParamName, SqlDbType.DateTime).Value = dtParamValue;
        }
        public void AddParameter(string strParamName, decimal dclParamValue)
        {
            cmd.Parameters.Add("@" + strParamName, SqlDbType.Decimal).Value = dclParamValue;
        }
        public void AddParameter(string strParamName, double dblParamValue)
        {
            cmd.Parameters.Add("@" + strParamName, SqlDbType.Decimal).Value = dblParamValue;
        }
        public void AddParameter(string strParamName, DataTable tablePara)
        {
            cmd.Parameters.Add("@" + strParamName, SqlDbType.Structured).Value = tablePara;
        }
        public void AddOutParameter(string strParamName, string strParamValue, int intParamSize)
        {
            SqlParameter pInOut=cmd.Parameters.Add("@" + strParamName, SqlDbType.VarChar, intParamSize);
            pInOut.Value= strParamValue;
            pInOut.Direction = ParameterDirection.Output;
            this.outParams[strParamName] = strParamValue;
        }
        public void AddOutParameter(string strParamName,  int intParamValue)
        {
            SqlParameter pInOut = cmd.Parameters.Add("@" + strParamName, SqlDbType.Int);
            pInOut.Value = intParamValue;
            pInOut.Direction = ParameterDirection.InputOutput;
        }
        public void AddOutParameter(string strParamName,  bool blnParamValue)
        {
            SqlParameter pInOut = cmd.Parameters.Add("@" + strParamName, SqlDbType.Bit);
            pInOut.Value = blnParamValue;
            pInOut.Direction = ParameterDirection.InputOutput;
        }
        public void AddOutParameter(string strParamName,  DateTime dtParamValue)
        {
            SqlParameter pInOut = cmd.Parameters.Add("@" + strParamName, SqlDbType.DateTime);
            pInOut.Value = dtParamValue;
            pInOut.Direction = ParameterDirection.InputOutput;
        }
        public void AddOutParameter(string strParamName,  decimal dclParamValue)
        {
            SqlParameter pInOut = cmd.Parameters.Add("@" + strParamName, SqlDbType.Decimal);
            pInOut.Value = dclParamValue;
            pInOut.Direction = ParameterDirection.InputOutput;
        }
        public void AddOutParameter(string strParamName,  double dblParamValue)
        {
            SqlParameter pInOut = cmd.Parameters.Add("@" + strParamName, SqlDbType.Decimal);
            pInOut.Value = dblParamValue;
            pInOut.Direction = ParameterDirection.InputOutput;
        }
        public void AddOutParameter(string strParamName,  DataTable tablePara)
        {
            SqlParameter pInOut = cmd.Parameters.Add("@" + strParamName, SqlDbType.Structured);
            pInOut.Value = tablePara;
            pInOut.Direction = ParameterDirection.InputOutput;
        }
        public DataTable FilterDataTable(DataTable dt, string strFilterQuery, string strSortBy)
        {
            DataTable dtTemp;
            DataRow[] dr;
            dr = dt.Select(strFilterQuery, strSortBy);
            dtTemp = dt.Clone();

            foreach (DataRow r in dr)
            {
                dtTemp.ImportRow(r);
            }
            return dtTemp;
        }

        public int ExecuteDML(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                //cmd.Parameters.Clear();
                foreach (string key in this.outParams.Keys)
                {
                    this.outParams[key] = cmd.Parameters[key];
                }
            }
            catch (System.Exception ex)
            {
                status = -1;
                string errorMessage = ex.Message;
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public DataSet ExecuteSelect(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnString);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                //cmd.Parameters.Clear();
                ArrayList keys=new ArrayList();
                foreach (string key in this.outParams.Keys) {
                    keys.Add(key);
                    
                }
                foreach (string key in keys.ToArray()) {
                    this.outParams[key]= cmd.Parameters["@"+key].Value;
                }
                
            }
            catch (System.Exception e)
            {
                ds = null;
                

                //prateek- line below should be uncommented.
                //throw e;
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public DataSet ExecuteSelectSlave(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnStringSlave);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                //cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                

                //prateek- line below should be uncommented.
                //throw e;
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public int ExecuteDMLVishal(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strVishalConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public DataSet ExecuteSelectBITLA(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnStringBitla);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                

                //prateek- line below should be uncommented.
                //throw e;
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public int ExecuteDMLBITLA(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringBitla);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLPurple(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringPurple);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLParveen(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringParveen);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
               
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public DataSet ExecuteSelect_CRS(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnStringCRS);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        // DB connector to Pull_Abhibus2 database
        public DataSet ExecuteSelectABHIBUS(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strAbhibusConnString);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                

                //prateek- line below should be uncommented.
                //throw e;
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public int ExecuteDMLABHIBUS(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strAbhibusConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLKesineni(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringKesineni);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }
        // DB connector to Pull_Kallada database
        public DataSet ExecuteSelectKallada(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strKalladaConnString);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public int ExecuteDMLKALLADA(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strKalladaConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLTICKETGOOSE(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strTicketGooseConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
               
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLKPN(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strKPNConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                //logger.log("fatal", "Message = " + ex.Message + "; Stack trace = " + ex.StackTrace.Replace("\n", "\t"));
               
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLHindusthan(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strHindusthanConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                //logger.log("fatal", "Message = " + ex.Message + "; Stack trace = " + ex.StackTrace.Replace("\n", "\t"));
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLOgive(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strOgiveConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
               
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLOlivea(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringOlivea);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                //logger.log("error", "\nStack trace : " + ex.StackTrace + "\nMessage : " + ex.Message);
               
            }

            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLDurgamba(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strDurgambaConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }


        public int ExecuteDMLHermes(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strHermesConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        // DB connector to Pull_Neeta database
        public DataSet ExecuteSelectNeeta(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strNeetaConnString);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                
                //throw e;
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        // DB connector to Pull_Neeta database
        public DataSet ExecuteSelectOgive(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strOgiveConnString);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                
                //throw e;
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public DataSet ExecuteSelectOlivea(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnStringOlivea);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                //logger.log("error", "\nStack trace : " + e.StackTrace + "\nMessage : " + e.Message);
                
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public DataSet ExecuteSelectNeetaITS(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnStringNeeta);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public DataSet ExecuteSelectPatel(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnStringPatel);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public int ExecuteDMLNeeta(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strNeetaConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLNeetaITS(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringNeeta);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLMantis2011(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringMantis2011);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }
        public int ExecuteDMLPaulo(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringPaulo);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }
        public int ExecuteDMLPatelTS(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringPatel);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLSriDurgamba(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strSriDurgambaConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        /// <summary>
        /// DML operations on Abhibus operator data on pull server
        /// </summary>
        /// <param name="strSQL"></param>
        /// <param name="cmdType"></param>
        /// <param name="intTimeout"></param>
        /// <returns></returns>
        public int ExecuteDMLAbhibusOperators(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strAbhibusOperatorsConnString);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
               
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }

        public int ExecuteDMLRSRTC(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringRSRTC);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }
        public DataSet ExecuteSelectRSRTC(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnStringRSRTC);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public int ExecuteDMLUPSRTC(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringUPSRTC);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
                
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }
        public DataSet ExecuteSelectUPSRTC(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnStringUPSRTC);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public DataSet ExecuteSelectHRTC(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnStringHRTC);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }

        public int ExecuteDMLMSRTC(string strSQL, CommandType cmdType, int intTimeout)
        {
            int status = 0;
            SqlConnection conn = new SqlConnection(strConnStringMSRTC);

            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                conn.Open();
                cmd.Connection = conn;
                status = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            catch (System.Exception ex)
            {
                status = -1;
               
            }
            finally
            {
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return status;
        }
        public DataSet ExecuteSelectMSRTC(string strSQL, CommandType cmdType, int intTimeout)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adp = new SqlDataAdapter();
            SqlConnection conn = new SqlConnection(strConnStringMSRTC);
            try
            {
                cmd.CommandText = strSQL;
                cmd.CommandType = cmdType;
                cmd.CommandTimeout = intTimeout;
                adp.SelectCommand = cmd;
                conn.Open();
                cmd.Connection = conn;
                adp.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (System.Exception e)
            {
                ds = null;
                
            }
            finally
            {
                adp.Dispose();
                cmd.Cancel();
                conn.Close();
                conn.Dispose();
            }
            return ds;
        }
    }
}