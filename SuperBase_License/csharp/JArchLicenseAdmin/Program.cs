namespace JArchLicenseAdmin;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 보통은 소스에 박힌 키가 쓰인다. 환경변수를 두면 재빌드 없이 교체할 수 있다.
        string? key = SupabaseLicenseClient.ResolveServiceKey();
        if (key == null)
        {
            MessageBox.Show(
                "service_role 키가 없습니다.\r\n\r\n" +
                "SupabaseLicenseClient.EmbeddedServiceKey 가 비어 있고 " +
                $"환경변수 {SupabaseLicenseClient.ServiceKeyEnvVar} 도 설정돼 있지 않습니다.\r\n\r\n" +
                "Supabase Dashboard → Project Settings → API Keys 에서 service_role 키를 복사한 뒤\r\n" +
                "아래를 실행하고 다시 시작하세요.\r\n\r\n" +
                $"    setx {SupabaseLicenseClient.ServiceKeyEnvVar} \"eyJ...\"",
                "service_role 키 없음", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var client = new SupabaseLicenseClient(key);
        Application.Run(new MainForm(client));
    }
}
