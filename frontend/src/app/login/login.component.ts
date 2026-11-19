import {
  Component,
  inject,
  ChangeDetectorRef
} from '@angular/core';

import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import {
  AuthService,
  LoginResponse
} from '../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {

  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  username = '';
  password = '';
  rememberMe = false;
  message = '';
  isLoading = false;

  onLogin(): void {

    this.message = '';

    if (!this.username.trim() || !this.password) {
      this.message = 'Please enter your username and password.';
      return;
    }

    this.isLoading = true;

    this.authService.login(
      {
        portalUsername: this.username.trim(),
        password: this.password
      },
      this.rememberMe
    )
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response: LoginResponse) => {

          console.log('CLAMS login successful:', {
            isSuccessful: response.isSuccessful,
            employeeName: response.employeeName,
            employeeNumber: response.employeeNumber,
            roles: response.roles,
            message: response.message
          });

          if (response.isSuccessful && response.accessToken) {

            const roles = response.roles ?? [];

            let destination = '/access-denied';

            if (
              roles.includes('SystemAdministrator') ||
              roles.includes('HRManager')
            ) {
              destination = '/dashboard';
            } else if (roles.includes('HROfficer')) {
              destination = '/employees';
            } else if (
              roles.includes('Management') ||
              roles.includes('ReadOnly')
            ) {
              destination = '/departments';
            } else if (roles.includes('Employee')) {
              destination = '/leave-types';
            }

            this.router.navigate([destination]);

          } else {
            this.message =
              response.message || 'Login was unsuccessful.';
          }

          this.cdr.detectChanges();
        },

        error: (error: HttpErrorResponse) => {

          console.error('CLAMS login error:', error);

          const serverMessage =
            error.error &&
              typeof error.error === 'object' &&
              'message' in error.error
              ? String(error.error.message)
              : null;

          this.message =
            serverMessage ??
            'Unable to connect to the CLAMS server. Please try again.';

          this.cdr.detectChanges();
        }
      });
  }

  resetForm(): void {
    this.username = '';
    this.password = '';
    this.rememberMe = false;
    this.message = '';
    this.isLoading = false;
  }

  forgotPassword(): void {
    this.message =
      'Please contact your HR administrator to reset your password.';
  }
}
