using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Data.SqlClient;
using System.Xml.Serialization;

namespace CommuterService
{
    public partial class CommuterServiceManager : ServiceBase
    {
        public CommuterServiceManager()
        {
            InitializeComponent();
        }

        private Thread _workerThread;
        private Dictionary<Guid, Thread> _runningThreads = new Dictionary<Guid, Thread>();
        private static readonly int DurationQueryRange = 10; //todo: change to 60
        private static readonly int DurationQueryInterval = 10;

        public void Start()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            _workerThread = new Thread(WorkerThreadProc);
            _workerThread.IsBackground = true;
            _workerThread.Start();
        }

        private void WorkerThreadProc()
        {
            DateTime lastUpdate = DateTime.Now.AddHours(-2).ToUniversalTime();

            while (true)
            {
                try
                {
                    Console.WriteLine(string.Format("{0} - Checking for Updates Started", DateTime.Now.ToString("MM/dd hh:mm tt")));

                    List<Guid> finishedThreads = new List<Guid>();
                    foreach (Guid key in _runningThreads.Keys)
                    {
                        if (0 != (_runningThreads[key].ThreadState & System.Threading.ThreadState.Stopped))
                        {
                            finishedThreads.Add(key);
                        }
                    }
                    foreach (Guid key in finishedThreads)
                    {
                        _runningThreads.Remove(key);
                    }

                    List<AutoResetEvent> waiters = new List<AutoResetEvent>();

                    using (SqlConnection connection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                    {
                        using (SqlConnection writeConnection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                        {
                            connection.Open();
                            writeConnection.Open();

                            DateTime now = DateTime.Now.ToUniversalTime();
                            SqlCommand command = new SqlCommand("GetRequiredUpdates", connection);
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("LastUpdate", lastUpdate);
                            command.Parameters.AddWithValue("CurrentTime", now);
                            command.Parameters.AddWithValue("DurationQueryRange", DurationQueryRange);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                // Read departures
                                while (reader.Read())
                                {
                                    LaunchProcessUpdate(writeConnection, reader);
                                    //waiters.Add(LaunchProcessUpdate(writeConnection, reader));
                                }

                                reader.NextResult();

                                // Read returns
                                while (reader.Read())
                                {
                                    LaunchProcessUpdate(writeConnection, reader);
                                    //waiters.Add(LaunchProcessUpdate(writeConnection, reader));
                                }
                            }

                            lastUpdate = now;
                        }
                    }
                    /*
                    // Wait for 200 seconds
                    for (int count = 0; count < 20; ++count)
                    {
                        for (int i = 0; i < waiters.Count; ++i)
                        {
                            if (waiters[i].WaitOne(0))
                            {
                                waiters.RemoveAt(i);
                                i--;
                            }
                        }
                        if (waiters.Count == 0)
                        {
                            break;
                        }
                        Thread.Sleep(1000 * 10);
                    }
                    */
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("{0} - Check Exception: {1}", DateTime.Now.ToString("MM/dd hh:mm tt"), ex.ToString()));
                }
                Console.WriteLine(string.Format("{0} - Checking for Updates Finished", DateTime.Now.ToString("MM/dd hh:mm tt")));
                // tick every 5 minutes
                Thread.Sleep(1000 * 60 * 5);
            }
        }

        private AutoResetEvent LaunchProcessUpdate(SqlConnection writeConnection, SqlDataReader reader)
        {
            try
            {
                bool isDeparture = Convert.ToBoolean(reader["Departure"]);
                string channelUri = (string)reader["ChannelURI"];
                DateTime sendTime = DateTime.Now.Date.Add(((DateTime)reader["SendTime"]).ToLocalTime().TimeOfDay);
                double timeZoneOffset = Convert.ToDouble(reader["TimeZoneOffset"]);
                string accentColor = Convert.ToString(reader["AccentColor"]);

                MobileSrc.Commuter.Shared.CommuteDefinition commute;

                using (StringReader stringReader = new StringReader(Convert.ToString(reader["CommuteDefinition"])))
                {
                    commute = (MobileSrc.Commuter.Shared.CommuteDefinition)_commuteDeserializer.Deserialize(stringReader);
                }

                if (!_runningThreads.ContainsKey(commute.Id))
                {
                    AutoResetEvent waiter = new AutoResetEvent(false);
                    Thread thread = new Thread(new ParameterizedThreadStart(ProcessUpdate));
                    thread.IsBackground = true;

                    _runningThreads.Add(commute.Id, thread);

                    thread.Start(new object[] { waiter, commute, isDeparture, channelUri, timeZoneOffset, accentColor, sendTime });

                    return waiter;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("{0} - Exception - {1}", DateTime.Now.ToString("MM/dd hh:mm tt"), ex.ToString()));
            }

            return null;
        }

        static XmlSerializer _routeDeserializer = new XmlSerializer(typeof(MobileSrc.Commuter.Shared.RouteDefinition));
        static XmlSerializer _commuteDeserializer = new XmlSerializer(typeof(MobileSrc.Commuter.Shared.CommuteDefinition));

        private static void ProcessUpdate(object param)
        {
            // We want to loop through
            try
            {
                object[] args = (object[])param;

                AutoResetEvent waiter = (AutoResetEvent)args[0];
                bool isDeparture = (bool)args[2];
                string channelUri = (string)args[3];
                double timeZoneOffset = (double)args[4];
                string accentColor = (string)args[5];
                DateTime sendTime = (DateTime)args[6];

                MobileSrc.Commuter.Shared.CommuteDefinition commute = (MobileSrc.Commuter.Shared.CommuteDefinition)args[1];

                Directory.CreateDirectory("logs");
                using (FileStream fs = File.Create(string.Format(@"logs\{0}_{1}.log", commute.Id, DateTime.Now.ToString("MM_dd_hh_tt"))))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.AutoFlush = true;

                        DateTime startTime = DateTime.Now;
                        //DateTime sendTime = startTime.AddMinutes(DurationQueryRange);
                        DateTime endTime = sendTime.AddMinutes(DurationQueryRange);

                        Console.WriteLine(string.Format("{0} - Processing Update for {1}", DateTime.Now.ToString("MM/dd hh:mm tt"), commute.Name));
                        sw.WriteLine(string.Format("{0} - Processing Update for {1}", DateTime.Now.ToString("MM/dd hh:mm tt"), commute.Name));

                        bool hasSentUpdate = false;
                        while (DateTime.Now <= endTime)
                        {
                            try
                            {
                                SortedList<TimeSpan, MobileSrc.Commuter.Shared.RouteDefinition> times = new SortedList<TimeSpan, MobileSrc.Commuter.Shared.RouteDefinition>();
                                MobileSrc.Commuter.Shared.RouteDefinition bestRoute = null;

                                bool sendUpdate = false;

                                if (!hasSentUpdate && DateTime.Now >= sendTime)
                                {
                                    sendUpdate = true;
                                    hasSentUpdate = true;
                                }

                                foreach (MobileSrc.Commuter.Shared.RouteDefinition route in commute.Routes)
                                {
                                    TimeSpan duration = MobileSrc.Commuter.Shared.Utils.RefreshRoute(commute, route, !isDeparture);

                                    while (times.ContainsKey(duration))
                                    {
                                        duration = duration.Add(TimeSpan.FromSeconds(1));
                                    }
                                    times.Add(duration, route);

                                    try
                                    {
                                        using (SqlConnection writeConnection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                                        {
                                            writeConnection.Open();
                                            SqlCommand updateCommand = new SqlCommand("UpdateRoute", writeConnection);
                                            updateCommand.CommandType = CommandType.StoredProcedure;
                                            updateCommand.Parameters.AddWithValue("idRoute", route.Id);
                                            updateCommand.Parameters.AddWithValue("idCommute", commute.Id);
                                            updateCommand.Parameters.AddWithValue("Date", DateTime.Now.ToUniversalTime());
                                            updateCommand.Parameters.AddWithValue("Duration", duration.TotalMinutes);
                                            updateCommand.Parameters.AddWithValue("IsDeparture", isDeparture);
                                            updateCommand.Parameters.AddWithValue("IsRequested", sendUpdate);

                                            updateCommand.ExecuteNonQuery();
                                        }
                                    }
                                    catch (Exception ex2)
                                    {
                                        Console.WriteLine(string.Format("{0} - Exception for {1} - {2}", DateTime.Now.ToString("MM/dd hh:mm tt"), commute.Name, ex2.ToString()));
                                        sw.WriteLine(string.Format("{0} - Exception for {1} - {2}", DateTime.Now.ToString("MM/dd hh:mm tt"), commute.Name, ex2.ToString()));
                                    }
                                }

                                if (sendUpdate && times.Count > 0)
                                {
                                    bestRoute = times.Values[0];

                                    DateTime updateTime = DateTime.Now.AddHours(timeZoneOffset);
                                    string tileUrl = string.Format(@"http://mobilesrc.com/commuter/LiveTile.aspx?commute={0}&route={1}&duration={2}&interval={3}&day={4}&color={5}", commute.Name, bestRoute.Name, (int)bestRoute.EstimatedDuration.TotalMinutes, "min", updateTime.AddHours(-timeZoneOffset).ToString("dddd @ hh:mm tt"), accentColor);
                                    MobileSrc.Commuter.Shared.NotificationResponse toast = MobileSrc.Commuter.Shared.Notifications.SendToast(channelUri, commute.Name, string.Format("{0} {1} minutes", times.Values[0].Name, (int)times.Keys[0].TotalMinutes));
                                    MobileSrc.Commuter.Shared.NotificationResponse tile = MobileSrc.Commuter.Shared.Notifications.SendTileUpdate(channelUri, "", 0, tileUrl);

                                    Console.WriteLine(string.Format("{0} - Responses for {1} [toast: {2}, tile: {3}]", DateTime.Now.ToString("MM/dd hh:mm tt"), commute.Name, toast, tile));
                                    sw.WriteLine(string.Format("{0} - Responses for {1} [toast: {2}, tile: {3}]", DateTime.Now.ToString("MM/dd hh:mm tt"), commute.Name, toast, tile));
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(string.Format("{0} - Exception for {1} - {2}", DateTime.Now.ToString("MM/dd hh:mm tt"), commute.Name, ex.ToString()));
                                sw.WriteLine(string.Format("{0} - Exception for {1} - {2}", DateTime.Now.ToString("MM/dd hh:mm tt"), commute.Name, ex.ToString()));
                            }
                            finally
                            {
                                waiter.Set();
                            }
                            Thread.Sleep(DurationQueryInterval * 60 * 1000);
                        }
                        Console.WriteLine(string.Format("{0} - Finished Update for {1}", DateTime.Now.ToString("MM/dd hh:mm tt"), commute.Name));
                        sw.WriteLine(string.Format("{0} - Finished Update for {1}", DateTime.Now.ToString("MM/dd hh:mm tt"), commute.Name));
                    }
                }
            }
            catch
            {
            }
        }

        protected override void OnStop()
        {
        }
    }
}
