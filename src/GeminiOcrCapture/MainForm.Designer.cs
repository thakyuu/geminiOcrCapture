namespace GeminiOcrCapture
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                
                // SoundPlayerのリソースを解放
                if (_soundPlayer != null)
                {
                    _soundPlayer.Dispose();
                }

                // NotifyIconのリソースを解放
                if (_notifyIcon != null)
                {
                    _notifyIcon.Dispose();
                }

                // ContextMenuStripのリソースを解放
                if (_contextMenu != null)
                {
                    _contextMenu.Dispose();
                }
                
                // ScreenCaptureのリソースを解放
                if (_screenCapture != null)
                {
                    _screenCapture.Dispose();
                }
                
                // GeminiServiceのリソースを解放
                if (_geminiService != null)
                {
                    _geminiService.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1, 1);
            this.Name = "MainForm";
            this.Text = "Gemini OCR Capture";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.ResumeLayout(false);
        }

        #endregion
    }
}