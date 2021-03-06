﻿using Covid.WorkerService.Configuration;
using log4net;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Topshelf;

namespace Covid.WorkerService
{
    class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            //.AddXmlFile("App.config", optional: false, reloadOnChange: true)
            //.AddXmlFile($"App.{environmentName}.config", optional: true)
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();

            //Console.WriteLine(configuration.GetConnectionString("Storage"));
            var apis = (configuration.GetSection("apis").Get<IEnumerable<Api>>()).ToDictionary(k => k.Name, v => v);
            
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead("log4net.config"));

            var repo = log4net.LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);

            _logger.Info("Application - Main is invoked");

            HostFactory.Run(x =>
            {
                x.Service<WorkerService>(() => new WorkerService(apis["local"]));
                x.EnableServiceRecovery(r => r.RestartService(TimeSpan.FromSeconds(10)));
                x.SetServiceName("ServiceTemplate");
                x.StartAutomatically();
            });
        }
    }
}
