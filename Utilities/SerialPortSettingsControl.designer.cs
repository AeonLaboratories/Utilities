namespace Utilities
{
    partial class SerialPortSettingsControl
    {
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
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.baudRate = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.portName = new System.Windows.Forms.ComboBox();
            this.parity = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.dataBits = new System.Windows.Forms.ComboBox();
            this.stopBits = new System.Windows.Forms.ComboBox();
            this.handshake = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // baudRate
            // 
            this.baudRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.baudRate.FormattingEnabled = true;
            this.baudRate.Location = new System.Drawing.Point(71, 30);
            this.baudRate.Name = "baudRate";
            this.baudRate.Size = new System.Drawing.Size(141, 21);
            this.baudRate.TabIndex = 1;
            this.baudRate.SelectedValueChanged += new System.EventHandler(this.baudRateChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(39, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Port";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(33, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Baud";
            // 
            // portName
            // 
            this.portName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.portName.FormattingEnabled = true;
            this.portName.Location = new System.Drawing.Point(71, 3);
            this.portName.Name = "portName";
            this.portName.Size = new System.Drawing.Size(141, 21);
            this.portName.TabIndex = 4;
            this.portName.SelectedValueChanged += new System.EventHandler(this.portNameChanged);
            // 
            // parity
            // 
            this.parity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.parity.FormattingEnabled = true;
            this.parity.Location = new System.Drawing.Point(71, 57);
            this.parity.Name = "parity";
            this.parity.Size = new System.Drawing.Size(141, 21);
            this.parity.TabIndex = 5;
            this.parity.SelectedValueChanged += new System.EventHandler(this.parityChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(32, 60);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Parity";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 87);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(49, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Data bits";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 114);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Stop Bits";
            // 
            // dataBits
            // 
            this.dataBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dataBits.FormattingEnabled = true;
            this.dataBits.Location = new System.Drawing.Point(71, 84);
            this.dataBits.Name = "dataBits";
            this.dataBits.Size = new System.Drawing.Size(141, 21);
            this.dataBits.TabIndex = 9;
            this.dataBits.SelectedValueChanged += new System.EventHandler(this.dataBitsChanged);
            // 
            // stopBits
            // 
            this.stopBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.stopBits.FormattingEnabled = true;
            this.stopBits.Location = new System.Drawing.Point(71, 111);
            this.stopBits.Name = "stopBits";
            this.stopBits.Size = new System.Drawing.Size(141, 21);
            this.stopBits.TabIndex = 10;
            this.stopBits.SelectedValueChanged += new System.EventHandler(this.stopBitsChanged);
            // 
            // handshake
            // 
            this.handshake.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.handshake.FormattingEnabled = true;
            this.handshake.Location = new System.Drawing.Point(71, 138);
            this.handshake.Name = "handshake";
            this.handshake.Size = new System.Drawing.Size(141, 21);
            this.handshake.TabIndex = 12;
            this.handshake.SelectedValueChanged += new System.EventHandler(this.handshakeChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 141);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(62, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "Handshake";
            // 
            // SerialPortSettingsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.handshake);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.stopBits);
            this.Controls.Add(this.dataBits);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.parity);
            this.Controls.Add(this.portName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.baudRate);
            this.Name = "SerialPortSettingsControl";
            this.Size = new System.Drawing.Size(215, 162);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
        public System.Windows.Forms.ComboBox baudRate;
        public System.Windows.Forms.ComboBox portName;
        public System.Windows.Forms.ComboBox parity;
        public System.Windows.Forms.ComboBox dataBits;
        public System.Windows.Forms.ComboBox stopBits;
        public System.Windows.Forms.ComboBox handshake;
    }
}
