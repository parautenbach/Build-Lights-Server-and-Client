// <copyright file="Listener.cs" company="What's That Light?">
// Copyright 2013 Pieter Rautenbach
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Protocol;

    using log4net;

    /// <summary>
    /// The single-threaded socket server. 
    /// </summary>
    public sealed class Listener
    {
        #region Constants and Fields

        /// <summary>
        /// The log4net instance to use for logging. 
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(typeof(Listener));

        /// <summary>
        /// The IP address to listen on.
        /// </summary>
        private readonly IPAddress address;

        /// <summary>
        /// The server's listener. 
        /// </summary>
        private readonly TcpListener listener;

        /// <summary>
        /// The port to listen on.
        /// </summary>
        private readonly int port;

        /// <summary>
        /// A lock to ensure that the server starts and stops correctly. 
        /// </summary>
        private readonly object runLock = new object();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Listener"/> class.
        /// </summary>
        /// <param name="address">The IP address to bind to.</param>
        /// <param name="port">The port to listen on. This honours <see cref="TcpListener"/>'s usage of 0, which will cause any available port between 1024 and 5,000 to be used.</param>
        public Listener(IPAddress address, int port)
        {
            this.Running = false;
            this.address = address;
            this.port = port;
            this.listener = new TcpListener(this.address, port);
            log.Info(string.Format("Server will listen to IP address {0} on port {1}", this.address, this.port));
        }

        #endregion

        #region Delegates

        /// <summary>
        /// A delegate used when a command has been received. 
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public delegate void CommandReceivedEventHandler(object sender, EventArgs e);

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a command has been received.
        /// </summary>
        public event CommandReceivedEventHandler OnCommandReceived;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this <see cref="Listener"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        public bool Running { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Handles the command.
        /// </summary>
        /// <param name="commandString">The command.</param>
        public void HandleCommand(string commandString)
        {
            Dictionary<string, string> commandDictionary = Parser.DecodeCommand(commandString);
            if (Parser.IsBuildServerNotification(commandDictionary))
            {
                IBuildServerNotification notification = Parser.DecodeBuildServerNotification(commandDictionary);
                log.Info(string.Format("Notification {0} event received", notification.Type));
                this.NotifyCommandReceived(notification);
            }
            else if (Parser.IsRequest(commandDictionary))
            {
                IRequest request = Parser.DecodeRequest(commandDictionary);
                log.Info(string.Format("Request {0} event received", request.Type));
                this.NotifyCommandReceived(request);
            }
            else
            {
                log.Warn(string.Format("Could not handle command: {0}", commandString));
            }
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            lock (this.runLock)
            {
                log.Info("Starting listener");
                if (this.Running)
                {
                    log.Error("Attempted to start an already running listener");
                    return;
                }

                this.Running = true;
                this.listener.Start();
                log.Info("Listener is running");
                this.listener.BeginAcceptTcpClient(this.HandleTcpClientAccept, null);
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            lock (this.runLock)
            {
                if (!this.Running)
                {
                    log.Error("Attempted to stop an already stopped listener");
                    return;
                }

                log.Info("Listener is stopping");
                try
                {
                    // Set the running state before stopping the listener
                    // to avoid an infinite loop in Run
                    this.Running = false;
                    if (this.listener != null)
                    {
                        this.listener.Stop();
                    }
                }
                catch (Exception e)
                {
                    log.Error(e);
                }

                log.Info("Listener stopped");
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <param name="tcpClient">The client.</param>
        internal void Process(TcpClient tcpClient)
        {
            try
            {
                // We opt to have states for starting and stopping, but it this stage it
                // seems redundant, so we'll stick to running and not running. 
                if (!this.Running)
                {
                    return;
                }

                using (NetworkStream stream = tcpClient.GetStream())
                {
                    // Byte buffer for data received
                    int dataByte;

                    // Start fresh for a new command
                    StringBuilder dataBuilder = new StringBuilder();
                    do
                    {
                        dataByte = stream.ReadByte();
                        dataBuilder.Append((char)dataByte);
                        if (Parser.IsCommandTerminatedProperly(dataBuilder.ToString()))
                        {
                            log.Info(string.Format("Command received: {0}", dataBuilder));
                            try
                            {
                                this.HandleCommand(dataBuilder.ToString());
                            }
                            catch (Exception e)
                            {
                                // Keep the listener running, so don't rethrow
                                log.Error(e);
                            }

                            // Multiple commands may be sent over a connected socket
                            dataBuilder = new StringBuilder();
                        }
                    }
                    while (this.Running && dataByte > -1 && dataBuilder.Length < Packet.MaximumPacketSize);
                    if (dataBuilder.Length >= Packet.MaximumPacketSize)
                    {
                        log.Warn("Maximum packet size exceeded - connection will now terminate");
                    }
                }
            }
            catch (SocketException se)
            {
                // This should only happen on Stop, because we're interrupting the accept blocking call
                if (se.ErrorCode != 10004)
                {
                    log.Error(se);
                }
            }
            catch (ThreadAbortException)
            {
                // Expected on shutdown
                Thread.ResetAbort();
            }
            catch (Exception e)
            {
                // Anything as unexpected as this must crash the server
                log.Error(e);
                throw;
            }
            finally
            {
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    log.Debug("Connection closed");
                }
            }
        }

        /// <summary>
        /// Handles the TCP client accept.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        private void HandleTcpClientAccept(IAsyncResult asyncResult)
        {
            try
            {
                // We want to be ready to accept the next client so that processing the current one doesn't block that
                TcpClient tcpClient = this.listener.EndAcceptTcpClient(asyncResult);
                log.Debug(string.Format("New connection from {0} accepted", tcpClient.Client.RemoteEndPoint));
                this.listener.BeginAcceptTcpClient(this.HandleTcpClientAccept, null);
                this.Process(tcpClient);
            }
            catch (ObjectDisposedException)
            {
                if (!this.Running)
                {
                    // This could happen during shutdown
                    return;
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }
        }

        /// <summary>
        /// Notifies all subscribers about the command received.
        /// </summary>
        /// <param name="command">The command.</param>
        private void NotifyCommandReceived(object command)
        {
            if (this.OnCommandReceived != null)
            {
                this.OnCommandReceived(command, EventArgs.Empty);
            }
        }

        #endregion
    }
}