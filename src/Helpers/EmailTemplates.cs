namespace authModule.src.Helpers
{
    public static class EmailTemplates
    {
        public static string GetOtpEmailBody(string otpCode, int expiryMinutes)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .otp-code {{ background-color: #f8f9fa; border: 2px dashed #dee2e6; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #495057; margin: 20px 0; border-radius: 4px; }}
        .info {{ color: #6c757d; font-size: 14px; text-align: center; margin-top: 20px; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6; color: #6c757d; font-size: 12px; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='color: #343a40; margin: 0;'>Authentication Code</h1>
        </div>
        <p style='color: #495057; font-size: 16px;'>Your OTP code for login is:</p>
        <div class='otp-code'>{otpCode}</div>
        <div class='info'>
            <p>This code will expire in <strong>{expiryMinutes} minutes</strong>.</p>
            <p>If you didn't request this code, please ignore this email.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
