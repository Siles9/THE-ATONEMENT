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
            this.pbRaccoonRoomBackground.BackColor = System.Drawing.Color.White; // Фон для комнаты енота (чтобы заполнить прозрачные участки)
            this.pbRaccoonRoomBackground.Image = global::Rac_Night.Properties.Resources.комната_енота; // <<<< УБЕДИТЕСЬ, ЧТО ИМЯ РЕСУРСА ПРАВИЛЬНОЕ

            // Новые Location и Size для комнаты енота
            // Рассчитано для формы 1280x720, чтобы комната енота вписалась в экран монитора
            this.pbRaccoonRoomBackground.Location = new System.Drawing.Point(33, 33);
            this.pbRaccoonRoomBackground.Name = "pbRaccoonRoomBackground";
            this.pbRaccoonRoomBackground.Size = new System.Drawing.Size(1214, 640);
            this.pbRaccoonRoomBackground.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage; // Растягивает изображение, чтобы оно заполнило весь PictureBox
            this.pbRaccoonRoomBackground.TabIndex = 0;
            this.pbRaccoonRoomBackground.TabStop = false;
            //
            // PcTamagochiForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            // ВОТ ГЛАВНОЕ ИСПРАВЛЕНИЕ: Устанавливаем Монитор как фоновое изображение формы
            this.BackgroundImage = global::Rac_Night.Properties.Resources.Монитор; // <<<< УБЕДИТЕСЬ, ЧТО ИМЯ РЕСУРСА ПРАВИЛЬНОЕ
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch; // Растягиваем монитор на всю форму

            // Новый размер формы: 1280x720 (сохраняет пропорции 16:9 Монитора, но не на весь экран)
            this.ClientSize = new System.Drawing.Size(1280, 720);

            this.ControlBox = false; // Убирает системные кнопки закрытия, минимизации
            this.Controls.Add(this.pbRaccoonRoomBackground);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None; // Убирает рамки
            this.KeyPreview = true; // Позволяет форме перехватывать нажатия клавиш (для Esc)
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PcTamagochiForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen; // Открывается по центру экрана
            this.Text = "Tamagochi Monitor"; // Заголовок (не будет виден из-за FormBorderStyle.None)
            ((System.ComponentModel.ISupportInitialize)(this.pbRaccoonRoomBackground)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbRaccoonRoomBackground;
    }
}