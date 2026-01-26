namespace authModule.src.Helpers
{
    public class OtpHelper
    {
        private readonly IConfiguration _config;

        public OtpHelper(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateOtpCode()
        {
            var codeLength = _config.GetValue<int>("Otp:CodeLength", 6);
            var random = new Random();
            var code = string.Empty;

            for (int i = 0; i < codeLength; i++)
            {
                code += random.Next(0, 10).ToString();
            }

            return code;
        }

        public DateTime GetOtpExpiryTime()
        {
            var expirationMinutes = _config.GetValue<int>("Otp:ExpirationMinutes", 5);
            return DateTime.UtcNow.AddMinutes(expirationMinutes);
        }

        public int GetExpirationMinutes()
        {
            return _config.GetValue<int>("Otp:ExpirationMinutes", 5);
        }
    }
}
