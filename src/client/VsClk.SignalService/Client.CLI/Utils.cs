// <copyright file="Utils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace SignalService.Client.CLI
{
    internal static class Utils
    {
        public static void ReadStringValue(string message, ref string value)
        {
            Console.Write($"{message}({value}):");
            var line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                value = line;
            }
        }

        public static void ReadIntValue(string message, ref int value)
        {
            Console.Write($"{message}({value}):");
            var line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                value = int.Parse(line);
            }
        }

        public static void ReadBoolValue(string message, ref bool value)
        {
            Console.Write($"{message}({value}):");
            var line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                value = bool.Parse(line);
            }
        }

        public static async Task WaitAllAsync<TResult>(
            List<Task<TResult>> tasks,
            Action<TResult> onResult,
            TraceSource traceSource,
            CancellationToken cancellationToken)
        {
            while (tasks.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var task = await Task.WhenAny(tasks);
                    tasks.Remove(task);
                    var result = await task;
                    onResult(result);
                }
                catch (Exception error)
                {
                    traceSource.Error($"Failed to complete task with error:{error.Message}");
                }
            }
        }

        public static async Task WaitAllAsync(
            List<Task> tasks,
            TraceSource traceSource,
            CancellationToken cancellationToken)
        {
            while (tasks.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var task = await Task.WhenAny(tasks);
                    tasks.Remove(task);
                    await task;
                }
                catch (Exception error)
                {
                    traceSource.Error($"Failed to complete task with error:{error.Message}");
                }
            }
        }
    }
}
