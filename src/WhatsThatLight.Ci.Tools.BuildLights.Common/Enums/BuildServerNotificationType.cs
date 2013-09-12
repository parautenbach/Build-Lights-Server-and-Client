// <copyright file="BuildServerNotificationType.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Enums
{
    /// <summary>
    /// Notification event types.
    /// </summary>
    public enum BuildServerNotificationType
    {
        /// <summary>
        /// Not a notification.
        /// </summary>
        None = 0,

        /// <summary>
        /// The notification's type is not known.
        /// </summary>
        Unknown = 1,

        /// <summary>
        /// The build is running. 
        /// </summary>
        BuildBuilding = 2,

        /// <summary>
        /// The build is still running, but one or more failures have occurred. 
        /// </summary>
        BuildFailing = 3,

        /// <summary>
        /// The build completed, but failed. 
        /// </summary>
        BuildFailed = 4,

        /// <summary>
        /// The build could not be started.
        /// </summary>
        BuildFailedToStart = 5,

        /// <summary>
        /// The build is probably hanging.
        /// </summary>
        BuildHanging = 6,

        /// <summary>
        /// The build completed successfully.
        /// </summary>
        BuildSuccessful = 7,

        /// <summary>
        /// Responsibility has been assigned for a failed build. 
        /// </summary>
        BuildResponsibilityAssigned = 8,

        /// <summary>
        /// Responsibility has been assigned for a failed test. 
        /// </summary>
        TestResponsibilityAssigned = 9
    }
}