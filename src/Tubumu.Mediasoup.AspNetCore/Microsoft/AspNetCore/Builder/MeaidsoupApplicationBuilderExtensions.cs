﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tubumu.Libuv;
using Tubumu.Mediasoup;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMediasoup(this IApplicationBuilder app)
        {
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<MediasoupServer>();
            var mediasoupOptions = app.ApplicationServices.GetRequiredService<MediasoupOptions>();
            var mediasoupServer = app.ApplicationServices.GetRequiredService<MediasoupServer>();
            var numberOfWorkers = mediasoupOptions.MediasoupStartupSettings.NumberOfWorkers;
            numberOfWorkers = !numberOfWorkers.HasValue || numberOfWorkers <= 0 ? Environment.ProcessorCount : numberOfWorkers;

            if (mediasoupOptions.MediasoupStartupSettings.WorkerInProcess)
            {
                for (var c = 0; c < numberOfWorkers; c++)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            var worker = app.ApplicationServices.GetRequiredService<WorkerNative>();
                            mediasoupServer.AddWorker(worker);
                        }
                        catch(Exception ex)
                        {
                            logger.LogError(ex, "Worker create failure.");
                        }
                    });
                }
            }
            else
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Loop.Default.Run(() =>
                    {
                        for (var c = 0; c < numberOfWorkers; c++)
                        {
                            var worker = app.ApplicationServices.GetRequiredService<Worker>();
                            worker.On("@success", (_, _) =>
                            {
                                mediasoupServer.AddWorker(worker);
                                logger.LogInformation($"Worker[pid:{worker.ProcessId}] create success.");
                                return Task.CompletedTask;
                            });
                        }
                    });
                });
            }

            return app;
        }
    }
}