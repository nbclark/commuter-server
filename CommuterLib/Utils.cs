using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MobileSrc.Commuter.Shared.RouteServices.Rest;

namespace MobileSrc.Commuter.Shared
{
    public static class Utils
    {
        private static string BingMapsApiKey = "AviYccEd2qfMoBG8NMmsl4F3LCL7Q25GPxS4AcwUcg6kFyraTzUvBsBdDYNxa9zV";

        public static TimeSpan RefreshRoute(CommuteDefinition definition, RouteDefinition route, bool reverseRoute)
        {
            RouteRestRequest request = new RouteRestRequest();

            request.Waypoints = new List<RestWaypoint>();
            request.Waypoints.Add(new RestWaypoint());

            request.ApplicationId = BingMapsApiKey;
            request.Waypoints[0].Location = new RestLocation();

            request.Waypoints[0].Location.Latitude = definition.StartPoint.Location.Latitude;
            request.Waypoints[0].Location.Longitude = definition.StartPoint.Location.Longitude;

            foreach (MobileSrc.Commuter.Shared.RouteLocation wayPoint in route.WayPoints)
            {
                request.Waypoints.Add(new RestWaypoint());
                request.Waypoints[request.Waypoints.Count - 1].Location = new RestLocation();
                request.Waypoints[request.Waypoints.Count - 1].Location.Latitude = wayPoint.Location.Latitude;
                request.Waypoints[request.Waypoints.Count - 1].Location.Longitude = wayPoint.Location.Longitude;
            }

            request.Waypoints.Add(new RestWaypoint());
            request.Waypoints[request.Waypoints.Count - 1].Location = new RestLocation();
            request.Waypoints[request.Waypoints.Count - 1].Location.Latitude = definition.EndPoint.Location.Latitude;
            request.Waypoints[request.Waypoints.Count - 1].Location.Longitude = definition.EndPoint.Location.Longitude;

            if (route.AvoidanceMeasures != RouteAvoid.None)
            {
                request.Avoid = route.AvoidanceMeasures;
            }

            if (reverseRoute)
            {
                System.Collections.Generic.List<RestWaypoint> reversed = new System.Collections.Generic.List<RestWaypoint>();

                for (int i = request.Waypoints.Count - 1; i >= 0; --i)
                {
                    reversed.Add(request.Waypoints[i]);
                }

                request.Waypoints = reversed;
            }

            request.Optimize = RouteOptimize.TimeWithTraffic;
            request.PathOutput = RoutePathOutput.Points;

            Route restRoute = request.Execute();
            return TimeSpan.FromSeconds(restRoute.TravelDuration);
        }
        /*
        public static TimeSpan RefreshRoute2(CommuteDefinition definition, RouteDefinition route, bool reverseRoute)
        {
            RouteServices.RouteServiceClient client = new RouteServices.RouteServiceClient("BasicHttpBinding_IRouteService");
            RouteServices.RouteRequest request = new RouteServices.RouteRequest();
            request.Waypoints = new Waypoint[route.WayPoints.Count()+2];
            request.Waypoints[0] = new Waypoint();

            request.Credentials = new Credentials();
            request.Credentials.ApplicationId = BingMapsApiKey;
            request.Waypoints[0].Location = new Location();

            request.Waypoints[0].Location.Latitude = definition.StartPoint.Location.Latitude;
            request.Waypoints[0].Location.Longitude = definition.StartPoint.Location.Longitude;

            int count = 1;
            foreach (MobileSrc.Commuter.Shared.RouteLocation wayPoint in route.WayPoints)
            {
                request.Waypoints[count] = new Waypoint();
                request.Waypoints[count].Location = new Location();
                request.Waypoints[count].Location.Latitude = wayPoint.Location.Latitude;
                request.Waypoints[count].Location.Longitude = wayPoint.Location.Longitude;

                count++;
            }

            request.Waypoints[count] = new Waypoint();
            request.Waypoints[count].Location = new Location();
            request.Waypoints[count].Location.Latitude = definition.EndPoint.Location.Latitude;
            request.Waypoints[count].Location.Longitude = definition.EndPoint.Location.Longitude;

            if (reverseRoute)
            {
                Waypoint[] reversed = new Waypoint[request.Waypoints.Length];

                for (int i = 0; i < request.Waypoints.Length; ++i)
                {
                    reversed[i] = request.Waypoints[request.Waypoints.Length - 1 - i];
                }

                request.Waypoints = reversed;
            }

            // Only accept results with high confidence.
            request.Options = new RouteOptions();
            request.Options.RoutePathType = RoutePathType.Points;
            request.Options.TrafficUsage = TrafficUsage.TrafficBasedTime;

            RouteResponse response = client.CalculateRoute(request);

            return TimeSpan.FromSeconds(response.Result.Summary.TimeInSeconds);
        }
        */
    }
}
