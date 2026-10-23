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

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface UserSession {
  userId?: number;
  email: string;
  role: 'Admin' | 'Customer' | string;
  isAdmin: boolean;
  token: string;
}
