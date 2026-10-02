export interface PortalLoginRequest {
  userName: string;
  password: string;
  deviceInfo?: string;
}

export interface AuthResponseData {
  token: string;
  expiresIn: number;
  isAdmin?: boolean | number | string;
  role?: string;
}

export interface UserSession {
  userId?: number;
  email: string;
  role: 'Admin' | 'Customer' | string;
  isAdmin: boolean;
  token: string;
}
