
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface LoginRequest {
  portalUsername: string;
  password: string;
}

export interface LoginResponse {
  isSuccessful: boolean;
  accessToken: string;
  expiresAt: string;
  userId: number;
  employeeId: number;
  portalUsername: string;
  employeeNumber: string;
  employeeName: string;
  roles: string[];
  message: string;
}

export interface CurrentUser {
  userId: number;
  employeeId: number;
  portalUsername: string;
  employeeNumber: string;
  employeeName: string;
  roles: string[];
  expiresAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5219/api/portal-accounts';

  login(
    request: LoginRequest,
    rememberMe: boolean
  ): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/login`, request)
      .pipe(
        tap((response: LoginResponse) => {
          if (response.isSuccessful && response.accessToken) {
            this.storeToken(response, rememberMe);
          }
        })
      );
  }

  private storeToken(
    response: LoginResponse,
    rememberMe: boolean
  ): void {
    this.clearToken();

    const storage = rememberMe ? localStorage : sessionStorage;

    const user: CurrentUser = {
      userId: response.userId,
      employeeId: response.employeeId,
      portalUsername: response.portalUsername,
      employeeNumber: response.employeeNumber,
      employeeName: response.employeeName,
      roles: response.roles,
      expiresAt: response.expiresAt
    };

    storage.setItem('clams_access_token', response.accessToken);
    storage.setItem('clams_user', JSON.stringify(user));
  }

  getToken(): string | null {
    return localStorage.getItem('clams_access_token')
      ?? sessionStorage.getItem('clams_access_token');
  }

  getCurrentUser(): CurrentUser | null {
    const savedUser =
      localStorage.getItem('clams_user')
      ?? sessionStorage.getItem('clams_user');

    return savedUser
      ? JSON.parse(savedUser) as CurrentUser
      : null;
  }

  isLoggedIn(): boolean {
    const token = this.getToken();

    if (!token) {
      return false;
    }

    const user = this.getCurrentUser();

    if (
      !user?.expiresAt ||
      new Date(user.expiresAt).getTime() <= Date.now()
    ) {
      this.clearToken();
      return false;
    }

    return true;
  }

  logout(): void {
    this.clearToken();
  }

  private clearToken(): void {
    localStorage.removeItem('clams_access_token');
    localStorage.removeItem('clams_user');

    sessionStorage.removeItem('clams_access_token');
    sessionStorage.removeItem('clams_user');
  }
}
