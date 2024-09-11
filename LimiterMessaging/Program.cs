namespace LimiterMessaging
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string message = args.Length > 0 ? args[0] : "No message provided.";

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(message));
        }
    }
}