namespace IRCBot
{
    partial class configuration
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.port_box = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.channels_box = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.server_name_box = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.max_message_length_box = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.spam_threshold_box = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.spam_timeout_box = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.spam_count_box = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.command_prefix_box = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.email_box = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.password_box = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.owner_nicks_box = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.bot_nick_box = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.bot_name_box = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.windows_start_box = new System.Windows.Forms.CheckBox();
            this.browse_button = new System.Windows.Forms.Button();
            this.log_folder_box = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.keep_logs_box = new System.Windows.Forms.CheckBox();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(12, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(400, 351);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.port_box);
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Controls.Add(this.channels_box);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.server_name_box);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(392, 325);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Server";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // port_box
            // 
            this.port_box.Location = new System.Drawing.Point(201, 41);
            this.port_box.Name = "port_box";
            this.port_box.Size = new System.Drawing.Size(185, 20);
            this.port_box.TabIndex = 5;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 44);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(66, 13);
            this.label7.TabIndex = 4;
            this.label7.Text = "Port Number";
            // 
            // channels_box
            // 
            this.channels_box.Location = new System.Drawing.Point(201, 67);
            this.channels_box.Name = "channels_box";
            this.channels_box.Size = new System.Drawing.Size(185, 20);
            this.channels_box.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(117, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Channels to join at start";
            // 
            // server_name_box
            // 
            this.server_name_box.Location = new System.Drawing.Point(201, 15);
            this.server_name_box.Name = "server_name_box";
            this.server_name_box.Size = new System.Drawing.Size(185, 20);
            this.server_name_box.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server Name";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.max_message_length_box);
            this.tabPage2.Controls.Add(this.label16);
            this.tabPage2.Controls.Add(this.spam_threshold_box);
            this.tabPage2.Controls.Add(this.label15);
            this.tabPage2.Controls.Add(this.spam_timeout_box);
            this.tabPage2.Controls.Add(this.label13);
            this.tabPage2.Controls.Add(this.spam_count_box);
            this.tabPage2.Controls.Add(this.label14);
            this.tabPage2.Controls.Add(this.command_prefix_box);
            this.tabPage2.Controls.Add(this.label9);
            this.tabPage2.Controls.Add(this.email_box);
            this.tabPage2.Controls.Add(this.label8);
            this.tabPage2.Controls.Add(this.password_box);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.owner_nicks_box);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.bot_nick_box);
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.bot_name_box);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(392, 325);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Bot Configuration";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // max_message_length_box
            // 
            this.max_message_length_box.Location = new System.Drawing.Point(201, 247);
            this.max_message_length_box.Name = "max_message_length_box";
            this.max_message_length_box.Size = new System.Drawing.Size(185, 20);
            this.max_message_length_box.TabIndex = 20;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(8, 250);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(109, 13);
            this.label16.TabIndex = 19;
            this.label16.Text = "Max Message Length";
            // 
            // spam_threshold_box
            // 
            this.spam_threshold_box.Location = new System.Drawing.Point(201, 195);
            this.spam_threshold_box.Name = "spam_threshold_box";
            this.spam_threshold_box.Size = new System.Drawing.Size(185, 20);
            this.spam_threshold_box.TabIndex = 18;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(8, 198);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(150, 13);
            this.label15.TabIndex = 17;
            this.label15.Text = "Spam Threshold (Milliseconds)";
            // 
            // spam_timeout_box
            // 
            this.spam_timeout_box.Location = new System.Drawing.Point(201, 221);
            this.spam_timeout_box.Name = "spam_timeout_box";
            this.spam_timeout_box.Size = new System.Drawing.Size(185, 20);
            this.spam_timeout_box.TabIndex = 16;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(8, 224);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(141, 13);
            this.label13.TabIndex = 15;
            this.label13.Text = "Spam Timeout (Milliseconds)";
            // 
            // spam_count_box
            // 
            this.spam_count_box.Location = new System.Drawing.Point(201, 169);
            this.spam_count_box.Name = "spam_count_box";
            this.spam_count_box.Size = new System.Drawing.Size(185, 20);
            this.spam_count_box.TabIndex = 14;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(8, 172);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(65, 13);
            this.label14.TabIndex = 13;
            this.label14.Text = "Spam Count";
            // 
            // command_prefix_box
            // 
            this.command_prefix_box.Location = new System.Drawing.Point(201, 145);
            this.command_prefix_box.Name = "command_prefix_box";
            this.command_prefix_box.Size = new System.Drawing.Size(185, 20);
            this.command_prefix_box.TabIndex = 12;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(8, 148);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(83, 13);
            this.label9.TabIndex = 11;
            this.label9.Text = "Command Prefix";
            // 
            // email_box
            // 
            this.email_box.Location = new System.Drawing.Point(201, 93);
            this.email_box.Name = "email_box";
            this.email_box.Size = new System.Drawing.Size(185, 20);
            this.email_box.TabIndex = 10;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(8, 96);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(32, 13);
            this.label8.TabIndex = 9;
            this.label8.Text = "Email";
            // 
            // password_box
            // 
            this.password_box.Location = new System.Drawing.Point(201, 67);
            this.password_box.Name = "password_box";
            this.password_box.Size = new System.Drawing.Size(185, 20);
            this.password_box.TabIndex = 8;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 70);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(53, 13);
            this.label6.TabIndex = 7;
            this.label6.Text = "Password";
            // 
            // owner_nicks_box
            // 
            this.owner_nicks_box.Location = new System.Drawing.Point(201, 119);
            this.owner_nicks_box.Name = "owner_nicks_box";
            this.owner_nicks_box.Size = new System.Drawing.Size(185, 20);
            this.owner_nicks_box.TabIndex = 6;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 122);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(75, 13);
            this.label5.TabIndex = 5;
            this.label5.Text = "Owner\'s Nicks";
            // 
            // bot_nick_box
            // 
            this.bot_nick_box.Location = new System.Drawing.Point(201, 41);
            this.bot_nick_box.Name = "bot_nick_box";
            this.bot_nick_box.Size = new System.Drawing.Size(185, 20);
            this.bot_nick_box.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 44);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(29, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Nick";
            // 
            // bot_name_box
            // 
            this.bot_name_box.Location = new System.Drawing.Point(201, 15);
            this.bot_name_box.Name = "bot_name_box";
            this.bot_name_box.Size = new System.Drawing.Size(185, 20);
            this.bot_name_box.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Name";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.label12);
            this.tabPage3.Controls.Add(this.label11);
            this.tabPage3.Controls.Add(this.windows_start_box);
            this.tabPage3.Controls.Add(this.browse_button);
            this.tabPage3.Controls.Add(this.log_folder_box);
            this.tabPage3.Controls.Add(this.label10);
            this.tabPage3.Controls.Add(this.keep_logs_box);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(392, 325);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Application Settings";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(8, 65);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(135, 13);
            this.label12.TabIndex = 8;
            this.label12.Text = "Start when Windows Starts";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(8, 16);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(95, 13);
            this.label11.TabIndex = 7;
            this.label11.Text = "Keep logs of chats";
            // 
            // windows_start_box
            // 
            this.windows_start_box.AutoSize = true;
            this.windows_start_box.Location = new System.Drawing.Point(201, 64);
            this.windows_start_box.Name = "windows_start_box";
            this.windows_start_box.Size = new System.Drawing.Size(15, 14);
            this.windows_start_box.TabIndex = 6;
            this.windows_start_box.UseVisualStyleBackColor = true;
            // 
            // browse_button
            // 
            this.browse_button.Location = new System.Drawing.Point(321, 36);
            this.browse_button.Name = "browse_button";
            this.browse_button.Size = new System.Drawing.Size(65, 23);
            this.browse_button.TabIndex = 5;
            this.browse_button.Text = "Browse";
            this.browse_button.UseVisualStyleBackColor = true;
            this.browse_button.Click += new System.EventHandler(this.browse_button_Click);
            // 
            // log_folder_box
            // 
            this.log_folder_box.Location = new System.Drawing.Point(201, 38);
            this.log_folder_box.Name = "log_folder_box";
            this.log_folder_box.Size = new System.Drawing.Size(114, 20);
            this.log_folder_box.TabIndex = 4;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(8, 41);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(85, 13);
            this.label10.TabIndex = 3;
            this.label10.Text = "Log Save Folder";
            // 
            // keep_logs_box
            // 
            this.keep_logs_box.AutoSize = true;
            this.keep_logs_box.Location = new System.Drawing.Point(201, 15);
            this.keep_logs_box.Name = "keep_logs_box";
            this.keep_logs_box.Size = new System.Drawing.Size(15, 14);
            this.keep_logs_box.TabIndex = 0;
            this.keep_logs_box.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.tabControl2);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(392, 325);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Module Configuration";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabControl2
            // 
            this.tabControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl2.HotTrack = true;
            this.tabControl2.Location = new System.Drawing.Point(3, 3);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(386, 319);
            this.tabControl2.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(337, 357);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Cancel";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(256, 357);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "Save";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(424, 392);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.tabControl1);
            this.MaximizeBox = false;
            this.Name = "configuration";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configuration";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox channels_box;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox server_name_box;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox bot_nick_box;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox bot_name_box;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox port_box;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox command_prefix_box;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox email_box;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox password_box;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox owner_nicks_box;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.CheckBox windows_start_box;
        private System.Windows.Forms.Button browse_button;
        private System.Windows.Forms.TextBox log_folder_box;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox keep_logs_box;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.TextBox spam_timeout_box;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox spam_count_box;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox spam_threshold_box;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TextBox max_message_length_box;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TabControl tabControl2;
    }
}