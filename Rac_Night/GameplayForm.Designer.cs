namespace Rac_Night
{
    partial class GameplayForm
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
            this._timeLabel = new System.Windows.Forms.Label();
            this._blindOverlay = new System.Windows.Forms.Panel();
            this._flashlightPicture = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this._flashlightPicture)).BeginInit();
            this.SuspendLayout();
            // 
            // _timeLabel
            // 
            this._timeLabel.AutoSize = true;
            this._timeLabel.BackColor = System.Drawing.Color.Transparent;
            this._timeLabel.Font = new System.Drawing.Font("Arial", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._timeLabel.ForeColor = System.Drawing.Color.Lime;
            this._timeLabel.Location = new System.Drawing.Point(20, 20);
            this._timeLabel.Name = "_timeLabel";
            this._timeLabel.Size = new System.Drawing.Size(181, 32);
            this._timeLabel.TabIndex = 1;
            this._timeLabel.Text = "НОЧЬ: 00:00";
            // 
            // _blindOverlay
            // 
            this._blindOverlay.BackColor = System.Drawing.Color.Black;
            this._blindOverlay.Location = new System.Drawing.Point(0, 0);
            this._blindOverlay.Name = "_blindOverlay";
            this._blindOverlay.Size = new System.Drawing.Size(100, 100);
            this._blindOverlay.TabIndex = 2;
            this._blindOverlay.Visible = false;
            // 
            // _flashlightPicture
            // 
            this._flashlightPicture.BackColor = System.Drawing.Color.Transparent;
            this._flashlightPicture.Location = new System.Drawing.Point(0, 0);
            this._flashlightPicture.Name = "_flashlightPicture";
            this._flashlightPicture.Size = new System.Drawing.Size(400, 400);
            this._flashlightPicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this._flashlightPicture.TabIndex = 3;
            this._flashlightPicture.TabStop = false;
            this._flashlightPicture.Visible = false;
            // 
            // GameplayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::Rac_Night.Properties.Resources.Окружение;
            this.ClientSize = new System.Drawing.Size(1904, 1041);
            this.Controls.Add(this._flashlightPicture);
            this.Controls.Add(this._blindOverlay);
            this.Controls.Add(this._timeLabel);
            this.Name = "GameplayForm";
            this.Text = "GameplayForm";
            ((System.ComponentModel.ISupportInitialize)(this._flashlightPicture)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label _timeLabel;
        private System.Windows.Forms.Panel _blindOverlay;
        private System.Windows.Forms.PictureBox _flashlightPicture;
    }
}