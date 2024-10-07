namespace LimiterMessaging
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length >= 2)
            {
                Application.Run(new LimiterMessagingForm(args[0], args[1]));
            }
            else
            {
                MessageBox.Show("Invalid arguments provided.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}