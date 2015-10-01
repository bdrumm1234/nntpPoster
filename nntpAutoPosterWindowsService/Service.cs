﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using log4net;
using nntpAutoposter;

namespace nntpAutoPosterWindowsService
{
    public partial class Service : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        Watcher watcher;
        AutoPoster poster;
        IndexerNotifier notifier;
        IndexerVerifier verifier;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                var configuration = new AutoPosterConfig();

                watcher = new Watcher(configuration);
                watcher.Start();
                log.Info("FileSystemWatcher started");

                poster = new AutoPoster(configuration);
                poster.Start();
                log.Info("Autoposter started");

                notifier = new IndexerNotifier(configuration);
                notifier.Start();
                log.Info("Notifier started");

                verifier = new IndexerVerifier(configuration);
                verifier.Start();
                log.Info("Verifier started");
            
            }
            catch (Exception ex)
            {
                log.Fatal("Fatal exception when starting the autoposter.", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                watcher.Stop(2000);
                log.Info("FileSystemWatcher stopped");

                verifier.Stop(2000);
                log.Info("Verifier stopped");

                notifier.Stop(2000);
                log.Info("Notifier stopped");

                poster.Stop(20000);
                log.Info("Autoposter stopped");
            }
              catch (Exception ex)
              {
                  log.Fatal("Fatal exception when stopping the autoposter.", ex);
                  throw;
              }
        }
    }
}