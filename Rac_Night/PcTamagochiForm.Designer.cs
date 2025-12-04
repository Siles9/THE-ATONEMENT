namespace Rac_Night
{
    partial class PcTamagochiForm
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
            this.pbRaccoonRoomBackground = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbRaccoonRoomBackground)).BeginInit();
            this.SuspendLayout();
            //
            // pbRaccoonRoomBackground
            //
            this.pbRaccoonRoomBackground.BackColor = System.Drawing.Color.Transparent;
            this.pbRaccoonRoomBackground.Image = global::Rac_Night.Properties.Resources.комната_енота; // <<<< УБЕДИТЕСЬ, ЧТО ИМЯ РЕСУРСА ПРАВИЛЬНОЕ
            this.pbRaccoonRoomBackground.Location = new System.Drawing.Point(100, 100); // <<<< ВАЖНО: ЭТИ КООРДИНАТЫ НУЖНО НАСТРОИТЬ ВРУЧНУЮ
            this.pbRaccoonRoomBackground.Name = "pbRaccoonRoomBackground";
            this.pbRaccoonRoomBackground.Size = new System.Drawing.Size(600, 400); // <<<< ВАЖНО: ЭТОТ РАЗМЕР НУЖНО НАСТРОИТЬ ВРУЧНУЮ
            this.pbRaccoonRoomBackground.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbRaccoonRoomBackground.TabIndex = 0;
            this.pbRaccoonRoomBackground.TabStop = false;
            //
            // PcTamagochiForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::Rac_Night.Properties.Resources.Экран_Планшет; // <<<< УБЕДИТЕСЬ, ЧТО ИМЯ РЕСУРСА ПРАВИЛЬНОЕ
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1400, 1200); // <<<< ВАЖНО: ЭТОТ РАЗМЕР НУЖНО НАСТРОИТЬ ПОД "Экран_Планшет"
            this.ControlBox = false; // Убирает системные кнопки закрытия, минимизации
            this.Controls.Add(this.pbRaccoonRoomBackground);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None; // Убирает рамки
            this.KeyPreview = true; // Позволяет форме перехватывать нажатия клавиш
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PcTamagochiForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen; // Открывается по центру экрана
            this.Text = "Tamagochi Tablet"; // Заголовок (не будет виден из-за FormBorderStyle.None)
            ((System.ComponentModel.ISupportInitialize)(this.pbRaccoonRoomBackground)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbRaccoonRoomBackground;
    }
}
