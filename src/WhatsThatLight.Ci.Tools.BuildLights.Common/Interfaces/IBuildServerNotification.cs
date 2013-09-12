// <copyright file="IBuildServerNotification.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces
{
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;

    /// <summary>
    /// Generic notification interface. 
    /// </summary>
    public interface IBuildServerNotification
    {
        #region Properties

        /// <summary>
        /// Gets the build config id.
        /// </summary>
        string BuildConfigId { get; }

        /// <summary>
        /// Gets the project id.
        /// </summary>
        string ProjectId { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        BuildServerNotificationType Type { get; }

        #endregion
    }
}