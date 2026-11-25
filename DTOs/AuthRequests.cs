namespace AquillaTextiles.DTOs;

public record SignupRequest(string Username, string Email, string Password, string ConfirmPassword);
public record LoginRequest(string Username, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);
