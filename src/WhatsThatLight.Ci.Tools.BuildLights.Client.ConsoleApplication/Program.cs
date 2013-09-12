// <copyright file="Program.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Client.ConsoleApplication
{
    using System;
    using System.Threading;

    using WhatsThatLight.Ci.Tools.BuildLights.Common;

    /// <summary>
    /// The main console application.
    /// </summary>
    internal class Program
    {
        #region Methods

        /// <summary>
        /// Main application entry point.
        /// </summary>
        private static void Main()
        {
            LightsManager lightsManager = null;
            try
            {
                LightsDeviceController lightsDeviceController = new LightsDeviceController(Config.GetUsbProductId(), Config.GetUsbVendorId(), Config.GetUsbUsage(), Config.GetUsbUsagePage(), Config.GetWaitForDeviceRetryPeriod(), Config.GetUsbControlTransferType());
                lightsManager = new LightsManager(lightsDeviceController, Config.GetLightsManagerPort(), Config.GetNotificationManagerHost(), Config.GetNotificationManagerPort(), Config.GetRegistrationRetryPeriod(), Config.GetUsbProtocolType());
                Thread thread = new Thread(lightsManager.Start);
                thread.Start();
                Console.WriteLine("Press any key to terminate...");
                Console.ReadKey(true);
                lightsManager.Stop();
                thread.Join();
                Console.WriteLine("Press any key to close...");
                Console.ReadKey(true);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Console.ReadKey(true);
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }
        }

        #endregion
    }
}