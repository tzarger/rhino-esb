﻿using System;
using System.IO;
using System.Reflection;
using Common.Logging;

namespace Rhino.ServiceBus.Host
{
    using System.ServiceProcess;
    using Hosting;

    internal partial class RhinoServiceBusHost : ServiceBase
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private RemoteAppDomainHost host;
        private string asm;
        private string cfg;
        private string bootStrapper;
        private string hostType;

        public RhinoServiceBusHost()
        {
            InitializeComponent();
        }

        public void SetArguments(ExecutingOptions options)
        {
            asm = options.Assembly;
            cfg = options.ConfigFile;
            bootStrapper = options.BootStrapper;
            hostType = options.Host;
        }

        protected override void OnStart(string[] ignored)
        {
            try
            {
                if (string.IsNullOrEmpty(bootStrapper) == false)
                {
                    var assembly = LoadAssembly();
                    var bootStrapperType = LoadBootStrapperType(assembly);
                    host = new RemoteAppDomainHost(bootStrapperType);
                    host.Configuration(cfg);
                }
                else
                {
                    host = new RemoteAppDomainHost(asm, cfg);
                }

                if (string.IsNullOrEmpty(hostType) == false)
                {
                    host.SetHostType(hostType);
                }

                host.Start();
            }
            catch (Exception x)
            {
                Log.Fatal("Hosted service failed to start", x);
                throw;
            }
        }

        private Assembly LoadAssembly()
        {
            try
            {
                return Assembly.LoadFrom(asm);
            }
            catch (FileNotFoundException)
            {
                throw new InvalidOperationException("The specified assembly file was not found: " + asm);
            }
        }

        private Type LoadBootStrapperType(Assembly assembly)
        {
            var type = assembly.GetType(bootStrapper);

            if (type == null)
            {
                throw new InvalidOperationException("Unable to load the specified bootstrapper type: " + bootStrapper);
            }

            return type;
        }

        protected override void OnStop()
        {
            try
            {
                if (host != null)
                    host.Close();
            }
            catch (Exception x)
            {
                Log.Fatal("Hosted service failed to stop", x);
                throw;
            }
        }

        public void DebugStart(string[] arguments)
        {
            OnStart(arguments);
        }

        public void InitialDeployment(string user)
        {
            var tmpHost = new RemoteAppDomainHost(asm, cfg);
            tmpHost.InitialDeployment(user);
            tmpHost.Close();

        }
    }
}