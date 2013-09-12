// <copyright file="BuildNotification.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Notifications
{
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Exceptions;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Protocol;

    /// <summary>
    /// A build notification. 
    /// </summary>
    public class BuildNotification : IBuildServerNotification
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildNotification"/> class.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        /// <param name="projectId">The project id.</param>
        /// <param name="buildConfigId">The build config id.</param>
        /// <param name="recipients">The recipients.</param>
        public BuildNotification(BuildServerNotificationType notificationType, string projectId, string buildConfigId, string[] recipients)
        {
            if (!Parser.IsBuildNotification(notificationType))
            {
                throw new InvalidBuildServerNotificationException(string.Format("{0} is not valid for a {1}", notificationType, typeof(BuildNotification)));
            }

            this.Type = notificationType;
            this.ProjectId = projectId;
            this.BuildConfigId = buildConfigId;
            this.Recipients = recipients;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the build config id.
        /// </summary>
        public string BuildConfigId { get; private set; }

        /// <summary>
        /// Gets the project id.
        /// </summary>
        public string ProjectId { get; private set; }

        /// <summary>
        /// Gets the recipients.
        /// </summary>
        public string[] Recipients { get; private set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public BuildServerNotificationType Type { get; private set; }

        #endregion
    }
}