/*using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;*/

namespace TC.CRM.Bot.Admin.GoogleSheets
{
    public class GoogleSheetsHelper
    {
        /*public SheetsService Service { get; set; }
        const string APPLICATION_NAME = "TestBot";
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        public GoogleSheetsHelper()
        {
            InitializeService();
        }
        private void InitializeService()
        {
            var credential = GetCredentialsFromFile();
            Service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = APPLICATION_NAME
            });
        }
        private GoogleCredential GetCredentialsFromFile()
        {
            GoogleCredential credential;
            using (var stream = new FileStream("GoogleSheets/client_secrets_testtest.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }
            return credential;
        }*/
    }
}
