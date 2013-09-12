// <copyright file="ProjectInstaller.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Client
{
    using System;
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.ServiceProcess;

    using log4net;

    /// <summary>
    /// Generated code. 
    /// </summary>
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        #region Constants and Fields

        /// <summary>
        /// Local logger. 
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(typeof(Service));

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectInstaller"/> class.
        /// </summary>
        public ProjectInstaller()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the AfterInstall event of the serviceInstaller control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Configuration.Install.InstallEventArgs"/> instance containing the event data.</param>
        private void ServiceInstallerAfterInstall(object sender, InstallEventArgs e)
        {
            try
            {
                ServiceController serviceController = new ServiceController(this.serviceInstaller.ServiceName);
                serviceController.Refresh();
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                serviceController.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        #endregion
    }
}