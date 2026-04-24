export interface LoginResponse {
  accessToken: string;
  user: {
    userName: string;
    fullName: string;
    role: string;
  };
}
