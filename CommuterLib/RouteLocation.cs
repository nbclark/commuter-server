using System;
using System.Collections.Generic;

namespace CommuterLib.Routes
{
    public class GpsLocation
    {
        public GpsLocation()
        {

        }
        public double Latitude
        {
            get;
            set;
        }
        public double Longitude
        {
            get;
            set;
        }
        public double Altitude
        {
            get;
            set;
        }
    }
    public class RouteLocation
    {
        public RouteLocation()
        {
            this.Address = string.Empty;
        }
        public GpsLocation Location
        {
            get;
            set;
        }
        public string Address
        {
            get;
            set;
        }
    }
    public class RouteDefinition
    {
        public event EventHandler Updated;

        public RouteDefinition()
        {
            this.WayPoints = new List<RouteLocation>();
            this.RoutePoints = new List<GpsLocation>();
            this.EstimatedDuration = TimeSpan.FromHours(0.234);
        }
        public List<RouteLocation> WayPoints
        {
            get;
            set;
        }
        public List<GpsLocation> RoutePoints
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
        public TimeSpan EstimatedDuration
        {
            get;
            set;
        }
        public double EstimatedDistance
        {
            get;
            set;
        }
        public string TravelSummary
        {
            get { return string.Format("{0} miles, {1} minutes", this.EstimatedDistance, this.EstimatedDuration.TotalMinutes); }
        }
        public DateTime LastUpdated
        {
            get;
            set;
        }
        internal void FireUpdated()
        {
            if (null != Updated)
            {
                Updated(this, null);
            }
        }
    }
    public class CommuteDefinition
    {
        public event EventHandler Updated;

        public CommuteDefinition()
        {
            this.Routes = new List<RouteDefinition>();
            this.StartPoint = new RouteLocation();
            this.EndPoint = new RouteLocation();
        }

        public RouteLocation StartPoint
        {
            get;
            set;
        }

        public RouteLocation EndPoint
        {
            get;
            set;
        }

        public List<RouteDefinition> Routes
        {
            get;
            set;
        }

        public virtual string Name
        {
            get;
            set;
        }

        public DateTime DepartureTime
        {
            get;
            set;
        }

        public DateTime ReturnTime
        {
            get;
            set;
        }
        public virtual string Description
        {
            get
            {
                return string.Concat(this.DepartureTime.ToShortTimeString(), " - ", this.ReturnTime.ToShortTimeString());
            }
        }
        public DateTime LastUpdated
        {
            get;
            set;
        }
        internal void FireUpdated()
        {
            if (null != Updated)
            {
                Updated(this, null);
            }
        }
    }

    public class IntroCommuteDefinition : CommuteDefinition
    {
        public override string Name
        {
            get
            {
                return "home";
            }
            set { }
        }
        public override string Description
        {
            get
            {
                return "commuter";
            }
        }
    }
}
