﻿using System;
using System.Collections.Generic;

namespace Tubumu.Mediasoup
{
    public class MediasoupOptions
    {
        public MediasoupStartupSettings MediasoupStartupSettings { get; set; }

        public MediasoupSettings MediasoupSettings { get; set; }

        public static MediasoupOptions Default { get; } = new MediasoupOptions
        {
            MediasoupStartupSettings = new MediasoupStartupSettings
            {
                WorkerPath = "mediasoup-worker",
                MediasoupVersion = "0.0.1",
                NumberOfWorkers = Environment.ProcessorCount,
            },
            MediasoupSettings = new MediasoupSettings
            {
                WorkerSettings = new WorkerSettings
                {
                    LogLevel = WorkerLogLevel.Warn,
                    LogTags = new WorkerLogTag[] {
                        WorkerLogTag.Info,
                        WorkerLogTag.Ice,
                        WorkerLogTag.Dtls,
                        WorkerLogTag.Rtp,
                        WorkerLogTag.Srtp,
                        WorkerLogTag.Rtcp,
                        WorkerLogTag.Rtx,
                        WorkerLogTag.Bwe,
                        WorkerLogTag.Score,
                        WorkerLogTag.Simulcast,
                        WorkerLogTag.Svc,
                        WorkerLogTag.Sctp,
                        WorkerLogTag.Message,
                    },
                    RtcMinPort = 10000,
                    RtcMaxPort = 59999,
                },
                RouterSettings = new RouterSettings
                {
                    RtpCodecCapabilities = new[]
                    {
                        new RtpCodecCapability
                        {
                            Kind      = MediaKind.Audio,
                            MimeType  = "audio/opus",
                            ClockRate = 48000,
                            Channels  = 2
                        },
                        new RtpCodecCapability {
                            Kind       = MediaKind.Video,
                            MimeType   = "video/VP8",
                            ClockRate  = 90000,
                            Parameters = new Dictionary<string, object>
                            {
                                { "x-google-start-bitrate" , 1000 }
                            }
                        },
                        new RtpCodecCapability {
                            Kind       = MediaKind.Video,
                            MimeType   = "video/VP9",
                            ClockRate  = 90000,
                            Parameters = new Dictionary<string, object>
                            {
                                { "profile-id"             , 2 },
                                { "x-google-start-bitrate" , 1000 }
                            }
                        },
                        new RtpCodecCapability {
                            Kind       = MediaKind.Video,
                            MimeType   = "video/h264",
                            ClockRate  = 90000,
                            Parameters = new Dictionary<string, object>
                            {
                                { "packetization-mode"      , 1 },
                                { "profile-level-id"        , "4d0032" },
                                { "level-asymmetry-allowed" , 1 },
                                { "x-google-start-bitrate"  , 1000 }
                            }
                        },
                        new RtpCodecCapability {
                            Kind       = MediaKind.Video,
                            MimeType   = "video/h264",
                            ClockRate  = 90000,
                            Parameters = new Dictionary<string, object>
                            {
                                { "packetization-mode"      , 1 },
                                { "profile-level-id"        , "42e01f" },
                                { "level-asymmetry-allowed" , 1 },
                                { "x-google-start-bitrate"  , 1000 }
                            }
                        }
                    },
                },
                WebRtcServerSettings = new WebRtcServerSettings
                {
                    ListenInfos = new[]
                    {
                        new WebRtcServerListenInfo
                        {
                            Protocol = TransportProtocol.UDP,
                            Ip = "0.0.0.0",
                            AnnouncedIp = null,
                            Port = 44444,
                        },
                        new WebRtcServerListenInfo
                        {
                            Protocol = TransportProtocol.TCP,
                            Ip = "0.0.0.0",
                            AnnouncedIp = null,
                            Port = 44444,
                        }
                    }
                },
                WebRtcTransportSettings = new WebRtcTransportSettings
                {
                    ListenIps = new[]
                    {
                        new TransportListenIp { Ip = "0.0.0.0",  AnnouncedIp = null }
                    },
                    InitialAvailableOutgoingBitrate = 1_000_000,
                    MinimumAvailableOutgoingBitrate = 600_000,
                    MaxSctpMessageSize = 256 * 1024,
                    MaximumIncomingBitrate = 1_500_000,
                },
                PlainTransportSettings = new PlainTransportSettings
                {
                    ListenIp = new TransportListenIp { Ip = "0.0.0.0", AnnouncedIp = null },
                    MaxSctpMessageSize = 256 * 1024,
                }
            }
        };
    }
}
