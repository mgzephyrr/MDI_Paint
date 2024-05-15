using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace MDI_Paint
{
    public partial class ProgressBarWindow : Window
    {
        public event EventHandler CancelRequested;
        public ProgressBarWindow()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int progress)
        {
            progressBar.Value = progress;
        }

        // Переопределяем обработчик события Closing
        protected override void OnClosing(CancelEventArgs e)
        {
            // Вызываем событие отмены, если есть подписчики
            CancelRequested?.Invoke(this, EventArgs.Empty);
            base.OnClosing(e);
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}