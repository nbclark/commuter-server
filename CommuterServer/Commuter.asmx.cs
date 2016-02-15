using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml.Serialization;
using System.IO;
using System.Text;

namespace MobileSrc.Commuter.Server
{
    /// <summary>
    /// Summary description for Commuter
    /// </summary>
    [WebService(Namespace = "")]
    [WebServiceBinding(ConformsTo = WsiProfiles.None)]
    [ToolboxItem(false)]
    public class Commuter : System.Web.Services.WebService
    {
        private void LogRequest(Guid deviceId, string action)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand("AddLog", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("idDevice", deviceId);
                    command.Parameters.AddWithValue("Action", action);

                    command.ExecuteNonQuery();
                }
            }
            catch
            {
            }
        }

        [WebMethod]
        public bool RegisterDevice(Guid deviceId, string name, bool enableTile, bool enableToast, int timeZoneOffset, string accentColor, string channelURI, MobileSrc.Commuter.Shared.CommuteDefinition[] commutes)
        {
            LogRequest(deviceId, "RegisterDevice");
            try
            {
                using (SqlConnection connection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand("AddDevice", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("idDevice", deviceId);
                    command.Parameters.AddWithValue("Name", name);
                    command.Parameters.AddWithValue("timeZoneOffset", timeZoneOffset);
                    command.Parameters.AddWithValue("enableTile", enableTile);
                    command.Parameters.AddWithValue("enableToast", enableToast);
                    command.Parameters.AddWithValue("accentColor", accentColor);
                    command.Parameters.AddWithValue("channelURI", channelURI);

                    command.ExecuteNonQuery();
                }

                foreach (MobileSrc.Commuter.Shared.CommuteDefinition commute in commutes)
                {
                    AddCommute(deviceId, commute);
                }
                using (SqlConnection connection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand("CleanupDevice", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("idDevice", deviceId);
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        static XmlSerializer _routeSerializer = new XmlSerializer(typeof(MobileSrc.Commuter.Shared.RouteDefinition));
        static XmlSerializer _commuteSerializer = new XmlSerializer(typeof(MobileSrc.Commuter.Shared.CommuteDefinition));

        [WebMethod]
        public MobileSrc.Commuter.Shared.CommuteHistory Test()
        {
            return GetCommuteHistory(Guid.Empty, new Guid("6026E5EA-7FEA-404C-BCE2-95666B081D33"));
        }

        [WebMethod]
        public MobileSrc.Commuter.Shared.NotificationResponse RequestTileUpdate(string channelURI, string imageURI)
        {
            return MobileSrc.Commuter.Shared.Notifications.SendTileUpdate(channelURI, "", 0, imageURI);
        }

        [WebMethod]
        public MobileSrc.Commuter.Shared.CommuteHistory GetCommuteHistory(Guid deviceId, Guid commuteId)
        {
            LogRequest(deviceId, "GetCommuteHistory");
            MobileSrc.Commuter.Shared.CommuteHistory history = new MobileSrc.Commuter.Shared.CommuteHistory();

            try
            {
                using (SqlConnection connection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand("GetCommuteHistory", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("idCommute", commuteId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Dictionary<Guid, MobileSrc.Commuter.Shared.RouteHistory> routes = new Dictionary<Guid, MobileSrc.Commuter.Shared.RouteHistory>();

                        while (reader.Read())
                        {
                            Guid idRoute = (Guid)reader["idRoute"];

                            if (!routes.ContainsKey(idRoute))
                            {
                                MobileSrc.Commuter.Shared.RouteHistory routeHistory = new MobileSrc.Commuter.Shared.RouteHistory();
                                routeHistory.RouteId = idRoute;
                                history.Routes.Add(routeHistory);
                                routes.Add(idRoute, routeHistory);
                            }
                            MobileSrc.Commuter.Shared.RouteHistory.RouteHistoryDay day = new MobileSrc.Commuter.Shared.RouteHistory.RouteHistoryDay();
                            day.Day = (DayOfWeek)Convert.ToInt32(reader["Day"]);
                            day.Minutes = Convert.ToDouble(reader["Duration"]);

                            bool isDeparture = Convert.ToBoolean(reader["IsDeparture"]);

                            if (isDeparture)
                            {
                                routes[idRoute].DepartureAverages.Add(day);
                            }
                            else
                            {
                                routes[idRoute].ReturnAverages.Add(day);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return history;
        }

        [WebMethod]
        public MobileSrc.Commuter.Shared.CommuteHistory ClearCommuteHistory(Guid deviceId, Guid commuteId)
        {
            LogRequest(deviceId, "ClearCommuteHistory");

            try
            {
                using (SqlConnection connection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand("ClearCommuteHistory", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("idCommute", commuteId);
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
            }

            return GetCommuteHistory(deviceId, commuteId);
        }

        [WebMethod]
        public bool AddCommute(Guid deviceId, MobileSrc.Commuter.Shared.CommuteDefinition commute)
        {
            LogRequest(deviceId, "AddCommute");
            try
            {
                using (SqlConnection connection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand("AddCommute", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("idCommute", commute.Id);
                    command.Parameters.AddWithValue("idDevice", deviceId);
                    command.Parameters.AddWithValue("DepartTime", commute.DepartureTime);
                    command.Parameters.AddWithValue("ReturnTime", commute.ReturnTime);
                    command.Parameters.AddWithValue("DaysOfWeek", commute.DaysOfWeek);

                    using (StringWriter writer = new StringWriter())
                    {
                        _commuteSerializer.Serialize(writer, commute);
                        command.Parameters.AddWithValue("Definition", writer.ToString());
                    }

                    command.ExecuteNonQuery();
                }

                foreach (MobileSrc.Commuter.Shared.RouteDefinition route in commute.Routes)
                {
                    AddRoute(deviceId, commute.Id, route);
                }

                using (SqlConnection connection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand("CleanupCommute", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("idCommute", commute.Id);
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        [WebMethod]
        public bool AddRoute(Guid deviceId, Guid commuteId, MobileSrc.Commuter.Shared.RouteDefinition route)
        {
            LogRequest(deviceId, "AddRoute");
            try
            {
                using (SqlConnection connection = new SqlConnection(@"Data Source=wpcommuter.db.3448251.hostedresource.com; Initial Catalog=wpcommuter; User ID=wpcommuter; Password='Ce86944';"))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand("AddRoute", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("idRoute", route.Id);
                    command.Parameters.AddWithValue("idCommute", commuteId);

                    using (StringWriter writer = new StringWriter())
                    {
                        _routeSerializer.Serialize(writer, route);
                        command.Parameters.AddWithValue("Definition", writer.ToString());
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
