﻿using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Tubumu.Libuv;

namespace Tubumu.Mediasoup
{
    public class Channel : ChannelBase
    {
        #region Constants

        private const int RecvBufferMaxLen = PayloadMaxLen * 2;

        #endregion Constants

        #region Private Fields

        /// <summary>
        /// Unix Socket instance for sending messages to the worker process.
        /// </summary>
        private readonly UVStream _producerSocket;

        /// <summary>
        /// Unix Socket instance for receiving messages to the worker process.
        /// </summary>
        private readonly UVStream _consumerSocket;

        /// <summary>
        /// Buffer for reading messages from the worker.
        /// </summary>
        private readonly byte[] _recvBuffer;
        private int _recvBufferCount;

        #endregion Private Fields

        public Channel(ILogger<Channel> logger, UVStream producerSocket, UVStream consumerSocket, int processId) : base(logger, processId)
        {
            _producerSocket = producerSocket;
            _consumerSocket = consumerSocket;

            _recvBuffer = new byte[RecvBufferMaxLen];
            _recvBufferCount = 0;

            _consumerSocket.Data += ConsumerSocketOnData;
            _consumerSocket.Closed += ConsumerSocketOnClosed;
            _consumerSocket.Error += ConsumerSocketOnError;
            _producerSocket.Closed += ProducerSocketOnClosed;
            _producerSocket.Error += ProducerSocketOnError;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            // Remove event listeners but leave a fake 'error' hander to avoid
            // propagation.
            _consumerSocket.Data -= ConsumerSocketOnData;
            _consumerSocket.Closed -= ConsumerSocketOnClosed;
            _consumerSocket.Error -= ConsumerSocketOnError;

            _producerSocket.Closed -= ProducerSocketOnClosed;
            _producerSocket.Error -= ProducerSocketOnError;

            // Destroy the socket after a while to allow pending incoming messages.
            // 在 Node.js 实现中，延迟了 200 ms。
            try
            {
                _producerSocket.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CloseAsync() | Worker[{_workerId}] _producerSocket.Close()");
            }

            try
            {
                _consumerSocket.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CloseAsync() | Worker[{_workerId}] _consumerSocket.Close()");
            }
        }

        protected override void SendRequestMessage(RequestMessage requestMessage, Sent sent)
        {
            var messageString = $"{requestMessage.Id}:{requestMessage.Method}:{requestMessage.HandlerId}:{requestMessage.Data ?? "undefined"}";
            var messageBytes = Encoding.UTF8.GetBytes(messageString);
            if (messageBytes.Length > MessageMaxLen)
            {
                throw new Exception("Channel request too big");
            }

            Loop.Default.Sync(() =>
            {
                try
                {
                    var messageBytesLengthBytes = BitConverter.GetBytes(messageBytes.Length);

                    // This may throw if closed or remote side ended.
                    _producerSocket.Write(messageBytesLengthBytes, ex =>
                    {
                        if (ex != null)
                        {
                            _logger.LogError(ex, $"_producerSocket.Write() | Worker[{_workerId}] Error");
                            sent.Reject(ex);
                        }
                    });
                    // This may throw if closed or remote side ended.
                    _producerSocket.Write(messageBytes, ex =>
                    {
                        if (ex != null)
                        {
                            _logger.LogError(ex, $"_producerSocket.Write() | Worker[{_workerId}] Error");
                            sent.Reject(ex);
                        }
                    });

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"_producerSocket.Write() | Worker[{_workerId}] Error");
                    sent.Reject(ex);
                }
            });
        }

        #region Event handles

        private void ConsumerSocketOnData(ArraySegment<byte> data)
        {
            if (data.Count > MessageMaxLen)
            {
                _logger.LogError($"ConsumerSocketOnData() | Worker[{_workerId}] Receiving data too large, ignore it");
                return;
            }

            // 数据回调通过单一线程进入，所以 _recvBuffer 是 Thread-safe 的。
            if (_recvBufferCount + data.Count > RecvBufferMaxLen)
            {
                _logger.LogError($"ConsumerSocketOnData() | Worker[{_workerId}] Receiving buffer is full, discarding all data into it");
                return;
            }

            Array.Copy(data.Array!, data.Offset, _recvBuffer, _recvBufferCount, data.Count);
            _recvBufferCount += data.Count;

            try
            {
                var readCount = 0;
                while (readCount < _recvBufferCount - sizeof(int) - 1)
                {
                    var msgLen = BitConverter.ToInt32(_recvBuffer, readCount);
                    readCount += sizeof(int);
                    if (readCount >= _recvBufferCount)
                    {
                        // Incomplete data.
                        break;
                    }

                    var messageBytes = new byte[msgLen];
                    Array.Copy(_recvBuffer, readCount, messageBytes, 0, msgLen);
                    readCount += msgLen;

                    var message = Encoding.UTF8.GetString(messageBytes, 0, messageBytes.Length);
                    ProcessMessage(message);
                }

                var remainingLength = _recvBufferCount - readCount;
                if (remainingLength == 0)
                {
                    _recvBufferCount = 0;
                }
                else
                {
                    var temp = new byte[remainingLength];
                    Array.Copy(_recvBuffer, readCount, temp, 0, remainingLength);
                    Array.Copy(temp, 0, _recvBuffer, 0, remainingLength);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ConsumerSocketOnData() | Worker[{_workerId}] Invalid data received from the worker process.");
                return;
            }
        }

        private void ConsumerSocketOnClosed()
        {
            _logger.LogDebug($"ConsumerSocketOnClosed() | Worker[{_workerId}] Consumer Channel ended by the worker process");
        }

        private void ConsumerSocketOnError(Exception? exception)
        {
            _logger.LogDebug(exception, $"ConsumerSocketOnError() | Worker[{_workerId}] Consumer Channel error");
        }

        private void ProducerSocketOnClosed()
        {
            _logger.LogDebug($"ProducerSocketOnClosed() | Worker[{_workerId}] Producer Channel ended by the worker process");
        }

        private void ProducerSocketOnError(Exception? exception)
        {
            _logger.LogDebug(exception, $"ProducerSocketOnError() | Worker[{_workerId}] Producer Channel error");
        }

        #endregion Event handles
    }
}
