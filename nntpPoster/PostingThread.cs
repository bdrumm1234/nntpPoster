﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using PostingNntpClient;
using Util.Configuration;

namespace nntpPoster
{
    class PostingThread : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Object monitor = new Object();
        private Boolean StopRequested;
        private Boolean Finished;
        private Task MyTask;

        private SimpleNntpPostingClient _client;
        private Boolean _continuePosting;

        private Settings _configuration;
        private WatchFolderSettings _folderConfiguration;
        private NewsHostConnectionInfo _connectionInfo;        
        private Queue<nntpMessage> _messageQueue;

        public event EventHandler<nntpMessage> MessagePosted;
        protected virtual void OnMessagePosted(nntpMessage e)
        {
            if (MessagePosted != null) MessagePosted(this, e);
        }        

        public PostingThread(Settings configuration, WatchFolderSettings folderConfiguration, NewsHostConnectionInfo connectionInfo, 
            Queue<nntpMessage> messageQueue)
        {
            _configuration = configuration;
            _folderConfiguration = folderConfiguration;
            _connectionInfo = connectionInfo;
            _messageQueue = messageQueue;
            MyTask = new Task(PostingTask, TaskCreationOptions.LongRunning);
        }

        public void Start()
        {
            StopRequested = false;
            MyTask.Start();
        }

        public void Stop()
        {
            lock (monitor)
            {
                StopRequested = true;
                Monitor.Pulse(monitor);
            }
            MyTask.Wait();
        }

        public Task RequestStop()
        {
            lock (monitor)
            {
                StopRequested = true;
                Monitor.Pulse(monitor);
            }
            return MyTask;
        }

        private void PostingTask()
        {
            try
            {
                while (!Finished)
                {
                    var message = GetNextMessageToPost();
                    if (message != null)
                    {
                        PostMessage(message);
                    }
                    else
                    {
                        if (_client != null)         //If the queue runs dry we close the connection
                        {
                            log.Debug("Disposing client because of empty queue.");
                            _client.Dispose();
                            _client = null;
                        }
                        if (StopRequested)
                        {
                            Finished = true;
                        }
                        else
                        {
                            lock (monitor)
                            {
                                if (Finished)
                                {
                                    break;
                                }
                                if (StopRequested)
                                {
                                    Finished = true;
                                    break;
                                }
                                Monitor.Wait(monitor, 100);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in the posting thread.", ex);
            }
        }

        private void PostMessage(nntpMessage message)
        {
            var retryCount = 0;
            var retry = true;
            while (retry && retryCount < _configuration.MaxRetryCount)
            {
                try
                {
                    if (_client == null)
                    {
                        log.Debug("Constructing new client.");
                        _client = new SimpleNntpPostingClient(_connectionInfo);
                        _client.Connect();
                    }
                    var partMessageId = _client.PostYEncMessage(
                        _folderConfiguration.FromAddress,
                        message.Subject,
                        message.PostInfo.PostedGroups,
                        message.PostInfo.PostedDateTime,
                        message.Prefix,
                        message.YEncFilePart.EncodedLines,
                        message.Suffix);
                    lock (message.PostInfo.Segments)
                    {
                        message.PostInfo.Segments.Add(new PostedFileSegment
                        {
                            MessageId = partMessageId,
                            Bytes = message.YEncFilePart.Size,
                            SegmentNumber = message.YEncFilePart.Number
                        });
                    }
                    retry = false;
                    OnMessagePosted(message);
                }
                catch (Exception ex)
                {
                    if (_client != null)         //If we get an Exception we close the connection
                    {
                        log.Debug("Disposing client because of exception.");
                        _client.Dispose();
                        _client = null;
                    }
                    log.Warn("Posting yEnc message failed", ex);

                    if (retryCount++ < _configuration.MaxRetryCount)
                        log.InfoFormat("Retrying to post message, attempt {0}", retryCount);
                    else
                        log.Error("Maximum retry attempts reached. Posting is probably corrupt.");
                }
            }
        }

        private nntpMessage GetNextMessageToPost()
        {
            lock (_messageQueue)
            {
                if (_messageQueue.Count > 0)
                    return _messageQueue.Dequeue();
            }
            if (!StopRequested) //If stop is requested it is logical the queue gets empty.
                log.Warn("Posting thread is starved, reduce threads to make more optimal use of resources.");
            return null;
        }

        public void Dispose()
        {
            if (_client != null)
            {
                log.Debug("Disposing client because of dispose request.");
                _client.Dispose();
            }
        }
    }
}
