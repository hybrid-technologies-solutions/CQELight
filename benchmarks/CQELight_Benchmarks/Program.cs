﻿using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using CQELight.Tools.Extensions;
using CQELight_Benchmarks.Benchmarks;
using CQELight_Benchmarks.Benchmarks.Buses;
using CQELight_Benchmarks.Benchmarks.DAL;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace CQELight_Benchmarks
{
    public enum TestArea
    {
        None,
        EventStore,
        Bus,
        ALL,
        DAL
    }

    internal static class Consts
    {
        public const string CONST_EVT_IDS_DIR = @"C:\temp_dev\evt_ids\";
        public const string CONST_AGG_IDS_DIR = @"C:\temp_dev\agg_ids\";
    }

    internal static class Program
    {

        #region Public properties

        public static IConfiguration GlobalConfiguration { get; private set; }

        #endregion

        #region Main

        static void Main(string[] args)
        {
            GlobalConfiguration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            Console.WriteLine("CQELight Benchmark application");
            Console.WriteLine("---- MENU -----");

            var testAreas = GetTestAreas();

            foreach (var testArea in testAreas)
            {
                ExecuteTest(testArea);
            }

            Console.WriteLine("Benchmark finished, press Enter to exit");
            Console.ReadLine();

        }

        #endregion

        #region Private methods

        private static void ExecuteTest(TestArea testArea)
        {
            List<Summary> summaries = new List<Summary>();
            if (testArea == TestArea.ALL)
            {
                //Event Stores
                summaries.Add(BenchmarkRunner.Run<MongoDbEventStoreBenchmark>(new Config()));
                summaries.Add(BenchmarkRunner.Run<EFCore_EventStoreBenchmark>(new Config()));
                //summaries.Add(BenchmarkRunner.Run<CosmosDbEventStoreBenchmark>(new Config()));
                //Buses
                summaries.Add(BenchmarkRunner.Run<InMemoryEventBusBenchmark>(new Config()));
                summaries.Add(BenchmarkRunner.Run<InMemoryCommandBusBenchmark>(new Config()));
                //DAL
                summaries.Add(BenchmarkRunner.Run<EFCore_DALBenchmark>(new Config()));

            }
            else if (testArea == TestArea.EventStore)
            {
                Console.WriteLine("Please select Event Store provider you want to test");
                Console.WriteLine("\t1. MongoDb");
                //Console.WriteLine("\t2. CosmosDb");
                Console.WriteLine("\t2. EFCore (SQLServer & SQLite)");

                var result = Console.ReadKey();
                Console.WriteLine();

                switch (result.Key)
                {
                    case ConsoleKey.NumPad1:
                    case ConsoleKey.D1:
                        summaries.Add(BenchmarkRunner.Run<MongoDbEventStoreBenchmark>(new Config()));
                        break;
                    case ConsoleKey.NumPad2:
                    case ConsoleKey.D2:
                        //    summaries.Add(BenchmarkRunner.Run<CosmosDbEventStoreBenchmark>(new Config()));
                        //    break;
                        //case ConsoleKey.NumPad3:
                        //case ConsoleKey.D3:
                        EFCore_EventStoreBenchmark.CreateDatabase(DatabaseType.SQLite);
                        EFCore_EventStoreBenchmark.CreateDatabase(DatabaseType.SQLServer);
                        summaries.Add(BenchmarkRunner.Run<EFCore_EventStoreBenchmark>(new Config()));
                        break;
                }
            }
            else if (testArea == TestArea.Bus)
            {
                Console.WriteLine("Please select bus provider you want to benchmark");
                Console.WriteLine("\t1. InMemory");
                Console.WriteLine("\t2. RabbitMQ (please ensure that it's installed and accessible with 'guest')");

                var result = Console.ReadKey();
                Console.WriteLine();

                switch (result.Key)
                {
                    case ConsoleKey.NumPad1:
                    case ConsoleKey.D1:
                        summaries.Add(BenchmarkRunner.Run<InMemoryEventBusBenchmark>(new Config()));
                        summaries.Add(BenchmarkRunner.Run<InMemoryCommandBusBenchmark>(new Config()));
                        break;
                    case ConsoleKey.NumPad2:
                    case ConsoleKey.D2:
                        summaries.Add(BenchmarkRunner.Run<RabbitMQEventBusBenchmark>(new Config()));
                        break;
                }
            }
            else if (testArea == TestArea.DAL)
            {
                Console.WriteLine("Please select DAL provider you want to benchmark");
                Console.WriteLine("\t1. EF Core");

                var result = Console.ReadKey();
                Console.WriteLine();
                switch (result.Key)
                {
                    case ConsoleKey.NumPad1:
                    case ConsoleKey.D1:
                        summaries.Add(BenchmarkRunner.Run<EFCore_DALBenchmark>(new Config()));
                        break;
                }
            }
            summaries.DoForEach(s => Console.WriteLine(s));
        }

        private static IEnumerable<TestArea> GetTestAreas()
        {
            TestArea? testArea = null;
            while (!testArea.HasValue)
            {
                Console.WriteLine("Please select area you wish to benchmark");
                Console.WriteLine("\t0. ALL");
                Console.WriteLine("\t1. Event store");
                Console.WriteLine("\t2. Bus");
                Console.WriteLine("\t3. DAL");
                var result = Console.ReadKey();
                Console.WriteLine();

                switch (result.Key)
                {
                    case ConsoleKey.NumPad1:
                    case ConsoleKey.D1:
                        yield return TestArea.EventStore;
                        break;
                    case ConsoleKey.NumPad2:
                    case ConsoleKey.D2:
                        yield return TestArea.Bus;
                        break;
                    case ConsoleKey.NumPad3:
                    case ConsoleKey.D3:
                        yield return TestArea.DAL;
                        break;
                    case ConsoleKey.NumPad0:
                    case ConsoleKey.D0:
                        yield return TestArea.ALL;
                        break;
                    default:
                        yield return TestArea.None;
                        break;
                }
            }
            Console.WriteLine();
        }

        #endregion

    }
}
