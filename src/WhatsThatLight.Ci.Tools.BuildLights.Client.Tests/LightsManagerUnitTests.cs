// <copyright file="LightsManagerUnitTests.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Client.Tests
{
    using System;
    using System.Text;

    using HidLibrary;

    using WhatsThatLight.Ci.Tools.BuildLights.Client.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Client.Exceptions;
    using WhatsThatLight.Ci.Tools.BuildLights.Client.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Requests;

    using NMock;

    using NUnit.Framework;

    ////using NMock;

    /// <summary>
    /// Test the client manager.
    /// </summary>
    public sealed class LightsManagerUnitTests : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// Mock factory. 
        /// </summary>
        private MockFactory mockFactory;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets up the tests.
        /// </summary>
        [TestFixtureSetUp]
        public void SetUp()
        {
            this.mockFactory = new MockFactory();
        }

        /// <summary>
        /// Tests the blink1 notify the USB device for all events that it should.
        /// </summary>
        [Test]
        public void TestBlink1NotifyUsbDeviceTestAllRequests()
        {
            LightsManager lightsManager = null;
            try
            {
                // Mocks
                Mock<LightsDeviceController> lightsDeviceController = this.mockFactory.CreateMock<LightsDeviceController>();

                // Expectations
                lightsDeviceController.Expects.AtLeastOne.Method(x => x.GetFeatureReportByteLength()).WillReturn(10);
                lightsDeviceController.Expects.AtLeastOne.Method(x => x.SendCommand(new byte[10])).WithAnyArguments().WillReturn(LightsDeviceResult.Ack);

                // Run and test
                lightsManager = new LightsManager(lightsDeviceController.MockObject, UsbProtocolType.Blink1);

                // Build activity
                LightsDeviceResult buildActiveRequestResult = lightsManager.NotifyLightsDevice(new BuildActiveRequest(true));
                Assert.That(buildActiveRequestResult, NUnit.Framework.Is.EqualTo(LightsDeviceResult.Ack));
                buildActiveRequestResult = lightsManager.NotifyLightsDevice(new BuildActiveRequest(false));
                Assert.That(buildActiveRequestResult, NUnit.Framework.Is.EqualTo(LightsDeviceResult.Ack));

                // Attention
                LightsDeviceResult attentionRequestResult = lightsManager.NotifyLightsDevice(new AttentionRequest(true));
                Assert.That(attentionRequestResult, NUnit.Framework.Is.EqualTo(LightsDeviceResult.Ack));
                attentionRequestResult = lightsManager.NotifyLightsDevice(new AttentionRequest(false));
                Assert.That(attentionRequestResult, NUnit.Framework.Is.EqualTo(LightsDeviceResult.Ack));
                attentionRequestResult = lightsManager.NotifyLightsDevice(new AttentionRequest(true, true));
                Assert.That(attentionRequestResult, NUnit.Framework.Is.EqualTo(LightsDeviceResult.Ack));

                // System status
                LightsDeviceResult statusRequestRequestResult = lightsManager.NotifyLightsDevice(new StatusRequest(true));
                Assert.That(statusRequestRequestResult, NUnit.Framework.Is.EqualTo(LightsDeviceResult.Ack));
                statusRequestRequestResult = lightsManager.NotifyLightsDevice(new StatusRequest(false));
                Assert.That(statusRequestRequestResult, NUnit.Framework.Is.EqualTo(LightsDeviceResult.Ack));
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the blink1 throws an exception for an unsupported USB protocol type.
        /// </summary>
        [Test]
        public void TestBlink1ThrowsExceptionForUnsupportedUsbProtocolType()
        {
            // Mocks
            Mock<LightsDeviceController> lightsDeviceController = this.mockFactory.CreateMock<LightsDeviceController>();

            // Expectations
            lightsDeviceController.Expects.AtLeastOne.Method(x => x.GetFeatureReportByteLength()).WillReturn(10);
            lightsDeviceController.Expects.AtLeastOne.Method(x => x.SendCommand(new byte[10])).WithAnyArguments().WillReturn(LightsDeviceResult.Ack);

            // Run and test
            LightsManager lightsManager = new LightsManager(lightsDeviceController.MockObject, UsbProtocolType.None);
            Assert.Throws<UnsupportedUsbProtocolTypeException>(() => lightsManager.NotifyLightsDevice(new BuildActiveRequest(true)));
        }

        /// <summary>
        /// Tests the handle command for all lights device results.
        /// </summary>
        /// <param name="result">The result.</param>
        [Test]
        [Sequential]
        public void TestHandleCommandForAllLightsDeviceResults([Values(LightsDeviceResult.Ack, LightsDeviceResult.Nak, LightsDeviceResult.NoResponse, LightsDeviceResult.NotConnected, LightsDeviceResult.NotOpen)] LightsDeviceResult result)
        {
            this.mockFactory.ClearExpectations();

            // Mocks
            Mock<ILightsDeviceController> mockLightsDeviceController = this.mockFactory.CreateMock<ILightsDeviceController>();
            mockLightsDeviceController.Expects.One.Method(x => x.SendCommand(null)).WithAnyArguments().WillReturn(result);

            // Any request that steps into the send command logic is sufficient
            AttentionRequest request = new AttentionRequest(true);

            // Run
            LightsManager lightsManager = null;
            try
            {
                lightsManager = new LightsManager(mockLightsDeviceController.MockObject, UsbProtocolType.DasBlinkenlichten);
                lightsManager.HandleCommand(request, EventArgs.Empty);
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }

            // Ensure a clean end to the test
            this.mockFactory.VerifyAllExpectationsHaveBeenMet();
        }

        /// <summary>
        /// Tests the handle command for server down request.
        /// </summary>
        [Test]
        public void TestHandleCommandForServerDownRequest()
        {
            // Mocks
            Mock<ILightsDeviceController> mockLightsDeviceController = this.mockFactory.CreateMock<ILightsDeviceController>();
            mockLightsDeviceController.Expects.One.Method(x => x.Start());
            mockLightsDeviceController.Expects.One.Method(x => x.Stop());
            mockLightsDeviceController.IgnoreUnexpectedInvocations = true;

            // One for HandleCommand; one for Stop
            mockLightsDeviceController.Expects.Exactly(2).Method(x => x.SendCommand(null)).WithAnyArguments().WillReturn(LightsDeviceResult.Ack);

            // Run
            StatusRequest request = new StatusRequest(false);
            LightsManager lightsManager = null;
            try
            {
                lightsManager = new LightsManager(mockLightsDeviceController.MockObject, UsbProtocolType.DasBlinkenlichten);
                lightsManager.Start();
                Assert.That(lightsManager.Running, NUnit.Framework.Is.True);
                lightsManager.HandleCommand(request, EventArgs.Empty);
                lightsManager.Stop();
                Assert.That(lightsManager.Running, NUnit.Framework.Is.False);
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }

            this.mockFactory.VerifyAllExpectationsHaveBeenMet();
        }

        /// <summary>
        /// Tests that the USB device responds witn not connected.
        /// </summary>
        [Test]
        public void TestNotifyUsbDeviceIsNullRespondsNotConnected()
        {
            // Run
            LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0, 1000, UsbControlTransferType.Raw) {
                                                                                                                                             Device = null
                                                                                                                                     };
            LightsManager lightsManager = null;
            try
            {
                lightsManager = new LightsManager(lightsDeviceController, UsbProtocolType.DasBlinkenlichten);
                BuildActiveRequest buildActiveRequest = new BuildActiveRequest(true);

                // Test
                Assert.That(lightsManager.NotifyLightsDevice(buildActiveRequest), NUnit.Framework.Is.EqualTo(LightsDeviceResult.NotConnected));
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the USB device responds OK when connected and opened.
        /// </summary>
        [Test]
        public void TestNotifyUsbDeviceNoResponse()
        {
            // Mocks
            Mock<IHidDevice> mockDevice = this.mockFactory.CreateMock<IHidDevice>();
            byte[] mockDataBytes = Encoding.ASCII.GetBytes(UsbResponse.Ack);
            HidDeviceData mockData = new HidDeviceData(mockDataBytes, HidDeviceData.ReadStatus.Success);
            HidReport mockReport = new HidReport(1, mockData);

            // Expectations
            mockDevice.Expects.AtLeastOne.GetProperty(x => x.IsConnected).WillReturn(true);
            using (this.mockFactory.Ordered)
            {
                mockDevice.Expects.One.GetProperty(x => x.IsOpen).WillReturn(false);
                mockDevice.Expects.AtLeastOne.GetProperty(x => x.IsOpen).WillReturn(true);
            }

            mockDevice.Expects.AtLeastOne.Method(x => x.OpenDevice());
            mockDevice.Expects.AtLeastOne.Method(x => x.CreateReport()).WillReturn(mockReport);
            mockDevice.Expects.AtLeastOne.Method(x => x.WriteReport(null)).WithAnyArguments().WillReturn(false);

            // Run
            LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0, 1000, UsbControlTransferType.Raw) {
                                                                                                                                             Device = mockDevice.MockObject
                                                                                                                                     };
            LightsManager lightsManager = null;
            try
            {
                lightsManager = new LightsManager(lightsDeviceController, UsbProtocolType.DasBlinkenlichten);
                BuildActiveRequest buildActiveRequest = new BuildActiveRequest(true);

                // Test
                Assert.That(lightsManager.NotifyLightsDevice(buildActiveRequest), NUnit.Framework.Is.EqualTo(LightsDeviceResult.NoResponse));
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }

            this.mockFactory.VerifyAllExpectationsHaveBeenMet();
        }

        /// <summary>
        /// Tests that the USB device responds OK when connected and opened.
        /// </summary>
        [Test]
        public void TestNotifyUsbDeviceRespondsAck()
        {
            LightsManager lightsManager = null;
            try
            {
                // Mocks
                Mock<IHidDevice> mockDevice = this.mockFactory.CreateMock<IHidDevice>();
                byte[] mockDataBytes = Encoding.ASCII.GetBytes(UsbResponse.Ack);
                HidDeviceData mockData = new HidDeviceData(mockDataBytes, HidDeviceData.ReadStatus.Success);
                HidReport mockReport = new HidReport(1, mockData);

                // Expectations
                mockDevice.Expects.AtLeastOne.GetProperty(x => x.IsConnected).WillReturn(true);
                using (this.mockFactory.Ordered)
                {
                    mockDevice.Expects.One.GetProperty(x => x.IsOpen).WillReturn(false);
                    mockDevice.Expects.AtLeastOne.GetProperty(x => x.IsOpen).WillReturn(true);
                }

                mockDevice.Expects.AtLeastOne.Method(x => x.OpenDevice());
                mockDevice.Expects.AtLeastOne.Method(x => x.CreateReport()).WillReturn(mockReport);
                mockDevice.Expects.AtLeastOne.Method(x => x.WriteReport(null)).WithAnyArguments().WillReturn(true);
                mockDevice.Expects.AtLeastOne.Method(x => x.Read()).WillReturn(mockData);

                // Run
                LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0, 1000, UsbControlTransferType.Raw) {
                                                                                                                                                 Device = mockDevice.MockObject
                                                                                                                                         };
                lightsManager = new LightsManager(lightsDeviceController, UsbProtocolType.DasBlinkenlichten);
                BuildActiveRequest buildActiveRequest = new BuildActiveRequest(true);

                // Test
                Assert.That(lightsManager.NotifyLightsDevice(buildActiveRequest), NUnit.Framework.Is.EqualTo(LightsDeviceResult.Ack));
                this.mockFactory.VerifyAllExpectationsHaveBeenMet();
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the USB device responds OK when connected and opened.
        /// </summary>
        [Test]
        public void TestNotifyUsbDeviceRespondsNak()
        {
            LightsManager lightsManager = null;
            try
            {
                // Mocks
                Mock<IHidDevice> mockDevice = this.mockFactory.CreateMock<IHidDevice>();
                byte[] mockDataBytes = Encoding.ASCII.GetBytes(UsbResponse.Nak);
                HidDeviceData mockData = new HidDeviceData(mockDataBytes, HidDeviceData.ReadStatus.Success);
                HidReport mockReport = new HidReport(1, mockData);

                // Expectations
                mockDevice.Expects.AtLeastOne.GetProperty(x => x.IsConnected).WillReturn(true);
                using (this.mockFactory.Ordered)
                {
                    mockDevice.Expects.One.GetProperty(x => x.IsOpen).WillReturn(false);
                    mockDevice.Expects.AtLeastOne.GetProperty(x => x.IsOpen).WillReturn(true);
                }

                mockDevice.Expects.AtLeastOne.Method(x => x.OpenDevice());
                mockDevice.Expects.AtLeastOne.Method(x => x.CreateReport()).WillReturn(mockReport);
                mockDevice.Expects.AtLeastOne.Method(x => x.WriteReport(null)).WithAnyArguments().WillReturn(true);
                mockDevice.Expects.AtLeastOne.Method(x => x.Read()).WillReturn(mockData);

                // Run
                LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0, 1000, UsbControlTransferType.Raw) {
                                                                                                                                                 Device = mockDevice.MockObject
                                                                                                                                         };
                lightsManager = new LightsManager(lightsDeviceController, UsbProtocolType.DasBlinkenlichten);
                BuildActiveRequest buildActiveRequest = new BuildActiveRequest(true);

                // Test
                Assert.That(lightsManager.NotifyLightsDevice(buildActiveRequest), NUnit.Framework.Is.EqualTo(LightsDeviceResult.Nak));
                this.mockFactory.VerifyAllExpectationsHaveBeenMet();
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the USB device responds with not open.
        /// </summary>
        [Test]
        public void TestNotifyUsbDeviceRespondsNotOpen()
        {
            LightsManager lightsManager = null;
            try
            {
                // Mocks
                Mock<IHidDevice> mockDevice = this.mockFactory.CreateMock<IHidDevice>();

                // Expectations
                mockDevice.Expects.AtLeastOne.GetProperty(x => x.IsConnected).WillReturn(true);
                mockDevice.Expects.AtLeastOne.GetProperty(x => x.IsOpen).WillReturn(false);
                mockDevice.Expects.AtLeastOne.Method(x => x.OpenDevice());

                // Run
                LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0, 1000, UsbControlTransferType.Raw) {
                                                                                                                                                 Device = mockDevice.MockObject
                                                                                                                                         };
                lightsManager = new LightsManager(lightsDeviceController, UsbProtocolType.DasBlinkenlichten);
                BuildActiveRequest buildActiveRequest = new BuildActiveRequest(true);

                // Test
                Assert.That(lightsManager.NotifyLightsDevice(buildActiveRequest), NUnit.Framework.Is.EqualTo(LightsDeviceResult.NotOpen));
                this.mockFactory.VerifyAllExpectationsHaveBeenMet();
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the USB device responds witn not connected.
        /// </summary>
        [Test]
        public void TestNotifyUsbDeviceRespondsToBuildActiveRequest()
        {
            LightsManager lightsManager = null;
            try
            {
                // Mocks
                Mock<IHidDevice> mockDevice = this.mockFactory.CreateMock<IHidDevice>();

                // Expectations
                mockDevice.Expects.AtLeastOne.GetProperty(x => x.IsConnected).WillReturn(false);

                // Run
                LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0, 1000, UsbControlTransferType.Raw) {
                                                                                                                                                 Device = mockDevice.MockObject
                                                                                                                                         };
                lightsManager = new LightsManager(lightsDeviceController, UsbProtocolType.DasBlinkenlichten);
                BuildActiveRequest buildActiveRequest = new BuildActiveRequest(true);

                // Test
                Assert.That(lightsManager.NotifyLightsDevice(buildActiveRequest), NUnit.Framework.Is.EqualTo(LightsDeviceResult.NotConnected));
                this.mockFactory.VerifyAllExpectationsHaveBeenMet();
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the USB device responds witn not connected.
        /// </summary>
        [Test]
        public void TestNotifyUsbDeviceRespondsToBuildAttentionRequest()
        {
            LightsManager lightsManager = null;
            try
            {
                // Mocks
                Mock<IHidDevice> mockDevice = this.mockFactory.CreateMock<IHidDevice>();

                // Expectations
                mockDevice.Expects.AtLeastOne.GetProperty(x => x.IsConnected).WillReturn(false);

                // Run
                LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0, 1000, UsbControlTransferType.Raw) {
                                                                                                                                                 Device = mockDevice.MockObject
                                                                                                                                         };
                lightsManager = new LightsManager(lightsDeviceController, UsbProtocolType.DasBlinkenlichten);
                AttentionRequest request = new AttentionRequest(true);

                // Test
                Assert.That(lightsManager.NotifyLightsDevice(request), NUnit.Framework.Is.EqualTo(LightsDeviceResult.NotConnected));
                this.mockFactory.VerifyAllExpectationsHaveBeenMet();
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the light manager's Start method can be invoked twice without failure.
        /// </summary>
        [Test]
        [Timeout(5000)]
        public void TestStartAndStartAgain()
        {
            LightsManager lightsManager = null;
            try
            {
                LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0);
                lightsManager = new LightsManager(lightsDeviceController);
                lightsManager.Start();
                Assert.That(lightsManager.Running, NUnit.Framework.Is.True);
                lightsManager.Start();
                Assert.That(lightsManager.Running, NUnit.Framework.Is.True);
                lightsManager.Stop();
                Assert.That(lightsManager.Running, NUnit.Framework.Is.False);
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the light manager's Start and Stop methods can be invoked without failure.
        /// </summary>
        [Test]
        [Timeout(5000)]
        public void TestStartAndStop()
        {
            LightsManager lightsManager = null;
            try
            {
                LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0);
                lightsManager = new LightsManager(lightsDeviceController);
                lightsManager.Start();
                Assert.That(lightsManager.Running, NUnit.Framework.Is.True);
                lightsManager.Stop();
                Assert.That(lightsManager.Running, NUnit.Framework.Is.False);
            }
            finally
            {
                if (lightsManager != null)
                {
                    lightsManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the light manager's Start method can be invoked twice without failure.
        /// </summary>
        [Test]
        [Timeout(5000)]
        public void TestStopAndStopAgain()
        {
            LightsManager lightsManager = null;
            try
            {
                LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0);
                lightsManager = new LightsManager(lightsDeviceController);
                lightsManager.Start();
                Assert.That(lightsManager.Running, NUnit.Framework.Is.True);
                lightsManager.Stop();
                Assert.That(lightsManager.Running, NUnit.Framework.Is.False);
                lightsManager.Stop();
                Assert.That(lightsManager.Running, NUnit.Framework.Is.False);
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

        #region Implemented Interfaces

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.mockFactory != null)
            {
                this.mockFactory.Dispose();
            }
        }

        #endregion

        #endregion
    }
}