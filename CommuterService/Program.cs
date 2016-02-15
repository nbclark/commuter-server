using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Data.SqlClient;
using MobileSrc.Commuter.Shared.RouteServices.Rest;

namespace CommuterService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            new CommuterServiceManager().Start();

            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new CommuterServiceManager() 
			};
            ServiceBase.Run(ServicesToRun);
        }
    }
}
