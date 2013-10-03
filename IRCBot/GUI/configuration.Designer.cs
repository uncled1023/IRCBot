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
            this.connect_button = new System.Windows.Forms.Button();
            this.edit_server_button = new System.Windows.Forms.Button();
            this.delete_server_button = new System.Windows.Forms.Button();
            this.add_server_button = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.server_list = new System.Windows.Forms.ListBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.minimize_to_tray = new System.Windows.Forms.CheckBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.windows_start_box = new System.Windows.Forms.CheckBox();
            this.browse_button = new System.Windows.Forms.Button();
            this.log_folder_box = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.keep_logs_box = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(417, 219);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.connect_button);
            this.tabPage1.Controls.Add(this.edit_server_button);
            this.tabPage1.Controls.Add(this.delete_server_button);
            this.tabPage1.Controls.Add(this.add_server_button);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.server_list);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(409, 193);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Server Configuration";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // connect_button
            // 
            this.connect_button.Enabled = false;
            this.connect_button.Location = new System.Drawing.Point(262, 119);
            this.connect_button.Name = "connect_button";
            this.connect_button.Size = new System.Drawing.Size(102, 23);
            this.connect_button.TabIndex = 5;
            this.connect_button.Text = "Connect";
            this.connect_button.UseVisualStyleBackColor = true;
            this.connect_button.Click += new System.EventHandler(this.connect_button_Click);
            // 
            // edit_server_button
            // 
            this.edit_server_button.Enabled = false;
            this.edit_server_button.Location = new System.Drawing.Point(262, 61);
            this.edit_server_button.Name = "edit_server_button";
            this.edit_server_button.Size = new System.Drawing.Size(102, 23);
            this.edit_server_button.TabIndex = 3;
            this.edit_server_button.Text = "Edit Server";
            this.edit_server_button.UseVisualStyleBackColor = true;
            this.edit_server_button.Click += new System.EventHandler(this.edit_server_button_Click);
            // 
            // delete_server_button
            // 
            this.delete_server_button.Enabled = false;
            this.delete_server_button.Location = new System.Drawing.Point(262, 90);
            this.delete_server_button.Name = "delete_server_button";
            this.delete_server_button.Size = new System.Drawing.Size(102, 23);
            this.delete_server_button.TabIndex = 4;
            this.delete_server_button.Text = "Delete Server";
            this.delete_server_button.UseVisualStyleBackColor = true;
            this.delete_server_button.Click += new System.EventHandler(this.delete_server_button_Click);
            // 
            // add_server_button
            // 
            this.add_server_button.Location = new System.Drawing.Point(262, 32);
            this.add_server_button.Name = "add_server_button";
            this.add_server_button.Size = new System.Drawing.Size(102, 23);
            this.add_server_button.TabIndex = 2;
            this.add_server_button.Text = "Add Server";
            this.add_server_button.UseVisualStyleBackColor = true;
            this.add_server_button.Click += new System.EventHandler(this.add_server_button_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(6, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Server List";
            // 
            // server_list
            // 
            this.server_list.FormattingEnabled = true;
            this.server_list.Location = new System.Drawing.Point(9, 32);
            this.server_list.Name = "server_list";
            this.server_list.Size = new System.Drawing.Size(201, 147);
            this.server_list.TabIndex = 1;
            this.server_list.SelectedIndexChanged += new System.EventHandler(this.server_list_SelectedIndexChanged);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.minimize_to_tray);
            this.tabPage2.Controls.Add(this.label12);
            this.tabPage2.Controls.Add(this.label11);
            this.tabPage2.Controls.Add(this.windows_start_box);
            this.tabPage2.Controls.Add(this.browse_button);
            this.tabPage2.Controls.Add(this.log_folder_box);
            this.tabPage2.Controls.Add(this.label10);
            this.tabPage2.Controls.Add(this.keep_logs_box);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(409, 193);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Advanced";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 13);
            this.label2.TabIndex = 29;
            this.label2.Text = "Minimize to Tray";
            // 
            // minimize_to_tray
            // 
            this.minimize_to_tray.AutoSize = true;
            this.minimize_to_tray.Location = new System.Drawing.Point(177, 89);
            this.minimize_to_tray.Name = "minimize_to_tray";
            this.minimize_to_tray.Size = new System.Drawing.Size(15, 14);
            this.minimize_to_tray.TabIndex = 10;
            this.minimize_to_tray.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 63);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(135, 13);
            this.label12.TabIndex = 27;
            this.label12.Text = "Start when Windows Starts";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 14);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(95, 13);
            this.label11.TabIndex = 26;
            this.label11.Text = "Keep logs of chats";
            // 
            // windows_start_box
            // 
            this.windows_start_box.AutoSize = true;
            this.windows_start_box.Location = new System.Drawing.Point(177, 65);
            this.windows_start_box.Name = "windows_start_box";
            this.windows_start_box.Size = new System.Drawing.Size(15, 14);
            this.windows_start_box.TabIndex = 9;
            this.windows_start_box.UseVisualStyleBackColor = true;
            // 
            // browse_button
            // 
            this.browse_button.Location = new System.Drawing.Point(327, 37);
            this.browse_button.Name = "browse_button";
            this.browse_button.Size = new System.Drawing.Size(65, 23);
            this.browse_button.TabIndex = 8;
            this.browse_button.Text = "Browse";
            this.browse_button.UseVisualStyleBackColor = true;
            this.browse_button.Click += new System.EventHandler(this.browse_button_Click);
            // 
            // log_folder_box
            // 
            this.log_folder_box.Location = new System.Drawing.Point(177, 39);
            this.log_folder_box.Name = "log_folder_box";
            this.log_folder_box.Size = new System.Drawing.Size(144, 20);
            this.log_folder_box.TabIndex = 7;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 39);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(85, 13);
            this.label10.TabIndex = 22;
            this.label10.Text = "Log Save Folder";
            // 
            // keep_logs_box
            // 
            this.keep_logs_box.AutoSize = true;
            this.keep_logs_box.Location = new System.Drawing.Point(177, 16);
            this.keep_logs_box.Name = "keep_logs_box";
            this.keep_logs_box.Size = new System.Drawing.Size(15, 14);
            this.keep_logs_box.TabIndex = 6;
            this.keep_logs_box.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(353, 225);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 51;
            this.button1.Text = "Cancel";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(272, 225);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 50;
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
            this.ClientSize = new System.Drawing.Size(440, 258);
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
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ListBox server_list;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button delete_server_button;
        private System.Windows.Forms.Button add_server_button;
        private System.Windows.Forms.Button edit_server_button;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox windows_start_box;
        private System.Windows.Forms.Button browse_button;
        private System.Windows.Forms.TextBox log_folder_box;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox keep_logs_box;
        private System.Windows.Forms.Button connect_button;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox minimize_to_tray;
    }
}