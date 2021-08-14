
namespace CryptoDeobber
{
    partial class Form1
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
            this.filePathBox = new System.Windows.Forms.TextBox();
            this.openFileBtn = new System.Windows.Forms.Button();
            this.filePathLbl = new System.Windows.Forms.Label();
            this.deobBtn = new System.Windows.Forms.Button();
            this.statusLbl = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // filePathBox
            // 
            this.filePathBox.Location = new System.Drawing.Point(51, 24);
            this.filePathBox.Name = "filePathBox";
            this.filePathBox.Size = new System.Drawing.Size(515, 20);
            this.filePathBox.TabIndex = 0;
            // 
            // openFileBtn
            // 
            this.openFileBtn.Location = new System.Drawing.Point(17, 21);
            this.openFileBtn.Name = "openFileBtn";
            this.openFileBtn.Size = new System.Drawing.Size(28, 23);
            this.openFileBtn.TabIndex = 1;
            this.openFileBtn.Text = "...";
            this.openFileBtn.UseVisualStyleBackColor = true;
            this.openFileBtn.Click += new System.EventHandler(this.openFileBtn_Click);
            // 
            // filePathLbl
            // 
            this.filePathLbl.AutoSize = true;
            this.filePathLbl.Location = new System.Drawing.Point(48, 8);
            this.filePathLbl.Name = "filePathLbl";
            this.filePathLbl.Size = new System.Drawing.Size(108, 13);
            this.filePathLbl.TabIndex = 2;
            this.filePathLbl.Text = "File path to assembly:";
            // 
            // deobBtn
            // 
            this.deobBtn.Location = new System.Drawing.Point(17, 113);
            this.deobBtn.Name = "deobBtn";
            this.deobBtn.Size = new System.Drawing.Size(81, 23);
            this.deobBtn.TabIndex = 3;
            this.deobBtn.Text = "Deobfuscate";
            this.deobBtn.UseVisualStyleBackColor = true;
            this.deobBtn.Click += new System.EventHandler(this.deobBtn_Click);
            // 
            // statusLbl
            // 
            this.statusLbl.AutoSize = true;
            this.statusLbl.ForeColor = System.Drawing.Color.Blue;
            this.statusLbl.Location = new System.Drawing.Point(48, 47);
            this.statusLbl.Name = "statusLbl";
            this.statusLbl.Size = new System.Drawing.Size(43, 13);
            this.statusLbl.TabIndex = 4;
            this.statusLbl.Text = "Status: ";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(578, 148);
            this.Controls.Add(this.statusLbl);
            this.Controls.Add(this.deobBtn);
            this.Controls.Add(this.filePathLbl);
            this.Controls.Add(this.openFileBtn);
            this.Controls.Add(this.filePathBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.Text = "Crypto Deobfuscator";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox filePathBox;
        private System.Windows.Forms.Button openFileBtn;
        private System.Windows.Forms.Label filePathLbl;
        private System.Windows.Forms.Button deobBtn;
        private System.Windows.Forms.Label statusLbl;
    }
}

