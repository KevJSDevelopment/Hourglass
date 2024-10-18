using AppLimiterLibrary.Dtos;

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

            MotivationalMessage message = new MotivationalMessage()
            {
                Id = 0,
                TypeId = 1,
                TypeDescription = "Message",
                ComputerId = ComputerIdentifier.GetUniqueIdentifier(),
                Message = args[0],
                FilePath = null
            };

            if (args.Length >= 2)
            {
                Application.Run(new LimiterMessagingForm(message, args[1], 0));
            }
            else
            {
                MessageBox.Show("Invalid arguments provided.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}