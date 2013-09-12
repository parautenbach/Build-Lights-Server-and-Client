// <copyright file="Utils.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    using Cassia;

    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;

    /// <summary>
    /// Common utilities. 
    /// </summary>
    public class Utils
    {
        #region Public Methods

        /// <summary>
        /// Adjusts the gamma level for a blink(1) USB device.
        /// </summary>
        /// <param name="rbgColorComponent">The RBG color component.</param>
        /// <returns>The adjusted gamma level</returns>
        public static ushort AdjustBlink1GammaLevel(ushort rbgColorComponent)
        {
            return Convert.ToUInt16(((1 << (rbgColorComponent / 32)) - 1) + ((1 << (rbgColorComponent / 32)) * ((rbgColorComponent % 32) + 1) + 15) / 32);
        }

        /// <summary>
        /// Creates a build key that can be used by the cache.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <returns>A build key</returns>
        public static string CreateBuildKey(IBuildServerNotification notification)
        {
            return notification.ProjectId + notification.BuildConfigId;
        }

        /// <summary>
        /// Gets the hostname. 
        /// </summary>
        /// <returns>The hostname.</returns>
        public static string GetHostname()
        {
            string host = Config.GetHostname();
            if (!string.IsNullOrEmpty(host))
            {
                return host;
            }

            return Dns.GetHostAddresses(Dns.GetHostName()).First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();
        }

        /// <summary>
        /// Gets the username of the currently logged on user after stripping the domain.
        /// </summary>
        /// <returns>The currently logged on user after stripping the domain.</returns>
        /// <remarks>
        /// This will iterate over all sessions to find the first (non-empty) user that
        /// is either being logged in or already logged in. 
        /// </remarks>
        public static string GetUsername()
        {
            // The detected user can be overwritten
            string username = Config.GetUsername();
            if (!string.IsNullOrEmpty(username))
            {
                return username;
            }

            ITerminalServicesManager manager = new TerminalServicesManager();
            using (ITerminalServer server = manager.GetLocalServer())
            {
                server.Open();
                foreach (ITerminalServicesSession session in server.GetSessions())
                {
                    if (!string.IsNullOrEmpty(session.UserName) && (session.ConnectionState == ConnectionState.Initializing || session.ConnectionState == ConnectionState.Active))
                    {
                        return session.UserName;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Determines whether the specified notification type is for an active build.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        /// <returns>
        ///   <c>true</c> if the specified notification type is for an active build; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsActiveBuild(BuildServerNotificationType notificationType)
        {
            return notificationType == BuildServerNotificationType.BuildBuilding || notificationType == BuildServerNotificationType.BuildHanging || notificationType == BuildServerNotificationType.BuildFailing;
        }

        /// <summary>
        /// Determines whether attention is required for the specified notification type.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        /// <returns>
        ///   <c>true</c> if attention is required for the specified notification type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAttentionRequired(BuildServerNotificationType notificationType)
        {
            return notificationType == BuildServerNotificationType.BuildFailed || notificationType == BuildServerNotificationType.BuildFailedToStart || notificationType == BuildServerNotificationType.BuildFailing || notificationType == BuildServerNotificationType.BuildHanging;
        }

        /// <summary>
        /// Determines whether attention is required for the specified notification type.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        /// <param name="state">The state.</param>
        /// <returns>
        ///   <c>true</c> if attention is required for the specified notification type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAttentionRequired(BuildServerNotificationType notificationType, string state)
        {
            return (notificationType == BuildServerNotificationType.BuildResponsibilityAssigned || notificationType == BuildServerNotificationType.TestResponsibilityAssigned) && (state == BuildServerResponsibilityState.Taken);
        }

        /// <summary>
        /// Determines whether to cancel the need for the user's attention. Basically, only if
        /// the build is successful, we'll cancel the current attention setting or the responsibility
        /// gets assigned to someone else the other <see cref="IsAttentionRequired(BuildServerNotificationType, string)"/>. 
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        /// <returns>
        ///   <c>true</c> if attention must be cancelled; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNoAttentionRequired(BuildServerNotificationType notificationType)
        {
            return notificationType == BuildServerNotificationType.BuildSuccessful;
        }

        /// <summary>
        /// Sends the command over a TCP socket.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        /// <param name="command">The command.</param>
        /// <returns>
        ///   <c>true</c> if the command got sent successfully; otherwise, <c>false</c>.
        /// </returns>
        public static bool SendCommand(string hostname, int port, string command)
        {
            try
            {
                using (TcpClient client = new TcpClient(hostname, port))
                {
                    NetworkStream stream = client.GetStream();
                    byte[] writeBuffer = Encoding.ASCII.GetBytes(command);
                    stream.Write(writeBuffer, 0, writeBuffer.Length);
                    return true;
                }
            }
            catch (SocketException)
            {
                return false;
            }
        }

        #endregion
    }
}