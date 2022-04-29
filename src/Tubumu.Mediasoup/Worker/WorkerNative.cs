﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tubumu.Utils.Extensions;

namespace Tubumu.Mediasoup
{
    public class WorkerNative : WorkerBase
    {
        #region P/Invoke Channel

        private static readonly LibMediasoupWorkerNative.ChannelReadFreeFn _channelReadFree = ChannelReadFree;

        private static void ChannelReadFree(IntPtr message, uint messageLen, IntPtr messageCtx)
        {
            ;
        }

        private static readonly LibMediasoupWorkerNative.ChannelReadFn _channelRead = ChannelRead;

        private static LibMediasoupWorkerNative.ChannelReadFreeFn? ChannelRead(IntPtr message, IntPtr messageLen, IntPtr messageCtx,
            IntPtr handle, IntPtr ctx)
        {
            return null;
            return _channelReadFree;
        }

        private static readonly LibMediasoupWorkerNative.ChannelWriteFn _channelWrite = ChannelWrite;

        private static void ChannelWrite(string message, uint messageLen, IntPtr ctx)
        {
            ;
        }

        #endregion

        #region P/Invoke PayloadChannel

        private static readonly LibMediasoupWorkerNative.PayloadChannelReadFreeFn _payloadChannelReadFree = PayloadChannelReadFree;

        private static void PayloadChannelReadFree(IntPtr message, uint messageLen, IntPtr messageCtx)
        {
            ;
        }

        private static readonly LibMediasoupWorkerNative.PayloadChannelReadFn _payloadChannelRead = PayloadChannelRead;

        internal static LibMediasoupWorkerNative.PayloadChannelReadFreeFn? PayloadChannelRead(IntPtr message, IntPtr messageLen, IntPtr messageCtx,
            IntPtr payload, IntPtr payloadLen, IntPtr payloadCapacity,
            IntPtr handle, IntPtr ctx)
        {
            return null;
            return _payloadChannelReadFree;
        }

        private static readonly LibMediasoupWorkerNative.PayloadChannelWriteFn _payloadchannelWrite = PayloadChannelWrite;

        private static void PayloadChannelWrite(string message, uint messageLen,
            IntPtr payload, uint payloadLen,
            IntPtr ctx)
        {
            ;
        }

        #endregion

        private readonly IntPtr _ptr;

        public WorkerNative(ILoggerFactory loggerFactory, MediasoupOptions mediasoupOptions) : base(loggerFactory, mediasoupOptions)
        {
            _ptr = IntPtr.Zero;// GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Pinned));

            var workerSettings = mediasoupOptions.MediasoupSettings.WorkerSettings;
            var argv = new List<string>
            {
               "" // Ignore `workerPath`
            };
            if (workerSettings.LogLevel.HasValue)
            {
                argv.Add($"--logLevel={workerSettings.LogLevel.Value.GetEnumMemberValue()}");
            }
            if (!workerSettings.LogTags.IsNullOrEmpty())
            {
                workerSettings.LogTags!.ForEach(m => argv.Add($"--logTag={m.GetEnumMemberValue()}"));
            }
            if (workerSettings.RtcMinPort.HasValue)
            {
                argv.Add($"--rtcMinPort={workerSettings.RtcMinPort}");
            }
            if (workerSettings.RtcMaxPort.HasValue)
            {
                argv.Add($"--rtcMaxPort={workerSettings.RtcMaxPort}");
            }
            if (!workerSettings.DtlsCertificateFile.IsNullOrWhiteSpace())
            {
                argv.Add($"--dtlsCertificateFile={workerSettings.DtlsCertificateFile}");
            }
            if (!workerSettings.DtlsPrivateKeyFile.IsNullOrWhiteSpace())
            {
                argv.Add($"--dtlsPrivateKeyFile={workerSettings.DtlsPrivateKeyFile}");
            }
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            argv.Add(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            var version = mediasoupOptions.MediasoupStartupSettings.MediasoupVersion;

            LibMediasoupWorkerNative.MediasoupWorkerRun(argv.Count - 1,
             argv.ToArray(),
             version,
             0,
             0,
             0,
             0,
             _channelRead,
             _ptr,
             _channelWrite,
             _ptr,
             _payloadChannelRead,
             _ptr,
             _payloadchannelWrite,
             _ptr
             );

            /*
            _channel = new Channel(_loggerFactory.CreateLogger<Channel>(), _pipes[3], _pipes[4], ProcessId);
            _channel.MessageEvent += OnChannelMessage;

            _payloadChannel = new PayloadChannel(_loggerFactory.CreateLogger<PayloadChannel>(), _pipes[5], _pipes[6], ProcessId);
            */
        }

        public override Task CloseAsync()
        {
            throw new NotImplementedException();
        }

        #region Event handles

        private void OnChannelMessage(string targetId, string @event, string? data)
        {
            if (@event != "running")
            {
                return;
            }

            _channel.MessageEvent -= OnChannelMessage;
            Emit("@success");
        }

        #endregion
    }
}
