﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject.Modules;
using NLog;
using NLog.Fluent;

namespace Drive.Logging.DI
{
    public class LoggingModule : NinjectModule
    {
        public override void Load()
        {
            //Bind<ILogger>().To<Logger>();
            Bind<ILogger>().ToMethod(x =>
            {
                var scope = x.Request.ParentRequest.Service.FullName;
                var log = (ILogger)LogManager.GetLogger(scope, typeof(Logger));
                return log;
            });
        }
    }
}
