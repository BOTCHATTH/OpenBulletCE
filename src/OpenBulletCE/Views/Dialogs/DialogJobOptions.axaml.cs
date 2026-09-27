using Avalonia.Controls;
using Avalonia.Interactivity;
using RuriLib.ViewModels;

namespace OpenBulletCE.Views.Dialogs
{
    /// <summary>
    /// OB2-style "Edit Job" options dialog — edits the runner's settings in place.
    /// </summary>
    public partial class DialogJobOptions : Window
    {
        /// <summary>Minutes to wait before starting the runner (0 = start immediately).</summary>
        public int DelayedStartMinutes { get; set; }

        /// <summary>The runner's settings, bound live in the dialog.</summary>
        public RLSettingsViewModel Settings { get; }

        public DialogJobOptions()
        {
            InitializeComponent();
        }

        public DialogJobOptions(RLSettingsViewModel settings) : this()
        {
            Settings = settings;
            DataContext = this;
        }

        private void Accept_Click(object sender, RoutedEventArgs e) => Close(true);

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close(false);
    }
}
