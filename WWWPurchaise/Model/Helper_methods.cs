using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using System.Xml.Linq;
//Schema for Settings.xml file please create it for manage instances of helper class
//<? xml version = "1.0" encoding = "utf-8" ?>
//  < DEFINITONS >
//   
//     < ORACLE >
//       < ConnectionString > Password = pass; User ID = user; Data Source = dbName </ ConnectionString >     
//     </ ORACLE >
//         
//     < POSTEGRESQL >     
//          < Host > IP of DBComp</Host>
//          <Port>5432</Port>
//          <CommandTimeout>0</CommandTimeout>
//          <ConnectionIdleLifetime >30</ConnectionIdleLifetime>
//          <ApplicationName>APPLICATION NAME</ApplicationName>
//          <Username>user</Username>
//          <Password>Password</Password>
//          <Database>DBname</Database>
//    </POSTEGRESQL>
//    <MAIL>
//          <Port>587</Port>
//          <DeliveryFormat>1</DeliveryFormat>
//          <Host>HOST NAme</Host>
//          <EnableSsl>false</EnableSsl>
//          <Timeout>10000</Timeout>
//          <DeliveryMethod>0</DeliveryMethod>
//          <UseDefaultCredentials>false</UseDefaultCredentials>
//          <CredentialsUserName>User email</CredentialsUserName>
//          <CredentialsPassword>Password</CredentialsPassword>
//    </MAIL>
//  </DEFINITONS>
namespace WWWPurchaise.Model
{
    /// <summary>
    /// Get connetions settings to Oracle
    /// </summary>
    public static class Oracle_conn
    {
        /// <summary>
        /// Get settings for connections with ORACLE
        /// </summary>
        public static string Connection_string { get; set; }
        /// <summary>
        /// Initialize data from XML
        /// </summary>
        static Oracle_conn()
        {
            try
            {
                XDocument Doc = XDocument.Load("Settings.xml");
                var oraconn = Doc.Descendants("ORACLE")
                    .Select(x => new
                    {
                        XConnection_string = (string)x.Element("ConnectionString")
                    });
                foreach (var res in oraconn)
                {
                    Connection_string = res.XConnection_string;
                }
            }
            catch (Exception e)
            {
                Loger.Log("Błąd pobrania ustawień połączenia z ORACLE: " + e);
            }
        }
    }
    /// <summary>
    /// Settings for postegresql Database conections
    /// </summary>
    public static class Postegresql_conn
    {
        public static NpgsqlConnectionStringBuilder Conn_set { get; set; }
        public static string Host { get; set; }
        public static int Port { get; set; }
        public static int CommandTimeout { get; set; }
        public static int ConnectionIdleLifetime { get; set; }
        public static string ApplicationName { get; set; }
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static string Database { get; set; }
        /// <summary>
        /// Initialize data from XML
        /// </summary>
        static Postegresql_conn()
        {

            try
            {
                XDocument Doc = XDocument.Load("Settings.xml");
                var pstgr = Doc.Descendants("POSTEGRESQL")
                    .Select(x => new
                    {
                        XHost = (string)x.Element("Host"),
                        XPort = Convert.ToInt32((string)x.Element("Port")),
                        XCommandTimeout = Convert.ToInt32((string)x.Element("CommandTimeout")),
                        XConnectionIdleLifetime = Convert.ToInt32((string)x.Element("ConnectionIdleLifetime")),
                        XApplicationName = (string)x.Element("ApplicationName"),
                        XUsername = (string)x.Element("Username"),
                        XPassword = (string)x.Element("Password"),
                        XDatabase = (string)x.Element("Database")
                    });
                foreach (var res in pstgr)
                {
                    Host = res.XHost;
                    Port = res.XPort;
                    CommandTimeout = res.XCommandTimeout;
                    ConnectionIdleLifetime = res.XConnectionIdleLifetime;
                    ApplicationName = res.XApplicationName;
                    Username = res.XUsername;
                    Password = res.XPassword;
                    Database = res.XDatabase;
                }
                Conn_set = new NpgsqlConnectionStringBuilder()
                {
                    Host = Host,
                    Port = Port,
                    ConnectionIdleLifetime = ConnectionIdleLifetime,
                    ApplicationName = ApplicationName,
                    Username = Username,
                    Password = Password,
                    Database = Database
                };
            }
            catch (Exception e)
            {
                Loger.Log("Błąd pobrania ustawień połączenia z POSTEGRESQL: " + e);
            }
        }
    }
    /// <summary>
    /// Simple logger class
    /// </summary>  
    public class Loger 
    {
        /// <summary>
        /// DateTime of Start service
        /// </summary>
        private static DateTime serw_run = DateTime.Now;
        /// <summary>
        /// String with data logs
        /// </summary>
        public static string Log_rek = "";
        public static void Log(string txt)
        {
            Log_rek = Log_rek + Environment.NewLine + txt;
        }
        public static void Srv_start()
        {
            if (Log_rek != "") { Srv_stop(); }
            Serw_run = DateTime.Now;
            Log_rek = "Logs Started at " + Serw_run;
        }
        public static void Srv_stop()
        {
            Save_stat_refr();
            Log_rek = "";
        }
        private static void Save_stat_refr()
        {
            try
            {
                string npA = Postegresql_conn.Conn_set.ToString();
                using (NpgsqlConnection conA = new NpgsqlConnection(npA))
                {
                    conA.Open();
                    using (NpgsqlTransaction tr_savelogs = conA.BeginTransaction())
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand("" +
                            "INSERT INTO public.server_query" +
                            "(start_date, end_dat, errors_found, log, id) " +
                            "VALUES" +
                            "(@run,@end,@er,@log,@id); ", conA))
                        {
                            cmd.Parameters.Add("run", NpgsqlTypes.NpgsqlDbType.Timestamp);
                            cmd.Parameters.Add("end", NpgsqlTypes.NpgsqlDbType.Timestamp);
                            cmd.Parameters.Add("er", NpgsqlTypes.NpgsqlDbType.Integer);
                            cmd.Parameters.Add("log", NpgsqlTypes.NpgsqlDbType.Text);
                            cmd.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Uuid);
                            cmd.Prepare();
                            string searchTerm = "Błąd";
                            string[] source = Log_rek.Split(new char[] { '.', '?', '!', ' ', ';', ':', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            var matchQuery = from word in source
                                             where word.ToLowerInvariant() == searchTerm.ToLowerInvariant()
                                             select word;
                            cmd.Parameters[0].Value = Serw_run;
                            cmd.Parameters[1].Value = DateTime.Now;
                            cmd.Parameters[2].Value = matchQuery.Count();
                            cmd.Parameters[3].Value = Log_rek;
                            cmd.Parameters[4].Value = System.Guid.NewGuid();
                            cmd.ExecuteNonQuery();
                        }
                        using (NpgsqlCommand cmd = new NpgsqlCommand("" +
                            "DELETE FROM public.server_query " +
                            "WHERE start_date<current_timestamp - interval '14 day '  ", conA))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        tr_savelogs.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                Log("Eeee coś nie działa: " + e);
            }
        }  
        public static DateTime Serw_run { get => serw_run; set => serw_run = value; }
    }
}
