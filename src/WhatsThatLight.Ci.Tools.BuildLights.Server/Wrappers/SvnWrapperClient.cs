// <copyright file="SvnWrapperClient.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Server.Wrappers
{
    using System;
    using System.Collections.ObjectModel;
    using System.Net;

    using WhatsThatLight.Ci.Tools.BuildLights.Server.Interfaces;

    using SharpSvn;

    /// <summary>
    /// A Subversion client wrapper.
    /// </summary>
    public sealed class SvnWrapperClient : IVcsClient, IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// The internal Subversion client.
        /// </summary>
        private readonly SvnClient client;

        /// <summary>
        /// The internal Subversion client's credentials.
        /// </summary>
        private readonly SvnCredentials credentials;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SvnWrapperClient"/> class.
        /// </summary>
        /// <param name="credentials">The Subversion server's credentials.</param>
        public SvnWrapperClient(SvnCredentials credentials)
        {
            this.client = new SvnClient();
            this.client.Authentication.DefaultCredentials = new NetworkCredential(credentials.Username, credentials.Password);
            this.credentials = credentials;
        }

        #endregion

        #region Implemented Interfaces

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.client != null)
            {
                this.client.Dispose();
            }
        }

        #endregion

        #region IVcsClient

        /// <summary>
        /// Gets the username from an SVN revision.
        /// </summary>
        /// <param name="revision">The revision.</param>
        /// <returns>The username.</returns>
        public string GetUsername(string revision)
        {
            Uri svnUrl = new Uri(this.credentials.Url);
            long revisionLong = long.Parse(revision);
            SvnLogArgs args = new SvnLogArgs {
                                                     Range = new SvnRevisionRange(revisionLong, revisionLong)
                                             };
            Collection<SvnLogEventArgs> logItems;
            this.client.GetLog(svnUrl, args, out logItems);
            return logItems[0].Author;
        }

        #endregion

        #endregion
    }
}