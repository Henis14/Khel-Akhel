export interface PortalLoginRequest {
  userName: string;
  password: string;
  deviceInfo?: string;
  captchaId?: string;
  captcha?: string;
}

export interface CaptchaResponseData {
  captchaId: string;
  captchaImage: string;
}

export interface AuthResponseData {
  token: string;
  expiresIn: number;
  isAdmin?: boolean | number | string;
  role?: string;
}

/** Reset Password — change an existing password (email + mobile must match the account). */
export interface ChangePasswordRequest {
  email: string;
  mobileNo: string;
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

/** Forgot Password — set a new password after matching email + mobile and passing the CAPTCHA. */
export interface ForgotPasswordRequest {
  email: string;
  mobileNo: string;
  newPassword: string;
  confirmPassword: string;
  captchaId?: string;
  captcha?: string;
}

export interface UserSession {
  userId?: number;
  email: string;
  role: 'Admin' | 'Customer' | string;
  isAdmin: boolean;
  token: string;
}
