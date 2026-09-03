namespace JArchLicenseAdmin;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        string? key = SupabaseLicenseClient.ReadServiceKeyFromEnvironment();
        if (key == null)
        {
            MessageBox.Show(
                $"환경변수 {SupabaseLicenseClient.ServiceKeyEnvVar} 가 없습니다.\r\n\r\n" +
                "Supabase Dashboard → Project Settings → API Keys 에서 service_role 키를 복사한 뒤\r\n" +
                "PowerShell 에서 아래를 실행하고 이 프로그램을 다시 시작하세요.\r\n\r\n" +
                $"    setx {SupabaseLicenseClient.ServiceKeyEnvVar} \"eyJ...\"\r\n\r\n" +
                "setx 는 새로 뜨는 프로세스에만 적용됩니다.",
                "service_role 키 없음", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var client = new SupabaseLicenseClient(key);
        Application.Run(new MainForm(client));
    }
}
