namespace WhatsThatLight.Ci.Tools.BuildLights.Server
{
    /// <summary>
    /// Generated code. 
    /// </summary>
    partial class ProjectInstaller
    {
        /// <summary>
        /// Generated code. 
        /// </summary>
        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;

        /// <summary>
        /// Generated code. 
        /// </summary>
        private System.ServiceProcess.ServiceInstaller serviceInstaller;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;
            // 
            // serviceInstaller
            // 
            this.serviceInstaller.Description = "Server for controlling Continuous Integration Lights.";
            this.serviceInstaller.DisplayName = "Continuous Integration Lights Server Service";
            this.serviceInstaller.ServiceName = "ContinuousIntegrationLightsServerService";
            this.serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller,
            this.serviceInstaller});

        }

        #endregion
    }
}