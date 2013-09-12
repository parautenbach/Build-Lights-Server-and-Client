// <copyright file="ResponsibilityNotification.cs" company="What's That Light?">
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
    /// A responsibility notification. 
    /// </summary>
    public class ResponsibilityNotification : IBuildServerNotification
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponsibilityNotification"/> class.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        /// <param name="projectId">The project id.</param>
        /// <param name="buildConfigId">The build config id.</param>
        /// <param name="username">The username.</param>
        /// <param name="state">The state.</param>
        public ResponsibilityNotification(BuildServerNotificationType notificationType, string projectId, string buildConfigId, string username, string state)
        {
            if (!Parser.IsResponsibilityNotification(notificationType))
            {
                throw new InvalidBuildServerNotificationException(string.Format("{0} is not valid for a {1}", notificationType, typeof(ResponsibilityNotification)));
            }

            this.Type = notificationType;
            this.ProjectId = projectId;
            this.BuildConfigId = buildConfigId;
            this.Recipient = username;
            this.State = state;
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
        /// Gets the recipient.
        /// </summary>
        public string Recipient { get; private set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        public string State { get; private set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public BuildServerNotificationType Type { get; private set; }

        #endregion
    }
}