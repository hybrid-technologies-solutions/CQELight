﻿using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using CQELight_Benchmarks.Custom;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace CQELight_Benchmarks
{
    internal class Config : ManualConfig
    {

        public Config()
        {
            Add(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLS        
            Add(DefaultConfig.Instance.GetLoggers().ToArray());
            Add(DefaultConfig.Instance.GetExporters().ToArray());
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
            Orderer = new CustomOrderProvider();
        }

        private class CustomOrderProvider : IOrderer// IOrderProvider
        {
            public bool SeparateLogicalGroups => false;

            public string GetGroupKey(BenchmarkCase benchmark, Summary summary) => null;

            public string GetHighlightGroupKey(BenchmarkCase benchmarkCase)
            {
                return benchmarkCase.Parameters.DisplayInfo;
            }
            
            public IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder
                (IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups)
                => logicalGroups;

            public IEnumerable<BenchmarkCase> GetExecutionOrder(ImmutableArray<BenchmarkCase> benchmarksCase)
                => from benchmark in benchmarksCase
                   orderby benchmark.Descriptor.WorkloadMethod.GetCustomAttribute<BenchmarkOrderAttribute>()?.Order ?? 1
                   select benchmark;

            public IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCases, Summary summary)
                =>
                from benchmark in benchmarksCases
                orderby summary[benchmark].ResultStatistics?.Mean
                select benchmark;

            public string GetLogicalGroupKey(ImmutableArray<BenchmarkCase> allBenchmarksCases, BenchmarkCase benchmarkCase)
                => "*";
        }

    }


}
