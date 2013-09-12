// <copyright file="TeamCityWrapperClient.cs" company="What's That Light?">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Notifications;
    using WhatsThatLight.Ci.Tools.BuildLights.Server.Interfaces;

    using Sharp2City;

    using log4net;

    /// <summary>
    /// TeamCity build server client. 
    /// </summary>
    public class TeamCityWrapperClient : IBuildServerClient
    {
        #region Constants and Fields

        /// <summary>
        /// Local logger. 
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(typeof(TeamCityWrapperClient));

        /// <summary>
        /// Wrapper class for a TeamCity client library. 
        /// </summary>
        private readonly TeamCityClient buildServerClient;

        /// <summary>
        /// Vcs client to get the username on a revision. 
        /// </summary>
        private readonly IVcsClient vcsClient;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamCityWrapperClient"/> class.
        /// </summary>
        /// <param name="credentials">The build server's credentials.</param>
        /// <param name="vcsClient">The VCS client. The v6.5 and v7.0 versions of TeamCity's REST API doesn't provide the username for the revision that triggered a build, and the request to get changes for a build is always empty. </param>
        public TeamCityWrapperClient(TeamCityCredentials credentials, IVcsClient vcsClient)
        {
            this.buildServerClient = new TeamCityClient(credentials.Url, credentials.Username, credentials.Password);
            this.vcsClient = vcsClient;
        }

        #endregion

        #region Implemented Interfaces

        #region IBuildServerClient

        /// <summary>
        /// Gets all active builds.
        /// </summary>
        /// <returns>
        /// Collection of <see cref="IBuildServerNotification"/>
        /// </returns>
        public IEnumerable<IBuildServerNotification> GetAllActiveBuildNotifications()
        {
            Log.Info("Getting all active builds");
            return (from build in this.buildServerClient.GetAllRunningBuilds()
                    where !build.Personal
                    select new BuildNotification(BuildServerNotificationType.BuildBuilding, build.ProjectId, build.BuildTypeId, this.GetContributors(build.Revisions))).AsParallel().Cast<IBuildServerNotification>().ToList();
        }

        /// <summary>
        /// Gets all builds that require attention.
        /// </summary>
        /// <returns>
        /// Collection of <see cref="IBuildServerNotification"/>
        /// </returns>
        public IEnumerable<IBuildServerNotification> GetAllBuildsThatRequireAttention()
        {
            Log.Info("Getting all builds that require attention");
            ConcurrentQueue<IBuildServerNotification> notifications = new ConcurrentQueue<IBuildServerNotification>();

            // Archived projects aren't important
            TeamCityProject[] projects = this.buildServerClient.GetAllProjects().Where(x => !x.Archived).ToArray();
            for (int i = 0; i < projects.Count(); i++)
            {
                TeamCityProject project = projects[i];
                Log.Info(string.Format("Progress: {0} / {1} - Project: {2} ({3})", i + 1, projects.Count(), project.Name, project.Id));
                foreach (TeamCityBuildConfiguration buildConfiguration in this.buildServerClient.GetBuildConfigurations(project))
                {
                    Log.Debug(string.Format("Build configuration {0} ({1})", buildConfiguration.Name, buildConfiguration.Id));

                    TeamCityBuild lastSuccessfulBuild = this.buildServerClient.GetLastSuccessfulBuild(buildConfiguration.Id);
                    TeamCityBuild lastBuild = this.buildServerClient.GetLastBuild(buildConfiguration);

                    // If there has been no builds, no successful builds or the last build has been successful, skip
                    if (lastBuild == null || lastSuccessfulBuild == null || lastBuild.BuildId == lastSuccessfulBuild.BuildId)
                    {
                        continue;
                    }

                    // Personal builds don't affect anyone else, because the code won't have been committed to a VCS
                    foreach (TeamCityBuild build in this.buildServerClient.GetBuildsSinceBuildNumber(buildConfiguration.Id, lastSuccessfulBuild.BuildNumber).Where(x => !x.Personal))
                    {
                        Log.Debug(string.Format("Build {0} ({1})", build.BuildNumber, build.BuildId));

                        // All we know is that these builds failed. We don't know whether anyone is set responsible,
                        // so the best we can do is to set only a build failed notification.
                        // TODO: TeamCity 7.0 supports an investigation element with the people responsible. 
                        notifications.Enqueue(new BuildNotification(BuildServerNotificationType.BuildFailed, build.ProjectId, build.BuildTypeId, this.GetContributors(build.Revisions)));
                    }
                }
            }

            return notifications;
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Gets the contributors (committers) for a revision.
        /// </summary>
        /// <param name="revisions">The revisions.</param>
        /// <returns>An array of contributors.</returns>
        private string[] GetContributors(IEnumerable<string> revisions)
        {
            return revisions.Select(revision => this.vcsClient.GetUsername(revision)).ToArray();
        }

        #endregion
    }
}